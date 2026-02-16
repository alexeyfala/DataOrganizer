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
	public bool DecryptContents(
		byte[] input,
		byte[] encryptedPassword,
		out byte[] output)
	{
		output = [];

		if (!_encryption.Decrypt(
			encryptedPassword,
			GetSessionId(),
			out byte[] decryptedPassword))
		{
			return false;
		}

		try
		{
			if (!_encryption.Decrypt(
				input,
				decryptedPassword,
				out byte[] decryptedContents))
			{
				return false;
			}

			output = decryptedContents;

			return true;
		}
		finally
		{
			CryptographicOperations.ZeroMemory(decryptedPassword);
		}
	}

	/// <inheritdoc />
	public bool EncryptContents(
		byte[] input,
		byte[] encryptedPassword,
		out byte[] output)
	{
		output = [];

		if (!_encryption.Decrypt(
			encryptedPassword,
			GetSessionId(),
			out byte[] decryptedPassword))
		{
			return false;
		}

		try
		{
			if (!_encryption.Encrypt(
				input,
				decryptedPassword,
				out byte[] encryptedContents))
			{
				return false;
			}

			output = encryptedContents;

			return true;
		}
		finally
		{
			CryptographicOperations.ZeroMemory(decryptedPassword);
		}
	}

	/// <inheritdoc />
	public async Task<FilesEncryptionResult> EncryptDecryptAsync(
		EditorViewModel viewModel,
		EncryptDecryptFilesParameters parameters,
		CancellationToken token = default)
	{
		try
		{
			if (parameters.Action == CryptoAction.ShowFileContents)
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

				return FilesEncryptionResult.FailedToLoadContents;
			}

			ContentsIsValidPair[] result = [.. _encryption.EncryptDecryptContents(
				contents,
				TextHelper.Utf8Encoding.GetBytes(parameters.Password),
				parameters.Action)];

			if (result.Length != contents.Length
				|| result.Any(x => !x.IsValid)
				|| result.Any(x => x.Id.IsDefault()))
			{
				viewModel.ShowErrorSnackbar(Strings.FailedToProcessContents);

				return FilesEncryptionResult.FailedToEncryptContents;
			}

			if (!_dbAccess.BackupDatabase(out var backupFilePath) || string.IsNullOrEmpty(backupFilePath))
			{
				viewModel.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup);

				return FilesEncryptionResult.UnableToCreateDatabaseBackup;
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

				return FilesEncryptionResult.FailedToSaveContents;
			}

			string? passwordHash = parameters.Action switch
			{
				CryptoAction.Encrypt => _encryption.EnhancedHashPassword(parameters.Password),
				CryptoAction.Decrypt => null,
				_ => throw new NotImplementedException()
			};

			if (!await _dbAccess.UpdatePropertyAsync(
				id: parameters.Folder.Id,
				propertyName: nameof(FolderModel.PasswordHash),
				value: passwordHash,
				token: token).ConfigureAwait(false))
			{
				await RestoreDatabaseAsync().ConfigureAwait(false);

				DeleteBackupFile();

				return FilesEncryptionResult.FailedToSavePasswordHash;
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

			return FilesEncryptionResult.Done;

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

			return FilesEncryptionResult.ExceptionThrown;
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
		HandlePasswordInputParameters parameters,
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

			if (parameters.Action == CryptoAction.ShowFileContents)
			{
				if (!ShowFileContents(parameters.Folder, password))
				{
					viewModel.ShowErrorSnackbar(Strings.FailedToShowFileContents);

					return HandlePasswordResult.FailedToShowFileContents;
				}
			}
			else
			{
				await EncryptDecryptAsync(
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
	public void HideFolderContents(FolderModelDto folder)
	{
		if (folder.EncryptedPassword is not null)
		{
			CryptographicOperations.ZeroMemory(folder.EncryptedPassword);

			folder.EncryptedPassword = null;
		}

		folder
			.ToEnumerable()
			.Concat(folder.GetAllChildren())
			.ForEach(x => x.EncryptionStatus = EncryptionStatus.Encrypted);
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
	public async Task TakePasswordAsync(
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

		if (!await view
			.ViewModel
			.GetResultAsync(token)
			.ConfigureAwait(false))
		{
			return;
		}

		HandlePasswordInputParameters parameters = new()
		{
			Action = action,
			Files = filesDto,
			Folder = folder
		};

		try
		{
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
	#endregion

	#region Service
	/// <summary>
	/// Shows file contents.
	/// </summary>
	private bool ShowFileContents(
		FolderModelDto folder,
		string password)
	{
		FolderModelDto? root = !string.IsNullOrEmpty(folder.PasswordHash)
			? folder
			: folder.FindParent(x => !string.IsNullOrEmpty(x.PasswordHash));

		if (root is null)
		{
			return false;
		}

		bool isEncrypted = _encryption.Encrypt(
			TextHelper.Utf8Encoding.GetBytes(password),
			GetSessionId(),
			out byte[] output);

		if (!isEncrypted)
		{
			return false;
		}

		root.EncryptedPassword = output;

		folder
			.ToEnumerable()
			.Concat(folder.GetAllChildren())
			.ForEach(x => x.EncryptionStatus = EncryptionStatus.Decrypted);

		return true;
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

			case CryptoAction.ShowFileContents:
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
