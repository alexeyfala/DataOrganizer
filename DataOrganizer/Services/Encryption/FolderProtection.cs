using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Encryption;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Dialogs;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notifications;
using Repository.Dto;
using Repository.Interfaces.Database;
using Repository.Services.Database;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Encryption;

public sealed class FolderProtection : IFolderProtection
{
	#region Data
	/// <inheritdoc cref="IContentVisibility" />
	private readonly IContentVisibility _contentVisibility;

	/// <inheritdoc cref="IEncryptedContentWriter" />
	private readonly IEncryptedContentWriter _contentWriter;

	/// <inheritdoc cref="IFolderContentsConverter" />
	private readonly IFolderContentsConverter _converter;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IEncryptionFailureReporter" />
	private readonly IEncryptionFailureReporter _encryptionFailureReporter;

	/// <inheritdoc cref="IKeeperUnlocker" />
	private readonly IKeeperUnlocker _keeperUnlocker;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public FolderProtection(
		IContentVisibility contentVisibility,
		IEncryptedContentWriter contentWriter,
		IFolderContentsConverter converter,
		IDbAccess dbAccess,
		IDbFailureReporter dbFailureReporter,
		IDialogService dialogService,
		IEncryptionService encryption,
		IEncryptionFailureReporter encryptionFailureReporter,
		IKeeperUnlocker keeperUnlocker,
		IMessenger messenger,
		INotificationService notification)
	{
		_contentVisibility = contentVisibility;

		_contentWriter = contentWriter;

		_converter = converter;

		_dbAccess = dbAccess;

		_dbFailureReporter = dbFailureReporter;

		_dialogService = dialogService;

		_encryption = encryption;

		_encryptionFailureReporter = encryptionFailureReporter;

		_keeperUnlocker = keeperUnlocker;

		_messenger = messenger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task ChangePasswordAsync(FolderDto folder, CancellationToken token = default)
	{
		if (folder.EncryptedDek is null)
		{
			return;
		}

		using PinnedBuffer? dek = await _keeperUnlocker.RequestDekAsync(
			keeper: folder,
			header: Strings.ChangePassword,
			label: Strings.OldPassword,
			token: token).ConfigureAwait(false);

		if (dek is null)
		{
			return;
		}

		try
		{
			using PinnedSecret newPassword = await _dialogService.RequestPasswordAsync(
				header: Strings.ChangePassword,
				label: Strings.NewPassword,
				mode: PasswordPromptMode.Create,
				token: token).ConfigureAwait(false);

			if (newPassword.IsEmpty)
			{
				return;
			}

			using PinnedBuffer newPasswordBinary = newPassword.ToUtf8Buffer();

			byte[] encryptedDek = _encryption.Encrypt(
				dek,
				newPasswordBinary,
				ContentIdentity.Dek);

			if (!await _dbAccess.UpdateFolderPropertiesAsync(folder.Id,
				[
					x => x.SetProperty(x => x.EncryptedDek, encryptedDek)
				], token).ConfigureAwait(false))
			{
				return;
			}

			folder.EncryptedDek = encryptedDek;

			_notification.ShowInformationSnackbar(Strings.PasswordChanged);
		}
		catch (Exception ex) when (EncryptionFailures.IsCryptographic(ex))
		{
			_encryptionFailureReporter.Report(ex);
		}
		catch (Exception ex)
		{
			_dbFailureReporter.Report(ex, Strings.FailedToChangePassword);
		}
	}

	/// <inheritdoc />
	public async Task DecryptFolderAsync(
		FolderDto folder,
		FileDto[] files,
		CancellationToken token = default)
	{
		if (folder.EncryptedDek is null)
		{
			return;
		}

		// Unwrapping is the password check, so a wrong password never pulls the contents into memory.
		using PinnedBuffer? decryptedDek = await _keeperUnlocker.RequestDekAsync(
			keeper: folder,
			header: Strings.DecryptFiles,
			token: token).ConfigureAwait(false);

		if (decryptedDek is null)
		{
			return;
		}

		FolderConversion? conversion = null;

		// The notes reach the objects only on a done conversion; until then their plain text is ours to erase.
		bool areNotesHandedOver = false;

		try
		{
			using ProgressScope _ = _messenger.ShowProgress();

			conversion = await _converter.ConvertAsync(
				new()
				{
					Dek = decryptedDek,
					Encrypt = false,
					Files = files,
					Folder = folder
				},
				token).ConfigureAwait(false);

			if (conversion is null)
			{
				return;
			}

			using DatabaseBackup? backup = await _dbAccess
				.CreateBackupAsync(token)
				.ConfigureAwait(false);

			if (backup is null)
			{
				_notification.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup);

				return;
			}

			UpdateDatabaseParameters parameters = new()
			{
				BackupFilePath = backup.FilePath,
				Contents = conversion.Converted,
				EncryptedDek = null,
				Files = files,
				Folder = folder,
				NewStatus = EncryptionStatus.None,
				Notes = conversion.Notes
			};

			if (await _contentWriter
				.UpdateDatabaseAsync(parameters, token)
				.ConfigureAwait(false) is not UpdateDatabaseOutcome.Saved)
			{
				return;
			}

			_contentVisibility.DiscardKeys(folder);

			areNotesHandedOver = true;
		}
		catch (Exception ex) when (EncryptionFailures.IsCryptographic(ex))
		{
			_encryptionFailureReporter.Report(ex);
		}
		catch (Exception ex)
		{
			_dbFailureReporter.Report(ex, Strings.FailedToProcessContents);
		}
		finally
		{
			// Whatever the outcome, the decrypted contents have no reader left here.
			WipeContents(conversion?.Converted ?? []);

			if (!areNotesHandedOver)
			{
				WipeNotes(conversion?.Notes ?? []);
			}
		}
	}

	/// <inheritdoc />
	public async Task EncryptFolderAsync(
		FolderDto folder,
		FileDto[] files,
		CancellationToken token = default)
	{
		using PinnedSecret password = await _dialogService.RequestPasswordAsync(
			header: Strings.EncryptFiles,
			mode: PasswordPromptMode.Create,
			token: token).ConfigureAwait(false);

		if (password.IsEmpty)
		{
			return;
		}

		try
		{
			using ProgressScope _ = _messenger.ShowProgress();

			using PinnedBuffer dek = _encryption.CreateRandomDek();

			FolderConversion? conversion = await _converter.ConvertAsync(
				new()
				{
					Dek = dek,
					Encrypt = true,
					Files = files,
					Folder = folder
				},
				token).ConfigureAwait(false);

			if (conversion is null)
			{
				return;
			}

			// The inner scope erases the plain text while a failure is still on its way out,
			// so nothing of it is left while the failure is being reported.
			try
			{
				using PinnedBuffer passwordBinary = password.ToUtf8Buffer();

				byte[] encryptedDek = _encryption.Encrypt(
					dek,
					passwordBinary,
					ContentIdentity.Dek);

				// The copy insures the one irreversible operation against a bug in the conversion,
				// and holds the contents in plain text until the operation ends.
				using DatabaseBackup? backup = await _dbAccess
					.CreateBackupAsync(token)
					.ConfigureAwait(false);

				if (backup is null)
				{
					_notification.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup);

					return;
				}

				UpdateDatabaseParameters parameters = new()
				{
					BackupFilePath = backup.FilePath,
					Contents = conversion.Converted,
					EncryptedDek = encryptedDek,
					Files = files,
					Folder = folder,
					NewStatus = EncryptionStatus.Encrypted,
					Notes = conversion.Notes
				};

				if (await _contentWriter
					.UpdateDatabaseAsync(parameters, token)
					.ConfigureAwait(false) is not UpdateDatabaseOutcome.Saved)
				{
					return;
				}
			}
			finally
			{
				WipeContents(conversion.Loaded);
			}
		}
		catch (Exception ex) when (EncryptionFailures.IsCryptographic(ex))
		{
			_encryptionFailureReporter.Report(ex);
		}
		catch (Exception ex)
		{
			_dbFailureReporter.Report(ex, Strings.FailedToProcessContents);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Overwrites the buffers of the given contents.
	/// </summary>
	private static void WipeContents(ValidatedContents[] contents)
	{
		contents.ForEach(x => x.Contents.ZeroMemory());
	}

	/// <summary>
	/// Overwrites the buffers of the given notes.
	/// </summary>
	private static void WipeNotes(IEnumerable<NoteUpdate> notes) => notes.ForEach(x => x.Note.ZeroMemory());
	#endregion
}
