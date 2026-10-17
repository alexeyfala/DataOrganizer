using DataOrganizer.Dto.Encryption;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Diagnostics;
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

/// <inheritdoc cref="IFolderCipher" />
public sealed class FolderCipher : IFolderCipher
{
	#region Data
	/// <inheritdoc cref="IContentVisibility" />
	private readonly IContentVisibility _contentVisibility;

	/// <inheritdoc cref="IEncryptedContentWriter" />
	private readonly IEncryptedContentWriter _contentWriter;

	/// <inheritdoc cref="IFolderContentsConverter" />
	private readonly IFolderContentsConverter _converter;

	/// <inheritdoc cref="IDbBackupService" />
	private readonly IDbBackupService _dbBackupService;

	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IEncryptionFailureReporter" />
	private readonly IEncryptionFailureReporter _encryptionFailureReporter;

	/// <inheritdoc cref="IKeeperDekWriter" />
	private readonly IKeeperDekWriter _keeperDekWriter;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public FolderCipher(
		IContentVisibility contentVisibility,
		IEncryptedContentWriter contentWriter,
		IFolderContentsConverter converter,
		IDbBackupService dbBackupService,
		IDbFailureReporter dbFailureReporter,
		IEncryptionService encryption,
		IEncryptionFailureReporter encryptionFailureReporter,
		IKeeperDekWriter keeperDekWriter,
		INotificationService notification)
	{
		_contentVisibility = contentVisibility;

		_contentWriter = contentWriter;

		_converter = converter;

		_dbBackupService = dbBackupService;

		_dbFailureReporter = dbFailureReporter;

		_encryption = encryption;

		_encryptionFailureReporter = encryptionFailureReporter;

		_keeperDekWriter = keeperDekWriter;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task ChangePasswordAsync(
		FolderDto folder,
		PinnedBuffer dek,
		PinnedSecret newPassword,
		CancellationToken token = default)
	{
		try
		{
			using PinnedBuffer newPasswordBinary = newPassword.ToUtf8Buffer();

			byte[] encryptedDek = _encryption.Encrypt(
				dek,
				newPasswordBinary,
				ContentIdentity.Dek);

			if (!await _keeperDekWriter
				.WriteAsync(folder, encryptedDek, token)
				.ConfigureAwait(false))
			{
				return;
			}

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
		PinnedBuffer dek,
		CancellationToken token = default)
	{
		FolderConversion? conversion = null;

		// The notes reach the objects only on a done conversion; until then their plain text is ours to erase.
		bool areNotesHandedOver = false;

		try
		{
			conversion = await _converter.ConvertAsync(
				new()
				{
					Dek = dek,
					Encrypt = false,
					Files = files,
					Folder = folder
				},
				token).ConfigureAwait(false);

			if (conversion is null)
			{
				return;
			}

			using DatabaseBackup? backup = await _dbBackupService
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
		PinnedSecret password,
		CancellationToken token = default)
	{
		try
		{
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
				using DatabaseBackup? backup = await _dbBackupService
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

				await _contentWriter
					.UpdateDatabaseAsync(parameters, token)
					.ConfigureAwait(false);
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
