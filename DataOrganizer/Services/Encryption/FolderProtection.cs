using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Encryption;
using Repository.DTO;
using Repository.Interfaces;
using Repository.Services;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Encryption;

public sealed class FolderProtection : IFolderProtection
{
	#region Data
	/// <inheritdoc cref="IEncryptedContentWriter" />
	private readonly IEncryptedContentWriter _contentWriter;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IEncryptionFailureReporter" />
	private readonly IEncryptionFailureReporter _failureReporter;

	/// <inheritdoc cref="IKeeperUnlocker" />
	private readonly IKeeperUnlocker _keeperUnlocker;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	public FolderProtection(
		IEncryptedContentWriter contentWriter,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IEncryptionService encryption,
		IEncryptionFailureReporter failureReporter,
		IKeeperUnlocker keeperUnlocker,
		ILogger logger,
		IMessenger messenger)
	{
		_contentWriter = contentWriter;

		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_encryption = encryption;

		_failureReporter = failureReporter;

		_keeperUnlocker = keeperUnlocker;

		_logger = logger;

		_messenger = messenger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task ChangePasswordAsync(FolderModelDto folder, CancellationToken token = default)
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
				ContentIdentity.ForDek(folder.Id));

			if (!await _dbAccess.UpdateFolderPropertiesAsync(folder.Id,
				[
					x => x.SetProperty(x => x.EncryptedDek, encryptedDek)
				], token).ConfigureAwait(false))
			{
				return;
			}

			folder.EncryptedDek = encryptedDek;

			_messenger.ShowSnackbar(Strings.PasswordChanged, SnackbarMessageLevel.Information);
		}
		catch (Exception ex) when (EncryptionFailures.IsCryptographic(ex))
		{
			_failureReporter.Report(ex);
		}
	}

	/// <inheritdoc />
	public async Task DecryptFolderAsync(
		FolderModelDto folder,
		FileModelDto[] files,
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

		try
		{
			using ProgressScope _ = _messenger.ShowProgress();

			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(files.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			if (!AreContentsValid(contents, files.Length))
			{
				_messenger.ShowSnackbar(Strings.FailedToLoadFilesContents, SnackbarMessageLevel.Error);

				return;
			}

			ContentsIsValidPair[] result = [.. _encryption.DecryptContents(contents, decryptedDek)];

			if (!AreContentsValid(result, contents.Length))
			{
				LogInvalidContents(result);

				_messenger.ShowSnackbar(Strings.EncryptedDataIsDamaged, SnackbarMessageLevel.Error);

				return;
			}

			NoteUpdate[] notes = ProcessNotes(
				folder,
				files,
				decryptedDek,
				encrypt: false);

			using DatabaseBackup? backup = await _dbAccess
				.BackupDatabaseAsync(token)
				.ConfigureAwait(false);

			if (backup is null)
			{
				_messenger.ShowSnackbar(Strings.UnableToCreateDatabaseBackup, SnackbarMessageLevel.Error);

				return;
			}

			UpdateDatabaseParameters parameters = new()
			{
				BackupFilePath = backup.FilePath,
				Contents = result,
				EncryptedDek = null,
				Files = files,
				Folder = folder,
				NewStatus = EncryptionStatus.None,
				Notes = notes
			};

			if (await _contentWriter
				.UpdateDatabaseAsync(parameters, token)
				.ConfigureAwait(false) is not UpdateDatabaseResult.Done)
			{
				return;
			}
		}
		catch (Exception ex) when (EncryptionFailures.IsCryptographic(ex))
		{
			_failureReporter.Report(ex);
		}
	}

	/// <inheritdoc />
	public async Task EncryptFolderAsync(
		FolderModelDto folder,
		FileModelDto[] files,
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

			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(files.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			try
			{
				if (!AreContentsValid(contents, files.Length))
				{
					_messenger.ShowSnackbar(Strings.FailedToLoadFilesContents, SnackbarMessageLevel.Error);

					return;
				}

				using PinnedBuffer dek = _encryption.CreateRandomDek();

				ContentsIsValidPair[] result = [.. _encryption.EncryptContents(contents, dek)];

				if (!AreContentsValid(result, contents.Length))
				{
					LogInvalidContents(result);

					_messenger.ShowSnackbar(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

					return;
				}

				using PinnedBuffer passwordBinary = password.ToUtf8Buffer();

				byte[] encryptedDek = _encryption.Encrypt(
					dek,
					passwordBinary,
					ContentIdentity.ForDek(folder.Id));

				NoteUpdate[] notes = ProcessNotes(
					folder,
					files,
					dek,
					encrypt: true);

				// The copy insures the one irreversible operation against a bug in the conversion,
				// and holds the contents in plain text until the operation ends.
				using DatabaseBackup? backup = await _dbAccess
					.BackupDatabaseAsync(token)
					.ConfigureAwait(false);

				if (backup is null)
				{
					_messenger.ShowSnackbar(Strings.UnableToCreateDatabaseBackup, SnackbarMessageLevel.Error);

					return;
				}

				UpdateDatabaseParameters parameters = new()
				{
					BackupFilePath = backup.FilePath,
					Contents = result,
					EncryptedDek = encryptedDek,
					Files = files,
					Folder = folder,
					NewStatus = EncryptionStatus.Encrypted,
					Notes = notes
				};

				if (await _contentWriter
					.UpdateDatabaseAsync(parameters, token)
					.ConfigureAwait(false) is not UpdateDatabaseResult.Done)
				{
					return;
				}
			}
			finally
			{
				WipeContents(contents);
			}
		}
		catch (Exception ex) when (EncryptionFailures.IsCryptographic(ex))
		{
			_failureReporter.Report(ex);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when every content is readable, carries an identifier, and there are as many of
	/// them as expected.
	/// </summary>
	private static bool AreContentsValid(ContentsIsValidPair[] contents, int expectedCount)
	{
		return contents.Length == expectedCount
			&& contents.All(x => x.IsValid && x.Id.IsNotDefault());
	}

	/// <summary>
	/// Overwrites the buffers of the given contents.
	/// </summary>
	private static void WipeContents(ContentsIsValidPair[] contents)
	{
		contents.ForEach(x => x.Contents.ZeroMemory());
	}

	/// <summary>
	/// Writes the identifiers of the contents that could not be converted to the log.
	/// </summary>
	private void LogInvalidContents(ContentsIsValidPair[] contents)
	{
		string identifiers = string.Join(", ", contents
			.Where(x => !x.IsValid)
			.Select(x => x.Id));

		_logger.LogError(
			$"The contents of these files cannot be converted: {identifiers}",
			assertDebug: false);
	}

	/// <summary>
	/// Converts the notes of a folder, of its subfolders and of the given files with the DEK.
	/// A note that cannot be converted throws, so the result is never partial.
	/// </summary>
	private NoteUpdate[] ProcessNotes(
		FolderModelDto folder,
		FileModelDto[] files,
		PinnedBuffer dek,
		bool encrypt)
	{
		List<NoteUpdate> notes = [];

		ExplorerModelBaseDto[] objects =
		[
			.. folder.WithSubfolders(),
			.. files
		];

		foreach (ExplorerModelBaseDto item in objects)
		{
			if (item.Note is not { } note || note.IsEmpty())
			{
				continue;
			}

			ContentIdentity identity = ContentIdentity.ForNote(item.Id);

			byte[] processed = encrypt
				? _encryption.EncryptWithDek(note, dek, identity)
				: _encryption.DecryptWithDek(note, dek, identity);

			notes.Add(new NoteUpdate(
				item.Id,
				item.EntityType,
				processed));
		}

		return [.. notes];
	}
	#endregion
}
