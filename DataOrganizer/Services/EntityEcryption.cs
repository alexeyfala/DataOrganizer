using Avalonia;
using Avalonia.Threading;
using DataOrganizer.Abstract;
using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using Entities.Models;
using Microsoft.Data.Sqlite;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class EntityEcryption : IEntityEcryption
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <summary>
	/// Encryption session identifier.
	/// </summary>
	private byte[]? _sessionId;
	#endregion

	#region Constructors
	public EntityEcryption(
		Application app,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IDispatcher dispatcher,
		IEncryptionService encryption,
		IFileSystem fileSystem,
		ILogger logger)
	{
		_app = app;

		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_dispatcher = dispatcher;

		_encryption = encryption;

		_fileSystem = fileSystem;

		_logger = logger;
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

		if (await _dialogService
			.RequestUserPasswordAsync(Strings.ChangePassword, Strings.OldPassword, token)
			.ConfigureAwait(true) is not { } oldPassword)
		{
			return;
		}

		if (!_encryption.EnhancedVerify(oldPassword, folder.PasswordHash))
		{
			ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.IncorrectPassword));

			return;
		}

		if (await _dialogService
			.RequestUserPasswordAsync(Strings.ChangePassword, Strings.NewPassword, token)
			.ConfigureAwait(false) is not { } newPassword)
		{
			return;
		}

		if (_encryption.RewrapDek(
			folder.EncryptedDek,
			TextHelper.Utf8Encoding.GetBytes(oldPassword),
			TextHelper.Utf8Encoding.GetBytes(newPassword)) is not { } encryptedDek)
		{
			return;
		}

		string passwordHash = _encryption.EnhancedHashPassword(newPassword);

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

		ExecuteInEditor(x => x.ShowInfoSnackbar(Strings.PasswordChanged));
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

		if (await _dialogService
			.RequestUserPasswordAsync(Strings.DecryptFiles, token: token)
			.ConfigureAwait(false) is not { } password)
		{
			return;
		}

		try
		{
			ExecuteInEditor(x => x.IsActionInProgress = true);

			if (!_encryption.EnhancedVerify(password, folder.PasswordHash))
			{
				ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.IncorrectPassword));

				return;
			}

			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(files.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			if (!AreLoadedContentsValid(contents, files.Length))
			{
				ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToLoadFilesContents));

				return;
			}

			if (_encryption.Decrypt(
				folder.EncryptedDek,
				TextHelper.Utf8Encoding.GetBytes(password)) is not { } decryptedDek)
			{
				return;
			}

			try
			{
				ContentsIsValidPair[] result = [.. _encryption.DecryptContents(contents, decryptedDek)];

				if (!AreContentsValid(result, contents.Length))
				{
					ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

					return;
				}

				if (_dbAccess.BackupDatabase() is not { } backupFilePath)
				{
					ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup));

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
				CryptographicOperations.ZeroMemory(decryptedDek);
			}
		}
		finally
		{
			ExecuteInEditor(x => x.IsActionInProgress = false);
		}
	}

	/// <inheritdoc />
	public bool DecryptSessionContents(
		byte[] encryptedContents,
		byte[] sessionEncryptedDek,
		out byte[] decryptedContents)
	{
		decryptedContents = [];

		if (_encryption.Decrypt(
			sessionEncryptedDek,
			GetSessionId()) is not { } decryptedDek)
		{
			return false;
		}

		try
		{
			if (_encryption.Decrypt(encryptedContents, decryptedDek) is not { } decrypted)
			{
				return false;
			}

			decryptedContents = decrypted;

			return true;
		}
		finally
		{
			CryptographicOperations.ZeroMemory(decryptedDek);
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
			CryptographicOperations.ZeroMemory(decryptedDek);
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
			ExecuteInEditor(x => x.IsActionInProgress = true);

			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(files.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			if (!AreLoadedContentsValid(contents, files.Length))
			{
				ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToLoadFilesContents));

				return;
			}

			byte[] dek = _encryption.CreateRandomDek();

			try
			{
				ContentsIsValidPair[] result = [.. _encryption.EncryptContents(contents, dek)];

				if (!AreContentsValid(result, contents.Length))
				{
					ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

					return;
				}

				if (_encryption.Encrypt(
					dek,
					TextHelper.Utf8Encoding.GetBytes(password)) is not { } encryptedDek)
				{
					return;
				}

				if (_dbAccess.BackupDatabase() is not { } backupFilePath)
				{
					ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup));

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
					PasswordHash = _encryption.EnhancedHashPassword(password)
				};

				await UpdateDatabaseAsync(parameters, token).ConfigureAwait(false);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(dek);
			}
		}
		finally
		{
			ExecuteInEditor(x => x.IsActionInProgress = false);
		}
	}

	/// <inheritdoc />
	public bool EncryptSessionContents(
		byte[] decryptedContents,
		byte[] sessionEncryptedDek,
		out byte[] encryptedContents)
	{
		encryptedContents = [];

		if (_encryption.Decrypt(
			sessionEncryptedDek,
			GetSessionId()) is not { } decryptedDek)
		{
			return false;
		}

		try
		{
			if (_encryption.Encrypt(
				decryptedContents,
				decryptedDek) is not { } encrypted)
			{
				return false;
			}

			encryptedContents = encrypted;

			return true;
		}
		finally
		{
			CryptographicOperations.ZeroMemory(decryptedDek);
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
		if (folder.SessionEncryptedDek is not null)
		{
			CryptographicOperations.ZeroMemory(folder.SessionEncryptedDek);

			folder.SessionEncryptedDek = null;
		}

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

		CryptographicOperations.ZeroMemory(_sessionId);

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
			ExecuteInEditor(x => x.IsActionInProgress = true);

			if (!_encryption.EnhancedVerify(password, root.PasswordHash))
			{
				ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.IncorrectPassword));

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
				CryptographicOperations.ZeroMemory(dek);
			}
		}
		finally
		{
			ExecuteInEditor(x => x.IsActionInProgress = false);
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
			ExecuteInEditor(x => x.IsActionInProgress = true);

			if (!_encryption.EnhancedVerify(password, folder.PasswordHash))
			{
				ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.IncorrectPassword));

				return;
			}

			if (ShowFolderContents(folder, password))
			{
				return;
			}

			ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToShowFileContents));
		}
		finally
		{
			ExecuteInEditor(x => x.IsActionInProgress = false);
		}
	}

	/// <inheritdoc />
	public bool TryToDecrypt(
		byte[] input,
		FileModelDto file,
		out byte[] output)
	{
		output = input;

		return file.FindParent(x => x.IsPasswordKeeper()) is { } root
			&& root.SessionEncryptedDek is not null
			&& DecryptSessionContents(input, root.SessionEncryptedDek, out output);
	}

	/// <inheritdoc />
	public async Task<byte[]?> TryToDecryptContentsAsync(
		FileModelDto file,
		byte[] contents,
		CancellationToken token = default)
	{
		if (file.EncryptionStatus == EncryptionStatus.Encrypted)
		{
			if (await _dialogService
				.RequestUserPasswordAsync(Strings.DecryptFiles, token: token)
				.ConfigureAwait(true) is not { } password)
			{
				return null;
			}

			if (file.FindParent(x => x.IsPasswordKeeper()) is not { } root
				|| root.EncryptedDek is null
				|| root.PasswordHash is null)
			{
				return null;
			}

			if (!_encryption.EnhancedVerify(password, root.PasswordHash))
			{
				ExecuteInBaseViewModel(x => x.ShowErrorSnackbar(Strings.IncorrectPassword));

				return null;
			}

			if (_encryption.Decrypt(
				root.EncryptedDek,
				TextHelper.Utf8Encoding.GetBytes(password)) is not { } decryptedDek)
			{
				ExecuteInBaseViewModel(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

				return null;
			}

			try
			{
				if (_encryption.Decrypt(contents, decryptedDek) is not { } decrypted)
				{
					ExecuteInBaseViewModel(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

					return null;
				}

				return decrypted;
			}
			finally
			{
				CryptographicOperations.ZeroMemory(decryptedDek);
			}
		}
		else if (file.EncryptionStatus == EncryptionStatus.Decrypted)
		{
			if (!TryToDecrypt(
				contents,
				file,
				out byte[] decrypted))
			{
				return null;
			}

			return decrypted;
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
				ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

				await _dbAccess
					.RestoreFromBackupAsync(parameters.BackupFilePath, token)
					.ConfigureAwait(false);

				DeleteDatabaseBackupFile(parameters.BackupFilePath);

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
				ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToProcessContents));

				await _dbAccess
					.RestoreFromBackupAsync(parameters.BackupFilePath, token)
					.ConfigureAwait(false);

				DeleteDatabaseBackupFile(parameters.BackupFilePath);

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

			DeleteDatabaseBackupFile(parameters.BackupFilePath);

			return UpdateDatabaseResult.Done;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return UpdateDatabaseResult.ExceptionThrown;
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
	/// Deletes the database backup file.
	/// </summary>
	private void DeleteDatabaseBackupFile(string filePath)
	{
		try
		{
			SqliteConnection.ClearAllPools();

			_fileSystem.EraseAndDeleteFile(filePath);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <summary>
	/// Searches <see cref="ViewModelBase" /> in main thread and executes the action.
	/// </summary>
	private void ExecuteInBaseViewModel(Action<ViewModelBase> action) => _dispatcher.Post(() =>
	{
		if (_app.FindBaseDataContext() is not { } viewModel)
		{
			return;
		}

		action(viewModel);
	});

	/// <summary>
	/// Searches <see cref="EditorViewModel" /> in main thread and executes the action.
	/// </summary>
	private void ExecuteInEditor(Action<EditorViewModel> action) => _dispatcher.Post(() =>
	{
		if (_app.FindDataContext<EditorViewModel>() is not { } viewModel)
		{
			return;
		}

		action(viewModel);
	});

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
			CryptographicOperations.ZeroMemory(dek);
		}
	}
	#endregion
}
