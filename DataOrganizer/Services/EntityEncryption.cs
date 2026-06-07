using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class EntityEncryption : IEntityEncryption
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="Lock" />
	private readonly Lock _mutex = new();

	/// <summary>
	/// Encryption session identifier.
	/// </summary>
	private byte[]? _sessionId;
	#endregion

	#region Constructors
	public EntityEncryption(
		IDbAccess dbAccess,
		IDialogService dialogService,
		IEncryptionService encryption,
		IFileSystem fileSystem,
		ILogger logger,
		IMessenger messenger)
	{
		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_encryption = encryption;

		_fileSystem = fileSystem;

		_logger = logger;

		_messenger = messenger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task ChangePasswordAsync(FolderModelDto folder, CancellationToken token = default)
	{
		if (folder.EncryptedDek is null || folder.PasswordHash is null)
		{
			return;
		}

		char[] oldPassword = await _dialogService
			.RequestPasswordAsync(Strings.ChangePassword, Strings.OldPassword, token)
			.ConfigureAwait(false);

		if (oldPassword.IsEmpty())
		{
			return;
		}

		try
		{
			if (!_encryption.VerifyPassword(oldPassword, folder.PasswordHash))
			{
				SendMessage(Strings.IncorrectPassword, SnackbarMessageLevel.Error);

				return;
			}

			char[] newPassword = await _dialogService
				.RequestPasswordAsync(Strings.ChangePassword, Strings.NewPassword, token)
				.ConfigureAwait(false);

			if (newPassword.IsEmpty())
			{
				return;
			}

			try
			{
				byte[] oldPasswordBinary = TextHelper
					.Utf8Encoding
					.GetBytes(oldPassword);

				byte[] newPasswordBinary = TextHelper
					.Utf8Encoding
					.GetBytes(newPassword);

				try
				{
					if (_encryption.RewrapDek(
						folder.EncryptedDek,
						oldPasswordBinary,
						newPasswordBinary) is not { } encryptedDek)
					{
						return;
					}

					string passwordHash = _encryption.HashPassword(newPassword);

					if (!await _dbAccess.UpdateFolderPropertiesAsync(folder.Id,
						[
							x => x.SetProperty(x => x.PasswordHash, passwordHash),
							x => x.SetProperty(x => x.EncryptedDek, encryptedDek)
						], token).ConfigureAwait(false))
					{
						return;
					}

					folder.PasswordHash = passwordHash;

					folder.EncryptedDek = encryptedDek;

					SendMessage(Strings.PasswordChanged, SnackbarMessageLevel.Information);
				}
				finally
				{
					oldPasswordBinary.ZeroMemory();

					newPasswordBinary.ZeroMemory();
				}
			}
			finally
			{
				MemoryMarshal
					.AsBytes(newPassword.AsSpan())
					.ZeroMemory();
			}
		}
		finally
		{
			MemoryMarshal
				.AsBytes(oldPassword.AsSpan())
				.ZeroMemory();
		}
	}

	/// <inheritdoc />
	public async Task DecryptFolderAsync(
		FolderModelDto folder,
		FileModelDto[] files,
		CancellationToken token = default)
	{
		if (folder.EncryptedDek is null || folder.PasswordHash is null)
		{
			return;
		}

		char[] password = await _dialogService
			.RequestPasswordAsync(Strings.DecryptFiles, token: token)
			.ConfigureAwait(false);

		if (password.IsEmpty())
		{
			return;
		}

		try
		{
			ShowProgressBar();

			if (!_encryption.VerifyPassword(password, folder.PasswordHash))
			{
				SendMessage(Strings.IncorrectPassword, SnackbarMessageLevel.Error);

				return;
			}

			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(files.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			if (!AreLoadedContentsValid(contents, files.Length))
			{
				SendMessage(Strings.FailedToLoadFilesContents, SnackbarMessageLevel.Error);

				return;
			}

			byte[] passwordBinary = TextHelper
				.Utf8Encoding
				.GetBytes(password);

			try
			{
				if (_encryption.Decrypt(
					folder.EncryptedDek,
					passwordBinary) is not { } decryptedDek)
				{
					return;
				}

				try
				{
					ContentsIsValidPair[] result = [.. _encryption.DecryptContents(contents, decryptedDek)];

					if (!AreContentsValid(result, contents.Length))
					{
						SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

						return;
					}

					if (await _dbAccess
						.BackupDatabaseAsync(token)
						.ConfigureAwait(false) is not { } backupFilePath || string.IsNullOrEmpty(backupFilePath))
					{
						SendMessage(Strings.UnableToCreateDatabaseBackup, SnackbarMessageLevel.Error);

						return;
					}

					UpdateDatabaseParameters parameters = new()
					{
						BackupFilePath = backupFilePath,
						Contents = result,
						EncryptedDek = null,
						Files = files,
						Folder = folder,
						NewStatus = EncryptionStatus.None,
						PasswordHash = null
					};

					await UpdateDatabaseAsync(parameters, token).ConfigureAwait(false);
				}
				finally
				{
					decryptedDek.ZeroMemory();
				}
			}
			finally
			{
				passwordBinary.ZeroMemory();
			}
		}
		finally
		{
			MemoryMarshal
				.AsBytes(password.AsSpan())
				.ZeroMemory();

			HideProgressBar();
		}
	}

	/// <inheritdoc />
	public byte[]? DecryptSessionContents(byte[] encryptedContents, byte[] sessionEncryptedDek)
	{
		if (_encryption.Decrypt(
			sessionEncryptedDek,
			GetSessionId()) is not { } decryptedDek)
		{
			return null;
		}

		try
		{
			return _encryption.DecryptWithDek(encryptedContents, decryptedDek);
		}
		finally
		{
			decryptedDek.ZeroMemory();
		}
	}

	/// <inheritdoc />
	public async Task EncryptFolderAsync(
		FolderModelDto folder,
		FileModelDto[] files,
		CancellationToken token = default)
	{
		char[] password = await _dialogService
			.RequestPasswordAsync(Strings.EncryptFiles, token: token)
			.ConfigureAwait(false);

		if (password.IsEmpty())
		{
			return;
		}

		try
		{
			ShowProgressBar();

			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(files.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			if (!AreLoadedContentsValid(contents, files.Length))
			{
				SendMessage(Strings.FailedToLoadFilesContents, SnackbarMessageLevel.Error);

				return;
			}

			byte[] dek = _encryption.CreateRandomDek();

			try
			{
				ContentsIsValidPair[] result = [.. _encryption.EncryptContents(contents, dek)];

				if (!AreContentsValid(result, contents.Length))
				{
					SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

					return;
				}

				byte[] passwordBinary = TextHelper
					.Utf8Encoding
					.GetBytes(password);

				if (_encryption.Encrypt(
					dek,
					passwordBinary) is not { } encryptedDek)
				{
					return;
				}

				try
				{
					if (await _dbAccess
						.BackupDatabaseAsync(token)
						.ConfigureAwait(false) is not { } backupFilePath || string.IsNullOrEmpty(backupFilePath))
					{
						SendMessage(Strings.UnableToCreateDatabaseBackup, SnackbarMessageLevel.Error);

						return;
					}

					UpdateDatabaseParameters parameters = new()
					{
						BackupFilePath = backupFilePath,
						Contents = result,
						EncryptedDek = encryptedDek,
						Files = files,
						Folder = folder,
						NewStatus = EncryptionStatus.Encrypted,
						PasswordHash = _encryption.HashPassword(password)
					};

					await UpdateDatabaseAsync(parameters, token).ConfigureAwait(false);
				}
				finally
				{
					passwordBinary.ZeroMemory();
				}
			}
			finally
			{
				dek.ZeroMemory();
			}
		}
		finally
		{
			MemoryMarshal
				.AsBytes(password.AsSpan())
				.ZeroMemory();

			HideProgressBar();
		}
	}

	/// <inheritdoc />
	public byte[]? EncryptSessionContents(byte[] decryptedContents, byte[] sessionEncryptedDek)
	{
		if (_encryption.Decrypt(
			sessionEncryptedDek,
			GetSessionId()) is not { } decryptedDek)
		{
			return null;
		}

		try
		{
			return _encryption.EncryptWithDek(decryptedContents, decryptedDek);
		}
		finally
		{
			decryptedDek.ZeroMemory();
		}
	}

	/// <inheritdoc />
	public byte[] GetSessionId()
	{
		lock (_mutex)
		{
			if (_sessionId?.IsNotEmpty() == true)
			{
				return _sessionId;
			}

			int length = RandomNumberGenerator.GetInt32(32, 65);

			_sessionId = RandomNumberGenerator.GetBytes(length);

			return _sessionId;
		}
	}

	/// <inheritdoc />
	public void HideFolderContents(FolderModelDto folder, IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		folder
			.SessionEncryptedDek?
			.ZeroMemory();

		folder.SessionEncryptedDek = null;

		folder
			.ToEnumerable()
			.Concat(folder.GetAllChildren())
			.ForEach(x => x.EncryptionStatus = EncryptionStatus.Encrypted);

		if (hierarchy.ContainsBy(x => x.EncryptionStatus == EncryptionStatus.Decrypted))
		{
			return;
		}

		ResetSessionId();
	}

	/// <inheritdoc />
	public void ResetSessionId()
	{
		lock (_mutex)
		{
			if (_sessionId is null)
			{
				return;
			}

			_sessionId.ZeroMemory();

			_sessionId = null;
		}
	}

	/// <inheritdoc />
	public async Task<bool> ShowFileContentsAsync(FileModelDto file, CancellationToken token = default)
	{
		if (file.FindParent(x => x.IsPasswordKeeper()) is not { } root
			|| root.EncryptedDek is null
			|| root.PasswordHash is null)
		{
			return false;
		}

		char[] password = await _dialogService
			.RequestPasswordAsync(Strings.ShowContents, token: token)
			.ConfigureAwait(false);

		if (password.IsEmpty())
		{
			return false;
		}

		try
		{
			ShowProgressBar();

			if (!_encryption.VerifyPassword(password, root.PasswordHash))
			{
				SendMessage(Strings.IncorrectPassword, SnackbarMessageLevel.Error);

				return false;
			}

			byte[] passwordBinary = TextHelper
				.Utf8Encoding
				.GetBytes(password);

			if (_encryption.Decrypt(
				root.EncryptedDek,
				passwordBinary) is not { } dek)
			{
				return false;
			}

			try
			{
				if (_encryption.Encrypt(
					dek,
					GetSessionId()) is not { } sessionEncryptedDek)
				{
					return false;
				}

				root.SessionEncryptedDek = sessionEncryptedDek;

				file.EncryptionStatus = EncryptionStatus.Decrypted;

				return true;
			}
			finally
			{
				passwordBinary.ZeroMemory();

				dek.ZeroMemory();
			}
		}
		finally
		{
			MemoryMarshal
				.AsBytes(password.AsSpan())
				.ZeroMemory();

			HideProgressBar();
		}
	}

	/// <inheritdoc />
	public async Task ShowFolderContentsAsync(FolderModelDto folder, CancellationToken token = default)
	{
		if (folder.FindPasswordKeeperOrSelf() is not { } root || root.PasswordHash is null)
		{
			return;
		}

		char[] password = await _dialogService
			.RequestPasswordAsync(Strings.ShowContents, token: token)
			.ConfigureAwait(false);

		if (password.IsEmpty())
		{
			return;
		}

		try
		{
			ShowProgressBar();

			if (!_encryption.VerifyPassword(password, root.PasswordHash))
			{
				SendMessage(Strings.IncorrectPassword, SnackbarMessageLevel.Error);

				return;
			}

			byte[] passwordBinary = TextHelper
				.Utf8Encoding
				.GetBytes(password);

			try
			{
				if (ShowFolderContents(folder, passwordBinary))
				{
					return;
				}

				SendMessage(Strings.FailedToShowFileContents, SnackbarMessageLevel.Error);
			}
			finally
			{
				passwordBinary.ZeroMemory();
			}
		}
		finally
		{
			MemoryMarshal
				.AsBytes(password.AsSpan())
				.ZeroMemory();

			HideProgressBar();
		}
	}

	/// <inheritdoc />
	public byte[]? TryToDecrypt(FileModelDto file, byte[] input)
	{
		return file.FindParent(x => x.IsPasswordKeeper()) is { } root && root.SessionEncryptedDek is not null
			? DecryptSessionContents(input, root.SessionEncryptedDek)
			: null;
	}

	/// <inheritdoc />
	public async Task<byte[]?> TryToDecryptContentsAsync(
		FileModelDto file,
		byte[] contents,
		string header,
		CancellationToken token = default)
	{
		if (file.EncryptionStatus == EncryptionStatus.Encrypted)
		{
			char[] password = await _dialogService
				.RequestPasswordAsync(header, token: token)
				.ConfigureAwait(false);

			if (password.IsEmpty())
			{
				return null;
			}

			try
			{
				if (file.FindParent(x => x.IsPasswordKeeper()) is not { } root
					|| root.EncryptedDek is null
					|| root.PasswordHash is null)
				{
					return null;
				}

				if (!_encryption.VerifyPassword(password, root.PasswordHash))
				{
					SendMessage(Strings.IncorrectPassword, SnackbarMessageLevel.Error);

					return null;
				}

				byte[] passwordBinary = TextHelper
					.Utf8Encoding
					.GetBytes(password);

				if (_encryption.Decrypt(
					root.EncryptedDek,
					passwordBinary) is not { } decryptedDek)
				{
					SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

					return null;
				}

				try
				{
					if (_encryption.DecryptWithDek(contents, decryptedDek) is not { } decrypted)
					{
						SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

						return null;
					}

					return decrypted;
				}
				finally
				{
					passwordBinary.ZeroMemory();

					decryptedDek.ZeroMemory();
				}
			}
			finally
			{
				MemoryMarshal
					.AsBytes(password.AsSpan())
					.ZeroMemory();
			}
		}
		else if (file.EncryptionStatus == EncryptionStatus.Decrypted)
		{
			return TryToDecrypt(file, contents);
		}

		return contents;
	}

	/// <inheritdoc />
	public async Task<UpdateDatabaseResult> UpdateDatabaseAsync(
		UpdateDatabaseParameters parameters,
		CancellationToken token = default)
	{
		try
		{
			DateTime updatedDate = DateTime.Now;

			Dictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]> updates = parameters
				.Contents
				.ToDictionary(x => x.Id, pair =>
			{
				return new Action<UpdateSettersBuilder<FileModel>>[]
				{
					builder => builder.SetProperty(x => x.Contents, pair.Contents),
					builder => builder.SetProperty(x => x.UpdatedDate, updatedDate)
				};
			});

			if (!await _dbAccess
				.UpdateFilePropertiesAsync(updates, token)
				.ConfigureAwait(false))
			{
				SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

				await _dbAccess
					.RestoreFromBackupAsync(parameters.BackupFilePath, token)
					.ConfigureAwait(false);

				DeleteFile(parameters.BackupFilePath);

				return UpdateDatabaseResult.FailedToSaveContentsInDb;
			}

			if (!await _dbAccess.UpdateFolderPropertiesAsync(parameters.Folder.Id,
				[
					x => x.SetProperty(x => x.PasswordHash, parameters.PasswordHash),
					x => x.SetProperty(x => x.EncryptedDek, parameters.EncryptedDek)
				], token).ConfigureAwait(false))
			{
				SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

				await _dbAccess
					.RestoreFromBackupAsync(parameters.BackupFilePath, token)
					.ConfigureAwait(false);

				DeleteFile(parameters.BackupFilePath);

				return UpdateDatabaseResult.FailedToSaveFolderPropertiesInDb;
			}

			ExplorerModelBaseDto[] objects =
			[
				.. parameters.Folder.ToEnumerable(),
				.. parameters.Folder.Children.GetFolders(),
				.. parameters.Files
			];

			objects.ForEach(x => x.EncryptionStatus = parameters.NewStatus);

			parameters
				.Folder
				.PasswordHash = parameters.PasswordHash;

			parameters
				.Folder
				.EncryptedDek = parameters.EncryptedDek;

			DeleteFile(parameters.BackupFilePath);

			return UpdateDatabaseResult.Done;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return UpdateDatabaseResult.ExceptionThrown;
		}

		void DeleteFile(string filePath)
		{
			try
			{
				_fileSystem.EraseAndDeleteFile(filePath);
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);
			}
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when the contents are valid.
	/// </summary>
	private static bool AreContentsValid(ContentsIsValidPair[] contents, int shouldBe)
	{
		return contents.Length == shouldBe
			&& contents.All(x => x.IsValid)
			&& contents.All(x => x.Id.IsNotDefault());
	}

	/// <summary>
	/// <c>True</c> when the loaded from database contents are valid.
	/// </summary>
	private static bool AreLoadedContentsValid(ContentsIsValidPair[] contents, int fileCount)
	{
		return contents.Length == fileCount && contents.All(x => x.IsValid);
	}

	/// <summary>
	/// Sends <see cref="ShowProgressBarMessage" /> to hide progress bar in the editor.
	/// </summary>
	private void HideProgressBar() => _messenger.Send(new ShowProgressBarMessage(false));

	/// <summary>
	/// Sends <see cref="ShowSnackbarMessage" /> to recepient.
	/// </summary>
	private void SendMessage(string message, SnackbarMessageLevel level)
	{
		_messenger.Send(new ShowSnackbarMessage(message, level));
	}

	/// <summary>
	/// Shows file contents in folder.
	/// </summary>
	private bool ShowFolderContents(
		FolderModelDto folder,
		byte[] password)
	{
		if (folder.FindPasswordKeeperOrSelf() is not { } root
			|| root.EncryptedDek is null
			|| _encryption.Decrypt(root.EncryptedDek, password) is not { } dek)
		{
			return false;
		}

		try
		{
			if (_encryption.Encrypt(dek, GetSessionId()) is not { } sessionEncryptedDek)
			{
				return false;
			}

			root.SessionEncryptedDek = sessionEncryptedDek;

			folder
				.ToEnumerable()
				.Concat(folder.GetAllChildren())
				.ForEach(x => x.EncryptionStatus = EncryptionStatus.Decrypted);

			return true;
		}
		finally
		{
			dek.ZeroMemory();
		}
	}

	/// <summary>
	/// Sends <see cref="ShowProgressBarMessage" /> to display progress bar in the editor.
	/// </summary>
	private void ShowProgressBar() => _messenger.Send(new ShowProgressBarMessage(true));
	#endregion
}
