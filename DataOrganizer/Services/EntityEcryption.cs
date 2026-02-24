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
		// TODO: Check if there are opened or executed files
		if (folder.EncryptedDek is null || folder.PasswordHash is null)
		{
			return;
		}

		PasswordBox view = _viewFactory.CreateUserControl<PasswordBox>();

		view
			.ViewModel
			.Label = Strings.OldPassword;

		_ = DialogHost.Show(view);

		if (!await view
			.ViewModel
			.GetResultAsync(token: token)
			.ConfigureAwait(true) || view.ViewModel.Password is not { } oldPassword)
		{
			return;
		}

		if (!_encryption.EnhancedVerify(oldPassword, folder.PasswordHash))
		{
			viewModel.ShowErrorSnackbar(Strings.IncorrectPassword);

			return;
		}

		view
			.ViewModel
			.Password = null;

		view = _viewFactory.CreateUserControl<PasswordBox>();

		view
			.ViewModel
			.Label = Strings.NewPassword;

		_ = DialogHost.Show(view);

		if (!await view
			.ViewModel
			.GetResultAsync(token: token)
			.ConfigureAwait(false) || view.ViewModel.Password is not { } newPassword)
		{
			return;
		}

		view
			.ViewModel
			.Password = null;

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
	public async Task<FolderEncryptionResult> EncryptDecryptFolderAsync(
		EditorViewModel viewModel,
		FolderEncryptionParameters parameters,
		CancellationToken token = default)
	{
		try
		{
			if (parameters.Action == CryptoAction.ShowFolderContents)
			{
				throw new InvalidOperationException("Unsupported action type");
			}

			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(parameters.Files.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			if (contents.Length != parameters.Files.Length || contents.Any(x => !x.IsValid))
			{
				viewModel.ShowErrorSnackbar(Strings.FailedToLoadFilesContents);

				return FolderEncryptionResult.FailedToLoadContents;
			}

			ContentsIsValidPair[] result = [];

			byte[]? encryptedDek = null;

			switch (parameters.Action)
			{
				case CryptoAction.Encrypt:
					byte[] dek = _encryption.CreateRandomDek();

					result = [.. _encryption.EncryptDecryptContents(
						contents,
						[.. dek],
						parameters.Action)];

					if (!_encryption.Encrypt(
						dek,
						TextHelper.Utf8Encoding.GetBytes(parameters.Password),
						out encryptedDek))
					{
						return FolderEncryptionResult.FailedToEncryptContents;
					}

					CryptographicOperations.ZeroMemory(dek);
					break;

				case CryptoAction.Decrypt:
					if (parameters.Folder.EncryptedDek is null)
					{
						return FolderEncryptionResult.FailedToDecryptContents;
					}

					if (!_encryption.Decrypt(
						parameters.Folder.EncryptedDek,
						TextHelper.Utf8Encoding.GetBytes(parameters.Password),
						out byte[] decryptedDek))
					{
						return FolderEncryptionResult.FailedToDecryptContents;
					}

					try
					{
						result = [.. _encryption.EncryptDecryptContents(
							contents,
							decryptedDek,
							parameters.Action)];
					}
					finally
					{
						CryptographicOperations.ZeroMemory(decryptedDek);
					}
					break;

				default:
					throw new InvalidOperationException("Unsupported action type");
			}

			if (result.Length != contents.Length
				|| result.Any(x => !x.IsValid)
				|| result.Any(x => x.Id.IsDefault()))
			{
				viewModel.ShowErrorSnackbar(Strings.FailedToProcessContents);

				return FolderEncryptionResult.FailedToEncryptContents;
			}

			if (!_dbAccess.BackupDatabase(out string? backupFilePath))
			{
				viewModel.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup);

				return FolderEncryptionResult.UnableToCreateDatabaseBackup;
			}

			DateTime updatedDate = DateTime.Now;

			Dictionary<Guid, PropertyNameValuePair[]> relations = result.ToDictionary(x => x.Id, x =>
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
				await RestoreDatabaseAsync().ConfigureAwait(false);

				DeleteBackupFile();

				return FolderEncryptionResult.FailedToSaveContents;
			}

			string? passwordHash = parameters.Action switch
			{
				CryptoAction.Encrypt => _encryption.EnhancedHashPassword(parameters.Password),
				CryptoAction.Decrypt => null,
				_ => throw new NotImplementedException()
			};

			PropertyNameValuePair[] properties =
			[
				new PropertyNameValuePair(nameof(FolderModel.PasswordHash), passwordHash),
				new PropertyNameValuePair(nameof(FolderModel.EncryptedDek), encryptedDek)
			];

			if (!await _dbAccess.UpdatePropertiesAsync(
				id: parameters.Folder.Id,
				token: token,
				properties: properties).ConfigureAwait(false))
			{
				await RestoreDatabaseAsync().ConfigureAwait(false);

				DeleteBackupFile();

				return FolderEncryptionResult.FailedToSavePasswordHash;
			}

			ExplorerModelBaseDto[] objects =
			[
				.. parameters.Folder.ToEnumerable(),
				.. parameters.Folder.Children.GetFolders(),
				.. parameters.Files
			];

			EncryptionStatus newStatus = parameters.Action switch
			{
				CryptoAction.Encrypt => EncryptionStatus.Encrypted,
				CryptoAction.Decrypt => EncryptionStatus.None,
				_ => throw new NotImplementedException()
			};

			objects.ForEach(x => x.EncryptionStatus = newStatus);

			parameters
				.Folder
				.PasswordHash = passwordHash;

			parameters
				.Folder
				.EncryptedDek = encryptedDek;

			DeleteBackupFile();

			string doneAction = parameters.Action switch
			{
				CryptoAction.Encrypt => "encrypted",
				CryptoAction.Decrypt => "decrypted",
				_ => throw new NotImplementedException()
			};

			_logger.LogInformation(
				$"{parameters.Files.Length} files {doneAction} in folder:{parameters.Folder.GetPropertyValues(
					true,
					nameof(FolderModelDto.Id),
					nameof(FolderModelDto.Name))}");

			return FolderEncryptionResult.Done;

			async Task RestoreDatabaseAsync()
			{
				viewModel.ShowErrorSnackbar(
					Strings.FailedToProcessContents +
					Environment.NewLine +
					Strings.TheDatabaseWillBeRestored);

				await _dbAccess
					.RestoreFromBackupAsync(backupFilePath, token)
					.ConfigureAwait(false);
			}

			void DeleteBackupFile()
			{
				SqliteConnection.ClearAllPools();

				_fileSystem.EraseAndDeleteFile(backupFilePath);
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return FolderEncryptionResult.ExceptionThrown;
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
	public async Task<HandlePasswordResult> HandlePasswordInputAsync(
		string? password,
		EditorViewModel viewModel,
		HandlePasswordParameters parameters,
		CancellationToken token = default)
	{
		if (string.IsNullOrEmpty(password))
		{
			return HandlePasswordResult.PasswordNotEntered;
		}

		try
		{
			viewModel.IsActionInProgress = true;

			if (parameters.Action != CryptoAction.Encrypt && !VerifyPasswordHash(
				parameters.Folder,
				parameters.Action,
				password))
			{
				viewModel.ShowErrorSnackbar(Strings.IncorrectPassword);

				return HandlePasswordResult.PasswordDoesNotMatch;
			}

			if (parameters.Action == CryptoAction.ShowFolderContents)
			{
				if (!ShowFolderContents(parameters.Folder, password))
				{
					viewModel.ShowErrorSnackbar(Strings.FailedToShowFileContents);

					return HandlePasswordResult.FailedToShowFileContents;
				}
			}
			else
			{
				await EncryptDecryptFolderAsync(
					viewModel,
					parameters.CreateFrom(password),
					token).ConfigureAwait(false);
			}

			return HandlePasswordResult.Applied;
		}
		finally
		{
			viewModel.IsActionInProgress = false;
		}
	}

	/// <inheritdoc />
	public async Task HideFileContentsAsync(
		FileModelDto file,
		EditorViewModel viewModel,
		CancellationToken token = default)
	{
		if (file.IsEdited || file.IsExecuted)
		{
			YesNoCancelBox view = _viewFactory.CreateUserControl<YesNoCancelBox>();

			view
				.ViewModel
				.Text = $"{Strings.CloseFilesBeingEdited}?";

			if (!AppDomain
				.CurrentDomain
				.IsRunningFromNUnit())
			{
				_ = DialogHost.Show(view);
			}

			YesNoCancelResult result = await view
				.ViewModel
				.GetResultAsync(YesNoCancelVariant.YesCancel, token)
				.ConfigureAwait(true);

			if (result != YesNoCancelResult.Yes)
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
			YesNoCancelBox view = _viewFactory.CreateUserControl<YesNoCancelBox>();

			view
				.ViewModel
				.Text = $"{Strings.CloseFilesBeingEdited}?";

			if (!AppDomain
				.CurrentDomain
				.IsRunningFromNUnit())
			{
				_ = DialogHost.Show(view);
			}

			YesNoCancelResult result = await view
				.ViewModel
				.GetResultAsync(YesNoCancelVariant.YesCancel, token)
				.ConfigureAwait(true);

			if (result != YesNoCancelResult.Yes)
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
	public async Task RequestPasswordAsync(
		EditorViewModel viewModel,
		FolderModelDto folder,
		CryptoAction action,
		CancellationToken token = default)
	{
		FileModelDto[] filesDto = [.. folder
			.Children
			.GetFiles()];

		if (filesDto.Length == 0)
		{
			viewModel.ShowInfoSnackbar(Strings.MissingFiles);

			return;
		}

		if (filesDto.Any(x => x.IsEdited || x.IsExecuted))
		{
			viewModel.ShowInfoSnackbar(Strings.YouMustCloseTheFilesYouAreEditing);

			return;
		}

		_logger.LogInformation("Show password box");

		PasswordBox view = _viewFactory.CreateUserControl<PasswordBox>();

		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		_ = DialogHost.Show(view);

		try
		{
			if (!await view
				.ViewModel
				.GetResultAsync(token: token)
				.ConfigureAwait(false))
			{
				return;
			}

			HandlePasswordParameters parameters = new()
			{
				Action = action,
				Files = filesDto,
				Folder = folder
			};

			await HandlePasswordInputAsync(
				view.ViewModel.Password,
				viewModel,
				parameters,
				token).ConfigureAwait(false);
		}
		finally
		{
			view
				.ViewModel
				.Password = null;
		}
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

		PasswordBox view = _viewFactory.CreateUserControl<PasswordBox>();

		if (!AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			_ = DialogHost.Show(view);
		}

		try
		{
			if (!await view
				.ViewModel
				.GetResultAsync(token: token)
				.ConfigureAwait(false) || view.ViewModel.Password is null)
			{
				return;
			}

			viewModel.IsActionInProgress = true;

			if (!_encryption.EnhancedVerify(view.ViewModel.Password, root.PasswordHash))
			{
				viewModel.ShowErrorSnackbar(Strings.IncorrectPassword);

				return;
			}

			if (!_encryption.Decrypt(
				root.EncryptedDek,
				TextHelper.Utf8Encoding.GetBytes(view.ViewModel.Password),
				out byte[] decryptedDek))
			{
				return;
			}

			try
			{
				if (!_encryption.Encrypt(
					decryptedDek,
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
				CryptographicOperations.ZeroMemory(decryptedDek);
			}
		}
		finally
		{
			view
				.ViewModel
				.Password = null;

			viewModel.IsActionInProgress = false;
		}
	}
	#endregion

	#region Service
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
			|| !_encryption.Decrypt(root.EncryptedDek, TextHelper.Utf8Encoding.GetBytes(password), out byte[] output)
			|| !_encryption.Encrypt(output, GetSessionId(), out byte[] sessionEncryptedDek))
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
			CryptographicOperations.ZeroMemory(output);
		}
	}

	/// <summary>
	/// Verifies the password by hash.
	/// </summary>
	private bool VerifyPasswordHash(
		FolderModelDto folder,
		CryptoAction action,
		string password)
	{
		string? passwordHash = folder.PasswordHash;

		switch (action)
		{
			case CryptoAction.Decrypt when !string.IsNullOrEmpty(passwordHash):
				return _encryption.EnhancedVerify(password, passwordHash);

			case CryptoAction.ShowFolderContents:
				if (!string.IsNullOrEmpty(passwordHash))
				{
					return _encryption.EnhancedVerify(password, passwordHash);
				}
				else if (folder.FindParent(x => !string.IsNullOrEmpty(x.PasswordHash)) is { } parent && !string.IsNullOrEmpty(parent.PasswordHash))
				{
					return _encryption.EnhancedVerify(password, parent.PasswordHash);
				}
				else
				{
					return false;
				}
		}

		return true;
	}
	#endregion
}
