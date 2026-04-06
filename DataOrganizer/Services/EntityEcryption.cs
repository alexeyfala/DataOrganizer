using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using Entities.Models;
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

public sealed class EntityEcryption : IEntityEcryption
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

	/// <inheritdoc cref="IViewModelExecutionService" />
	private readonly IViewModelExecutionService _viewModel;

	/// <summary>
	/// Encryption session identifier.
	/// </summary>
	private byte[]? _sessionId;
	#endregion

	#region Constructors
	public EntityEcryption(
		IDbAccess dbAccess,
		IDialogService dialogService,
		IEncryptionService encryption,
		IFileSystem fileSystem,
		ILogger logger,
		IViewModelExecutionService viewModel)
	{
		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_encryption = encryption;

		_fileSystem = fileSystem;

		_logger = logger;

		_viewModel = viewModel;
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
				_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.IncorrectPassword));

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
				byte[] oldPasswordBytes = TextHelper
					.Utf8Encoding
					.GetBytes(oldPassword);

				byte[] newPasswordBytes = TextHelper
					.Utf8Encoding
					.GetBytes(newPassword);

				try
				{
					if (_encryption.RewrapDek(
						folder.EncryptedDek,
						oldPasswordBytes,
						newPasswordBytes) is not { } encryptedDek)
					{
						return;
					}

					string passwordHash = _encryption.HashPassword(newPassword);

					PropertyNameValuePair[] properties =
					[
						new PropertyNameValuePair(nameof(FolderModel.PasswordHash), passwordHash),
						new PropertyNameValuePair(nameof(FolderModel.EncryptedDek), encryptedDek)
					];

					if (!await _dbAccess.UpdatePropertiesAsync(
						id: folder.Id,
						token: token,
						properties: properties).ConfigureAwait(false))
					{
						return;
					}

					folder.PasswordHash = passwordHash;

					folder.EncryptedDek = encryptedDek;

					_viewModel.ExecuteInEditor(x => x.ShowInfoSnackbar(Strings.PasswordChanged));
				}
				finally
				{
					oldPasswordBytes.ZeroMemory();

					newPasswordBytes.ZeroMemory();
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
			_viewModel.ExecuteInEditor(x => x.IsActionInProgress = true);

			if (!_encryption.VerifyPassword(password, folder.PasswordHash))
			{
				_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.IncorrectPassword));

				return;
			}

			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(files.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			if (!AreLoadedContentsValid(contents, files.Length))
			{
				_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToLoadFilesContents));

				return;
			}

			byte[] passwordBytes = TextHelper
				.Utf8Encoding
				.GetBytes(password);

			try
			{
				if (_encryption.Decrypt(
					folder.EncryptedDek,
					passwordBytes) is not { } decryptedDek)
				{
					return;
				}

				try
				{
					ContentsIsValidPair[] result = [.. _encryption.DecryptContents(contents, decryptedDek)];

					if (!AreContentsValid(result, contents.Length))
					{
						_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

						return;
					}

					if (_dbAccess.BackupDatabase() is not { } backupFilePath || string.IsNullOrEmpty(backupFilePath))
					{
						_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup));

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
				passwordBytes.ZeroMemory();
			}
		}
		finally
		{
			MemoryMarshal
				.AsBytes(password.AsSpan())
				.ZeroMemory();

			_viewModel.ExecuteInEditor(x => x.IsActionInProgress = false);
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
			return _encryption.Decrypt(encryptedContents, decryptedDek);
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
		if (await _dialogService
			.RequestUserPasswordAsync(Strings.EncryptFiles, token: token)
			.ConfigureAwait(false) is not { } password)
		{
			return;
		}

		try
		{
			_viewModel.ExecuteInEditor(x => x.IsActionInProgress = true);

			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(files.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			if (!AreLoadedContentsValid(contents, files.Length))
			{
				_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToLoadFilesContents));

				return;
			}

			byte[] dek = _encryption.CreateRandomDek();

			try
			{
				ContentsIsValidPair[] result = [.. _encryption.EncryptContents(contents, dek)];

				if (!AreContentsValid(result, contents.Length))
				{
					_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

					return;
				}

				if (_encryption.Encrypt(
					dek,
					TextHelper.Utf8Encoding.GetBytes(password)) is not { } encryptedDek)
				{
					return;
				}

				if (_dbAccess.BackupDatabase() is not { } backupFilePath || string.IsNullOrEmpty(backupFilePath))
				{
					_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup));

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
				dek.ZeroMemory();
			}
		}
		finally
		{
			_viewModel.ExecuteInEditor(x => x.IsActionInProgress = false);
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
			return _encryption.Encrypt(decryptedContents, decryptedDek);
		}
		finally
		{
			decryptedDek.ZeroMemory();
		}
	}

	/// <inheritdoc />
	public byte[] GetSessionId()
	{
		if (_sessionId?.Length > 0)
		{
			return _sessionId;
		}

		_sessionId = RandomNumberGenerator.GetBytes(Random
			.Shared
			.Next(32, 65));

		return _sessionId;
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
		if (_sessionId is null)
		{
			return;
		}

		_sessionId.ZeroMemory();

		_sessionId = null;
	}

	/// <inheritdoc />
	public async Task ShowFileContentsAsync(FileModelDto file, CancellationToken token = default)
	{
		if (file.FindParent(x => x.IsPasswordKeeper()) is not { } root
			|| root.EncryptedDek is null
			|| root.PasswordHash is null)
		{
			return;
		}

		if (await _dialogService
			.RequestUserPasswordAsync(Strings.ShowContents, token: token)
			.ConfigureAwait(false) is not { } password)
		{
			return;
		}

		try
		{
			_viewModel.ExecuteInEditor(x => x.IsActionInProgress = true);

			if (!_encryption.VerifyPassword(password, root.PasswordHash))
			{
				_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.IncorrectPassword));

				return;
			}

			if (_encryption.Decrypt(
				root.EncryptedDek,
				TextHelper.Utf8Encoding.GetBytes(password)) is not { } dek)
			{
				return;
			}

			try
			{
				if (_encryption.Encrypt(
					dek,
					GetSessionId()) is not { } sessionEncryptedDek)
				{
					return;
				}

				root.SessionEncryptedDek = sessionEncryptedDek;

				file.EncryptionStatus = EncryptionStatus.Decrypted;
			}
			finally
			{
				dek.ZeroMemory();
			}
		}
		finally
		{
			_viewModel.ExecuteInEditor(x => x.IsActionInProgress = false);
		}
	}

	/// <inheritdoc />
	public async Task ShowFolderContentsAsync(FolderModelDto folder, CancellationToken token = default)
	{
		if (folder.PasswordHash is null)
		{
			return;
		}

		if (await _dialogService
			.RequestUserPasswordAsync(Strings.ShowContents, token: token)
			.ConfigureAwait(false) is not { } password)
		{
			return;
		}

		try
		{
			_viewModel.ExecuteInEditor(x => x.IsActionInProgress = true);

			if (!_encryption.VerifyPassword(password, folder.PasswordHash))
			{
				_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.IncorrectPassword));

				return;
			}

			if (ShowFolderContents(folder, password))
			{
				return;
			}

			_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToShowFileContents));
		}
		finally
		{
			_viewModel.ExecuteInEditor(x => x.IsActionInProgress = false);
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
			if (await _dialogService
				.RequestUserPasswordAsync(header, token: token)
				.ConfigureAwait(false) is not { } password)
			{
				return null;
			}

			if (file.FindParent(x => x.IsPasswordKeeper()) is not { } root
				|| root.EncryptedDek is null
				|| root.PasswordHash is null)
			{
				return null;
			}

			if (!_encryption.VerifyPassword(password, root.PasswordHash))
			{
				_viewModel.ExecuteInBaseViewModel(x => x.ShowErrorSnackbar(Strings.IncorrectPassword));

				return null;
			}

			if (_encryption.Decrypt(
				root.EncryptedDek,
				TextHelper.Utf8Encoding.GetBytes(password)) is not { } decryptedDek)
			{
				_viewModel.ExecuteInBaseViewModel(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

				return null;
			}

			try
			{
				if (_encryption.Decrypt(contents, decryptedDek) is not { } decrypted)
				{
					_viewModel.ExecuteInBaseViewModel(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

					return null;
				}

				return decrypted;
			}
			finally
			{
				decryptedDek.ZeroMemory();
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

			Dictionary<Guid, PropertyNameValuePair[]> relations = parameters.Contents.ToDictionary(x => x.Id, x =>
			{
				return new PropertyNameValuePair[]
				{
					new(nameof(FileModel.Contents), x.Contents),
					new(nameof(FileModel.UpdatedDate), updatedDate)
				};
			});

			if (!await _dbAccess
				.UpdatePropertiesAsync(relations, token)
				.ConfigureAwait(false))
			{
				_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

				await _dbAccess
					.RestoreFromBackupAsync(parameters.BackupFilePath, token)
					.ConfigureAwait(false);

				DeleteFile(parameters.BackupFilePath);

				return UpdateDatabaseResult.FailedToSaveContentsInDb;
			}

			PropertyNameValuePair[] properties =
			[
				new PropertyNameValuePair(nameof(FolderModel.PasswordHash), parameters.PasswordHash),
				new PropertyNameValuePair(nameof(FolderModel.EncryptedDek), parameters.EncryptedDek)
			];

			if (!await _dbAccess.UpdatePropertiesAsync(
				id: parameters.Folder.Id,
				token: token,
				properties: properties).ConfigureAwait(false))
			{
				_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

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

	#region Service
	/// <summary>
	/// Returns <c>True</c> if the contents are valid.
	/// </summary>
	private static bool AreContentsValid(ContentsIsValidPair[] contents, int shouldBe)
	{
		return contents.Length == shouldBe
			&& contents.All(x => x.IsValid)
			&& contents.All(x => x.Id.IsNotDefault());
	}

	/// <summary>
	/// Returns <c>True</c> if the loaded from database contents are valid.
	/// </summary>
	private static bool AreLoadedContentsValid(ContentsIsValidPair[] contents, int fileCount)
	{
		return contents.Length == fileCount && contents.All(x => x.IsValid);
	}

	/// <summary>
	/// Shows file contents in folder.
	/// </summary>
	private bool ShowFolderContents(
		FolderModelDto folder,
		string password)
	{
		FolderModelDto? root = folder.IsPasswordKeeper()
			? folder
			: folder.FindParent(x => x.IsPasswordKeeper());

		if (root is null
			|| root.EncryptedDek is null
			|| _encryption.Decrypt(root.EncryptedDek, TextHelper.Utf8Encoding.GetBytes(password)) is not { } dek
			|| _encryption.Encrypt(dek, GetSessionId()) is not { } sessionEncryptedDek)
		{
			return false;
		}

		try
		{
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
	#endregion
}
