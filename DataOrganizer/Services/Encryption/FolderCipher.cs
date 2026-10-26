using DataOrganizer.Dto.Encryption;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using Diagnostics.Interfaces;
using FileSystemOperations.Interfaces;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Notifications.Interfaces;
using Repository.Dto;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TextProcessing.Extensions;

namespace DataOrganizer.Services.Encryption;

/// <inheritdoc cref="IFolderCipher" />
public sealed class FolderCipher : IFolderCipher
{
	#region Data
	/// <inheritdoc cref="IBackedUpWriter" />
	private readonly IBackedUpWriter _backedUpWriter;

	/// <inheritdoc cref="IContentKeys" />
	private readonly IContentKeys _contentKeys;

	/// <inheritdoc cref="IEncryptedContentWriter" />
	private readonly IEncryptedContentWriter _contentWriter;

	/// <inheritdoc cref="IFolderContentsConverter" />
	private readonly IFolderContentsConverter _converter;

	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

	/// <inheritdoc cref="IPasswordDekWrapper" />
	private readonly IPasswordDekWrapper _encryption;

	/// <inheritdoc cref="IEncryptionFailureReporter" />
	private readonly IEncryptionFailureReporter _encryptionFailureReporter;

	/// <inheritdoc cref="IKeeperDekWriter" />
	private readonly IKeeperDekWriter _keeperDekWriter;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public FolderCipher(
		IBackedUpWriter backedUpWriter,
		IContentKeys contentKeys,
		IEncryptedContentWriter contentWriter,
		IFolderContentsConverter converter,
		IDbFailureReporter dbFailureReporter,
		IPasswordDekWrapper encryption,
		IEncryptionFailureReporter encryptionFailureReporter,
		IKeeperDekWriter keeperDekWriter,
		INotificationService notification)
	{
		_backedUpWriter = backedUpWriter;

		_contentKeys = contentKeys;

		_contentWriter = contentWriter;

		_converter = converter;

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
		catch (Exception ex)
		{
			ReportFailure(ex, Strings.FailedToChangePassword);
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

			UpdateDatabaseParameters parameters = new()
			{
				Contents = conversion.Converted,
				EncryptedDek = null,
				Files = files,
				Folder = folder,
				NewStatus = EncryptionStatus.None,
				Notes = conversion.Notes
			};

			if (!await _backedUpWriter
				.WriteAsync(() => SaveAsync(parameters, token), token)
				.ConfigureAwait(false))
			{
				return;
			}

			_contentKeys.DiscardKeys(folder);

			areNotesHandedOver = true;
		}
		catch (Exception ex)
		{
			ReportFailure(ex, Strings.FailedToProcessContents);
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

				UpdateDatabaseParameters parameters = new()
				{
					Contents = conversion.Converted,
					EncryptedDek = encryptedDek,
					Files = files,
					Folder = folder,
					NewStatus = EncryptionStatus.Encrypted,
					Notes = conversion.Notes
				};

				// The copy insures the one irreversible operation against a bug in the conversion,
				// and holds the contents in plain text until the operation ends.
				_ = await _backedUpWriter
					.WriteAsync(() => SaveAsync(parameters, token), token)
					.ConfigureAwait(false);
			}
			finally
			{
				WipeContents(conversion.Loaded);
			}
		}
		catch (Exception ex)
		{
			ReportFailure(ex, Strings.FailedToProcessContents);
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

	/// <summary>
	/// Reports a failure: a cryptographic one to its own reporter, any other as a database failure under
	/// <paramref name="text" />.
	/// </summary>
	private void ReportFailure(
		Exception exception,
		string text,
		[CallerMemberName] string callerName = "")
	{
		if (EncryptionFailures.IsCryptographic(exception))
		{
			// The log names the operation that failed, not this helper.
			_encryptionFailureReporter.Report(exception, callerName);

			return;
		}

		_dbFailureReporter.Report(exception, text);
	}

	/// <summary>
	/// Writes the converted contents; <c>true</c> when they are saved.
	/// </summary>
	private async Task<bool> SaveAsync(UpdateDatabaseParameters parameters, CancellationToken token)
	{
		return await _contentWriter
			.UpdateDatabaseAsync(parameters, token)
			.ConfigureAwait(false) is UpdateDatabaseOutcome.Saved;
	}
	#endregion
}
