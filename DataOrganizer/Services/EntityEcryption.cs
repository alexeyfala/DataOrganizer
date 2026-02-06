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
	public async Task<FilesEncryptionResult> EncryptDecryptAsync(
		EditorViewModel viewModel,
		EncryptDecryptFilesParameters parameters,
		CancellationToken token = default)
	{
		try
		{
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
				viewModel.ShowErrorSnackbar(Strings.FailedToEncryptFilesContents);

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
					Strings.FailedToEncryptFilesContents +
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
	public async Task<PasswordMatchResult> HandlePasswordInputAsync(
		PasswordBox view,
		EditorViewModel viewModel,
		HandlePasswordInputParameters inputParameters,
		CancellationToken token = default)
	{
		DialogOverlayPopupHost? popupHost = view.FindLogicalParent<DialogOverlayPopupHost>();

		try
		{
			if (!AppDomain
				.CurrentDomain
				.IsRunningFromNUnit())
			{
				DialogHost.Close(null);
			}

			if (view
				.ViewModel
				.Password is not { } password)
			{
				return PasswordMatchResult.NotEntered;
			}

			try
			{
				Func<bool> condition = () => popupHost?.IsActuallyOpen == false;

				await condition
					.WaitAsync(300, 10, token)
					.ConfigureAwait(false);

				viewModel.IsActionInProgress = true;

				if (inputParameters.Action == CryptoAction.Decrypt
					&& inputParameters.Folder.PasswordHash is { } passwordHash
					&& !_encryption.EnhancedVerify(password, passwordHash))
				{
					viewModel.ShowErrorSnackbar(Strings.IncorrectPassword);

					return PasswordMatchResult.DoesNotMatch;
				}

				EncryptDecryptFilesParameters parameters = new()
				{
					Action = inputParameters.Action,
					Files = inputParameters.Files,
					Folder = inputParameters.Folder,
					Password = password,
				};

				await EncryptDecryptAsync(
					viewModel,
					parameters,
					token).ConfigureAwait(false);

				return PasswordMatchResult.Matches;
			}
			finally
			{
				viewModel.IsActionInProgress = false;
			}
		}
		finally
		{
			popupHost = null;
		}
	}

	/// <inheritdoc />
	public Task TakeCryptPasswordAsync(
		EditorViewModel viewModel,
		TakeCryptPasswordParameters inputParameters,
		CancellationToken token = default)
	{
		FileModelDto[] filesDto = [.. inputParameters
			.Folder
			.Children
			.GetFiles()];

		if (filesDto.Length == 0)
		{
			viewModel.ShowInfoSnackbar(inputParameters.Action switch
			{
				CryptoAction.Encrypt => Strings.ThereAreNoFilesToEncrypt,
				CryptoAction.Decrypt => Strings.ThereAreNoFilesToDecrypt,
				_ => throw new NotImplementedException()
			});

			return Task.CompletedTask;
		}

		if (filesDto.Any(x => x.IsEdited || x.IsExecuted))
		{
			viewModel.ShowInfoSnackbar(Strings.YouMustCloseTheFilesYouAreEditing);

			return Task.CompletedTask;
		}

		_logger.LogInformation("Show password box");

		PasswordBox view = _viewFactory.CreateUserControl<PasswordBox>();

		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return Task.CompletedTask;
		}

		view
			.ViewModel
			.DefaultPressedCallback = () =>
			{
				HandlePasswordInputParameters parameters = new()
				{
					Action = inputParameters.Action,
					Files = filesDto,
					Folder = inputParameters.Folder
				};

				return HandlePasswordInputAsync(
					view,
					viewModel,
					parameters,
					token);
			};

		return DialogHost.Show(view);
	}
	#endregion
}
