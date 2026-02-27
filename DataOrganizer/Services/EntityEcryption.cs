using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using DialogHostAvalonia;
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
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;

	/// <summary>
	/// Encryption session identifier.
	/// </summary>
	private byte[]? _sessionId;
	#endregion

	#region Constructors
	public EntityEcryption(
		IDbAccess dbAccess,
		IEncryptionService encryption,
		IFileSystem fileSystem,
		ILogger logger,
		IViewFactory viewFactory)
	{
		_dbAccess = dbAccess;

		_encryption = encryption;

		_fileSystem = fileSystem;

		_logger = logger;

		_viewFactory = viewFactory;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task ChangePasswordAsync(
		FolderModelDto folder,
		EditorViewModel viewModel,
		CancellationToken token = default)
	{
		// TODO: Make test
		if (folder.EncryptedDek is null || folder.PasswordHash is null)
		{
			return;
		}

		if (folder.AnyFile(x => x.IsEdited || x.IsExecuted))
		{
			if (!await viewModel
				.RequestUserCloseFilesAsync(token)
				.ConfigureAwait(true))
			{
				return;
			}

			viewModel.CloseFiles(
				folder.GetFiles(x => x.IsEdited),
				folder.GetFiles(x => x.IsExecuted));
		}

		if (await RequestUserPasswordAsync(Strings.OldPassword, token).ConfigureAwait(true) is not { } oldPassword)
		{
			return;
		}

		if (!_encryption.EnhancedVerify(oldPassword, folder.PasswordHash))
		{
			viewModel.ShowErrorSnackbar(Strings.IncorrectPassword);

			return;
		}

		if (await RequestUserPasswordAsync(Strings.NewPassword, token).ConfigureAwait(false) is not { } newPassword)
		{
			return;
		}

		if (!_encryption.RewrapDek(
			folder.EncryptedDek,
			TextHelper.Utf8Encoding.GetBytes(oldPassword),
			TextHelper.Utf8Encoding.GetBytes(newPassword),
			out byte[] encryptedDek))
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

		viewModel.ShowInfoSnackbar(Strings.PasswordChanged);
	}

	/// <inheritdoc />
	public async Task DecryptFolderAsync(
		FolderModelDto folder,
		FileModelDto[] files,
		EditorViewModel viewModel,
		CancellationToken token = default)
	{
		// TODO: Make test
		if (folder.EncryptedDek is null || folder.PasswordHash is null)
		{
			return;
		}

		if (await RequestUserPasswordAsync(Strings.Password, token).ConfigureAwait(false) is not { } password)
		{
			return;
		}

		try
		{
			viewModel.IsActionInProgress = true;

			if (!_encryption.EnhancedVerify(password, folder.PasswordHash))
			{
				viewModel.ShowErrorSnackbar(Strings.IncorrectPassword);

				return;
			}

			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(files.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			if (!AreLoadedContentsValid(contents, files.Length))
			{
				viewModel.ShowErrorSnackbar(Strings.FailedToLoadFilesContents);

				return;
			}

			if (!_encryption.Decrypt(
				folder.EncryptedDek,
				TextHelper.Utf8Encoding.GetBytes(password),
				out byte[] decryptedDek))
			{
				return;
			}

			try
			{
				ContentsIsValidPair[] result = [.. _encryption.DecryptContents(contents, decryptedDek)];

				if (!AreContentsValid(result, contents.Length))
				{
					viewModel.ShowErrorSnackbar(Strings.FailedToProcessContents);

					return;
				}

				if (!_dbAccess.BackupDatabase(out string? backupFilePath))
				{
					viewModel.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup);

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

				await UpdateDatabaseAsync(
					parameters,
					viewModel,
					token).ConfigureAwait(false);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(decryptedDek);
			}
		}
		finally
		{
			viewModel.IsActionInProgress = false;
		}
	}

	/// <inheritdoc />
	public bool DecryptSessionContents(
		byte[] encryptedContents,
		byte[] sessionEncryptedDek,
		out byte[] decryptedContents)
	{
		decryptedContents = [];

		if (!_encryption.Decrypt(
			sessionEncryptedDek,
			GetSessionId(),
			out byte[] decryptedDek))
		{
			return false;
		}

		try
		{
			return _encryption.Decrypt(
				encryptedContents,
				decryptedDek,
				out decryptedContents);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(decryptedDek);
		}
	}

	/// <inheritdoc />
	public async Task EncryptFolderAsync(
		FolderModelDto folder,
		EditorViewModel viewModel,
		CancellationToken token = default)
	{
		// TODO: Make test
		FileModelDto[] files = [.. folder
			.Children
			.GetFiles()];

		if (files.Length == 0)
		{
			viewModel.ShowInfoSnackbar(Strings.MissingFiles);

			return;
		}

		if (!AreFilesValid(files, viewModel))
		{
			return;
		}

		if (await RequestUserPasswordAsync(Strings.Password, token).ConfigureAwait(false) is not { } password)
		{
			return;
		}

		try
		{
			viewModel.IsActionInProgress = true;

			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(files.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			if (!AreLoadedContentsValid(contents, files.Length))
			{
				viewModel.ShowErrorSnackbar(Strings.FailedToLoadFilesContents);

				return;
			}

			byte[] dek = _encryption.CreateRandomDek();

			try
			{
				ContentsIsValidPair[] result = [.. _encryption.EncryptContents(contents, dek)];

				if (!AreContentsValid(result, contents.Length))
				{
					viewModel.ShowErrorSnackbar(Strings.FailedToProcessContents);

					return;
				}

				if (!_encryption.Encrypt(
					dek,
					TextHelper.Utf8Encoding.GetBytes(password),
					out byte[] encryptedDek))
				{
					return;
				}

				if (!_dbAccess.BackupDatabase(out string? backupFilePath))
				{
					viewModel.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup);

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

				await UpdateDatabaseAsync(
					parameters,
					viewModel,
					token).ConfigureAwait(false);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(dek);
			}
		}
		finally
		{
			viewModel.IsActionInProgress = false;
		}
	}

	/// <inheritdoc />
	public bool EncryptSessionContents(
		byte[] decryptedContents,
		byte[] sessionEncryptedDek,
		out byte[] encryptedContents)
	{
		encryptedContents = [];

		if (!_encryption.Decrypt(
			sessionEncryptedDek,
			GetSessionId(),
			out byte[] decryptedDek))
		{
			return false;
		}

		try
		{
			return _encryption.Encrypt(
				decryptedContents,
				decryptedDek,
				out encryptedContents);
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
	public async Task HideFileContentsAsync(
		FileModelDto file,
		EditorViewModel viewModel,
		CancellationToken token = default)
	{
		if (file.IsEdited || file.IsExecuted)
		{
			if (!await viewModel
				.RequestUserCloseFilesAsync(token)
				.ConfigureAwait(true))
			{
				return;
			}

			viewModel.CloseFile(file);
		}

		file.EncryptionStatus = EncryptionStatus.Encrypted;
	}

	/// <inheritdoc />
	public async Task HideFolderContentsAsync(
		FolderModelDto folder,
		EditorViewModel viewModel,
		CancellationToken token = default)
	{
		if (folder.AnyFile(x => x.IsEdited || x.IsExecuted))
		{
			if (!await viewModel
				.RequestUserCloseFilesAsync(token)
				.ConfigureAwait(true))
			{
				return;
			}

			viewModel.CloseFiles(
				folder.GetFiles(x => x.IsEdited),
				folder.GetFiles(x => x.IsExecuted));
		}

		if (folder.SessionEncryptedDek is not null)
		{
			CryptographicOperations.ZeroMemory(folder.SessionEncryptedDek);

			folder.SessionEncryptedDek = null;
		}

		folder
			.ToEnumerable()
			.Concat(folder.GetAllChildren())
			.ForEach(x => x.EncryptionStatus = EncryptionStatus.Encrypted);

		if (viewModel
			.Hierarchy
			.ContainsBy(x => x.EncryptionStatus == EncryptionStatus.Decrypted))
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
	public async Task ShowFileContentsAsync(
		FileModelDto file,
		EditorViewModel viewModel,
		CancellationToken token = default)
	{
		if (file.FindParent(x => x.IsPasswordKeeper()) is not { } root
			|| root.PasswordHash is null
			|| root.EncryptedDek is null)
		{
			return;
		}

		if (await RequestUserPasswordAsync(Strings.Password, token).ConfigureAwait(false) is not { } password)
		{
			return;
		}

		try
		{
			viewModel.IsActionInProgress = true;

			if (!_encryption.EnhancedVerify(password, root.PasswordHash))
			{
				viewModel.ShowErrorSnackbar(Strings.IncorrectPassword);

				return;
			}

			if (!_encryption.Decrypt(
				root.EncryptedDek,
				TextHelper.Utf8Encoding.GetBytes(password),
				out byte[] dek))
			{
				return;
			}

			try
			{
				if (!_encryption.Encrypt(
					dek,
					GetSessionId(),
					out byte[] sessionEncryptedDek))
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
			viewModel.IsActionInProgress = false;
		}
	}

	/// <inheritdoc />
	public async Task ShowFolderContentsAsync(
		FolderModelDto folder,
		EditorViewModel viewModel,
		CancellationToken token = default)
	{
		// TODO: Make test
		if (folder.PasswordHash is null)
		{
			return;
		}

		FileModelDto[] files = [.. folder
			.Children
			.GetFiles()];

		if (files.Length == 0)
		{
			viewModel.ShowInfoSnackbar(Strings.MissingFiles);

			return;
		}

		if (!AreFilesValid(files, viewModel))
		{
			return;
		}

		if (await RequestUserPasswordAsync(Strings.Password, token).ConfigureAwait(false) is not { } password)
		{
			return;
		}

		try
		{
			viewModel.IsActionInProgress = true;

			if (!_encryption.EnhancedVerify(password, folder.PasswordHash))
			{
				viewModel.ShowErrorSnackbar(Strings.IncorrectPassword);

				return;
			}

			if (ShowFolderContents(folder, password))
			{
				return;
			}

			viewModel.ShowErrorSnackbar(Strings.FailedToShowFileContents);
		}
		finally
		{
			viewModel.IsActionInProgress = false;
		}
	}

	/// <inheritdoc />
	public async Task UpdateDatabaseAsync(
		UpdateDatabaseParameters parameters,
		EditorViewModel viewModel,
		CancellationToken token = default)
	{
		try
		{
			// TODO: Make test
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
				viewModel.ShowErrorSnackbar(Strings.FailedToProcessContents);

				await _dbAccess
					.RestoreFromBackupAsync(parameters.BackupFilePath, token)
					.ConfigureAwait(false);

				DeleteDatabaseBackupFile(parameters.BackupFilePath);

				return;
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
				viewModel.ShowErrorSnackbar(Strings.FailedToProcessContents);

				await _dbAccess
					.RestoreFromBackupAsync(parameters.BackupFilePath, token)
					.ConfigureAwait(false);

				DeleteDatabaseBackupFile(parameters.BackupFilePath);

				return;
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
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
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
			&& !contents.Any(x => x.Id.IsDefault());
	}

	/// <summary>
	/// Returns <c>True</c> if the files are valid.
	/// </summary>
	private static bool AreFilesValid(FileModelDto[] files, EditorViewModel viewModel)
	{
		if (files.Any(x => x.IsEdited || x.IsExecuted))
		{
			viewModel.ShowInfoSnackbar(Strings.YouMustCloseTheFilesYouAreEditing);

			return false;
		}

		return true;
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
	/// Requests a password from user.
	/// </summary>
	private async Task<string?> RequestUserPasswordAsync(string label, CancellationToken token = default)
	{
		_logger.LogInformation("Show password box");

		PasswordBox view = _viewFactory.CreateUserControl<PasswordBox>();

		view
			.ViewModel
			.Label = label;

		if (!AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			_ = DialogHost.Show(view);
		}

		if (!await view
			.ViewModel
			.GetResultAsync(token: token)
			.ConfigureAwait(false) || view.ViewModel.Password is null)
		{
			return null;
		}

		try
		{
			return view
				.ViewModel
				.Password;
		}
		finally
		{
			view
				.ViewModel
				.Password = null;
		}
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
			|| !_encryption.Decrypt(root.EncryptedDek, TextHelper.Utf8Encoding.GetBytes(password), out byte[] dek)
			|| !_encryption.Encrypt(dek, GetSessionId(), out byte[] sessionEncryptedDek))
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
