using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Messages;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using Repository.DTO;
using Repository.Interfaces;
using Repository.Services;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Encryption;

public sealed class EntityEncryption : IEntityEncryption
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="ISessionKeyStore" />
	private readonly ISessionKeyStore _sessionKeyStore;
	#endregion

	#region Constructors
	public EntityEncryption(
		IDbAccess dbAccess,
		IDialogService dialogService,
		IEncryptionService encryption,
		ILogger logger,
		IMessenger messenger,
		ISessionKeyStore sessionKeyStore)
	{
		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_encryption = encryption;

		_logger = logger;

		_messenger = messenger;

		_sessionKeyStore = sessionKeyStore;
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

		if (await RequestDekAsync(
			keeperId: folder.Id,
			encryptedDek: folder.EncryptedDek,
			header: Strings.ChangePassword,
			label: Strings.OldPassword,
			token: token).ConfigureAwait(false) is not { } dek)
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
				ContentIdentity.ForDek(folder.Id).ToAssociatedData());

			if (!await _dbAccess.UpdateFolderPropertiesAsync(folder.Id,
				[
					x => x.SetProperty(x => x.EncryptedDek, encryptedDek)
				], token).ConfigureAwait(false))
			{
				return;
			}

			folder.EncryptedDek = encryptedDek;

			SendMessage(Strings.PasswordChanged, SnackbarMessageLevel.Information);
		}
		catch (Exception ex) when (ex is InvalidCredentialException or CryptographicException)
		{
			ReportCryptographicFailure(ex);
		}
		finally
		{
			dek.ZeroMemory();
		}
	}

	/// <inheritdoc />
	public byte[] Decrypt(FileModelDto file, byte[] input)
	{
		// Empty content is written without encryption, so there is nothing to open here.
		if (input.IsEmpty())
		{
			return input;
		}

		if (file.FindPasswordKeeper() is not { } root)
		{
			throw new InvalidOperationException(
				$@"The file ""{file.Id}"" is marked as decrypted but belongs to no password keeper.");
		}

		return _sessionKeyStore.Decrypt(
			root.Id,
			ContentIdentity.ForContents(file.Id),
			input);
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
		if (await RequestDekAsync(
			keeperId: folder.Id,
			encryptedDek: folder.EncryptedDek,
			header: Strings.DecryptFiles,
			token: token).ConfigureAwait(false) is not { } decryptedDek)
		{
			return;
		}

		try
		{
			ShowProgressBar();

			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(files.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			if (!AreLoadedContentsValid(contents, files.Length))
			{
				SendMessage(Strings.FailedToLoadFilesContents, SnackbarMessageLevel.Error);

				return;
			}

			ContentsIsValidPair[] result = [.. _encryption.DecryptContents(contents, decryptedDek)];

			if (!AreContentsValid(result, contents.Length))
			{
				SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

				return;
			}

			if (ProcessNotes(
				folder,
				files,
				decryptedDek,
				encrypt: false) is not { } notes)
			{
				SendMessage(Strings.FailedToProcessNotes, SnackbarMessageLevel.Error);

				return;
			}

			using DatabaseBackup? backup = await _dbAccess
				.BackupDatabaseAsync(token)
				.ConfigureAwait(false);

			if (backup is null)
			{
				SendMessage(Strings.UnableToCreateDatabaseBackup, SnackbarMessageLevel.Error);

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

			await UpdateDatabaseAsync(parameters, token).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is InvalidCredentialException or CryptographicException)
		{
			ReportCryptographicFailure(ex);
		}
		finally
		{
			decryptedDek.ZeroMemory();

			HideProgressBar();
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
			ShowProgressBar();

			ContentsIsValidPair[] contents = await _dbAccess
				.GetFilesContentsAsync(files.Select(x => x.Id), token)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			if (!AreLoadedContentsValid(contents, files.Length))
			{
				SendMessage(Strings.FailedToLoadFilesContents, SnackbarMessageLevel.Error);

				return;
			}

			byte[] dek = _encryption.CreateRandomDek();

			try
			{
				ContentsIsValidPair[] result = [.. _encryption.EncryptContents(contents, dek)];

				if (!AreContentsValid(result, contents.Length))
				{
					SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

					return;
				}

				using PinnedBuffer passwordBinary = password.ToUtf8Buffer();

				byte[] encryptedDek = _encryption.Encrypt(
					dek,
					passwordBinary,
					ContentIdentity.ForDek(folder.Id).ToAssociatedData());

				if (ProcessNotes(
					folder,
					files,
					dek,
					encrypt: true) is not { } notes)
				{
					SendMessage(Strings.FailedToProcessNotes, SnackbarMessageLevel.Error);

					return;
				}

				using DatabaseBackup? backup = await _dbAccess
					.BackupDatabaseAsync(token)
					.ConfigureAwait(false);

				if (backup is null)
				{
					SendMessage(Strings.UnableToCreateDatabaseBackup, SnackbarMessageLevel.Error);

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

				await UpdateDatabaseAsync(parameters, token).ConfigureAwait(false);
			}
			finally
			{
				dek.ZeroMemory();
			}
		}
		catch (Exception ex) when (ex is InvalidCredentialException or CryptographicException)
		{
			ReportCryptographicFailure(ex);
		}
		finally
		{
			HideProgressBar();
		}
	}

	/// <inheritdoc />
	public void HideAllContents(IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		hierarchy
			.FilterBy(x => x.EncryptionStatus == EncryptionStatus.Decrypted)
			.ForEach(x => x.EncryptionStatus = EncryptionStatus.Encrypted);

		_sessionKeyStore.LockAll();
	}

	/// <inheritdoc />
	public void HideFileContents(FileModelDto file)
	{
		file.EncryptionStatus = EncryptionStatus.Encrypted;

		LockKeeperOf(file);
	}

	/// <inheritdoc />
	public void HideFolderContents(FolderModelDto folder)
	{
		folder
			.ToEnumerable()
			.Concat(folder.GetAllChildren())
			.ForEach(x => x.EncryptionStatus = EncryptionStatus.Encrypted);

		LockKeeperOf(folder);
	}

	/// <inheritdoc />
	public async Task<bool> ShowFileContentsAsync(FileModelDto file, CancellationToken token = default)
	{
		if (file.FindPasswordKeeper() is not { } root || root.EncryptedDek is null)
		{
			return false;
		}

		if (await RequestDekAsync(
			keeperId: root.Id,
			encryptedDek: root.EncryptedDek,
			header: Strings.ShowContents,
			token: token).ConfigureAwait(false) is not { } dek)
		{
			return false;
		}

		try
		{
			ShowProgressBar();

			if (!_sessionKeyStore.Unlock(root.Id, dek))
			{
				return false;
			}

			file.EncryptionStatus = EncryptionStatus.Decrypted;

			return true;
		}
		catch (Exception ex) when (ex is InvalidCredentialException or CryptographicException)
		{
			ReportCryptographicFailure(ex);

			return false;
		}
		finally
		{
			dek.ZeroMemory();

			HideProgressBar();
		}
	}

	/// <inheritdoc />
	public async Task ShowFolderContentsAsync(FolderModelDto folder, CancellationToken token = default)
	{
		if (folder.FindPasswordKeeper() is not { } root || root.EncryptedDek is null)
		{
			return;
		}

		if (await RequestDekAsync(
			keeperId: root.Id,
			encryptedDek: root.EncryptedDek,
			header: Strings.ShowContents,
			token: token).ConfigureAwait(false) is not { } dek)
		{
			return;
		}

		try
		{
			ShowProgressBar();

			if (ShowFolderContents(folder, root.Id, dek))
			{
				return;
			}

			SendMessage(Strings.FailedToShowFileContents, SnackbarMessageLevel.Error);
		}
		catch (Exception ex) when (ex is InvalidCredentialException or CryptographicException)
		{
			ReportCryptographicFailure(ex);
		}
		finally
		{
			dek.ZeroMemory();

			HideProgressBar();
		}
	}

	/// <inheritdoc />
	public async Task<byte[]?> TryToDecryptContentsAsync(
		FileModelDto file,
		byte[] contents,
		string header,
		CancellationToken token = default)
	{
		// Empty content is written without encryption, so neither a password nor a key is needed.
		if (contents.IsEmpty())
		{
			return contents;
		}

		if (file.EncryptionStatus == EncryptionStatus.Encrypted)
		{
			if (file.FindPasswordKeeper() is not { } root || root.EncryptedDek is null)
			{
				return null;
			}

			if (await RequestDekAsync(
				keeperId: root.Id,
				encryptedDek: root.EncryptedDek,
				header: header,
				token: token).ConfigureAwait(false) is not { } decryptedDek)
			{
				return null;
			}

			try
			{
				return _encryption.DecryptWithDek(
					contents,
					decryptedDek,
					ContentIdentity.ForContents(file.Id).ToAssociatedData());
			}
			catch (Exception ex) when (ex is InvalidCredentialException or CryptographicException)
			{
				ReportCryptographicFailure(ex);

				return null;
			}
			finally
			{
				decryptedDek.ZeroMemory();
			}
		}
		else if (file.EncryptionStatus == EncryptionStatus.Decrypted)
		{
			try
			{
				return Decrypt(file, contents);
			}
			catch (Exception ex) when (ex is CryptographicException or InvalidOperationException)
			{
				ReportCryptographicFailure(ex);

				return null;
			}
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

			Dictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]> updates = parameters
				.Contents
				.ToDictionary(x => x.Id, pair =>
			{
				return new Action<UpdateSettersBuilder<FileModel>>[]
				{
					builder => builder.SetProperty(x => x.Contents, pair.Contents),
					builder => builder.SetProperty(x => x.UpdatedDate, updatedDate)
				};
			});

			// A note of a file is stored in the same transaction as its contents.
			foreach (NoteUpdate note in parameters.Notes.Where(x => !x.IsFolderNote()))
			{
				if (!updates.TryGetValue(note.Id, out Action<UpdateSettersBuilder<FileModel>>[]? setters))
				{
					continue;
				}

				updates[note.Id] = [.. setters, builder => builder.SetProperty(x => x.Note, note.Note)];
			}

			if (!await _dbAccess
				.UpdateFilePropertiesAsync(updates, token)
				.ConfigureAwait(false))
			{
				SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

				await _dbAccess
					.RestoreFromBackupAsync(parameters.BackupFilePath, token)
					.ConfigureAwait(false);

				return UpdateDatabaseResult.FailedToSaveContentsInDb;
			}

			if (!await _dbAccess.UpdateFolderPropertiesAsync(parameters.Folder.Id,
				[
					x => x.SetProperty(x => x.EncryptedDek, parameters.EncryptedDek)
				], token).ConfigureAwait(false))
			{
				SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

				await _dbAccess
					.RestoreFromBackupAsync(parameters.BackupFilePath, token)
					.ConfigureAwait(false);

				return UpdateDatabaseResult.FailedToSaveFolderPropertiesInDb;
			}

			Dictionary<Guid, Action<UpdateSettersBuilder<FolderModel>>[]> folderNotes = parameters
				.Notes
				.Where(x => x.IsFolderNote())
				.ToDictionary(x => x.Id, note =>
			{
				return new Action<UpdateSettersBuilder<FolderModel>>[]
				{
					builder => builder.SetProperty(x => x.Note, note.Note),
					builder => builder.SetProperty(x => x.UpdatedDate, updatedDate)
				};
			});

			if (folderNotes.Count > 0 && !await _dbAccess
				.UpdateFolderPropertiesAsync(folderNotes, token)
				.ConfigureAwait(false))
			{
				SendMessage(Strings.FailedToProcessNotes, SnackbarMessageLevel.Error);

				await _dbAccess
					.RestoreFromBackupAsync(parameters.BackupFilePath, token)
					.ConfigureAwait(false);

				return UpdateDatabaseResult.FailedToSaveFolderPropertiesInDb;
			}

			ExplorerModelBaseDto[] objects = GetObjects(parameters.Folder, parameters.Files);

			objects.ForEach(x => x.EncryptionStatus = parameters.NewStatus);

			ApplyNotes(objects, parameters.Notes);

			parameters
				.Folder
				.EncryptedDek = parameters.EncryptedDek;

			return UpdateDatabaseResult.Done;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return UpdateDatabaseResult.ExceptionThrown;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Applies the processed notes to the objects, wiping the replaced buffers.
	/// </summary>
	private static void ApplyNotes(ExplorerModelBaseDto[] objects, NoteUpdate[] notes)
	{
		if (notes.Length == 0)
		{
			return;
		}

		Dictionary<Guid, byte[]> processed = notes.ToDictionary(x => x.Id, x => x.Note);

		foreach (ExplorerModelBaseDto item in objects)
		{
			if (!processed.TryGetValue(item.Id, out byte[]? note))
			{
				continue;
			}

			byte[]? replaced = item.Note;

			item.Note = note;

			// The replaced buffer holds the note in plain text after an encryption.
			replaced?.ZeroMemory();
		}
	}

	/// <summary>
	/// <c>True</c> when the contents are valid.
	/// </summary>
	private static bool AreContentsValid(ContentsIsValidPair[] contents, int shouldBe)
	{
		return contents.Length == shouldBe
			&& contents.All(x => x.IsValid)
			&& contents.All(x => x.Id.IsNotDefault());
	}

	/// <summary>
	/// <c>True</c> when the loaded from database contents are valid.
	/// </summary>
	private static bool AreLoadedContentsValid(ContentsIsValidPair[] contents, int fileCount)
	{
		return contents.Length == fileCount && contents.All(x => x.IsValid);
	}

	/// <summary>
	/// Returns the folder itself, its subfolders and the given files as one sequence.
	/// </summary>
	private static ExplorerModelBaseDto[] GetObjects(FolderModelDto folder, FileModelDto[] files)
	{
		return
		[
			.. folder.ToEnumerable(),
			.. folder.Children.GetFolders(),
			.. files
		];
	}

	/// <summary>
	/// Sends <see cref="ShowProgressBarMessage" /> to hide progress bar in the editor.
	/// </summary>
	private void HideProgressBar() => _messenger.Send(new ShowProgressBarMessage(false));

	/// <summary>
	/// Drops the key of the keeper the object belongs to, but only once nothing under that keeper is shown.
	/// </summary>
	private void LockKeeperOf(ExplorerModelBaseDto item)
	{
		FolderModelDto? keeper = item.FindPasswordKeeper();

		if (keeper?
			.ToEnumerable()
			.Concat(keeper.GetAllChildren())
			.ContainsBy(x => x.EncryptionStatus == EncryptionStatus.Decrypted) != false)
		{
			return;
		}

		_sessionKeyStore.Lock(keeper.Id);
	}

	/// <summary>
	/// Converts the notes of a folder, of its subfolders and of the given files with the DEK;
	/// <c>null</c> when a note cannot be converted.
	/// </summary>
	private NoteUpdate[]? ProcessNotes(
		FolderModelDto folder,
		FileModelDto[] files,
		byte[] dek,
		bool encrypt)
	{
		List<NoteUpdate> notes = [];

		foreach (ExplorerModelBaseDto item in GetObjects(folder, files))
		{
			if (item.Note is not { } note || note.IsEmpty())
			{
				continue;
			}

			byte[] associatedData = ContentIdentity
				.ForNote(item.Id)
				.ToAssociatedData();

			byte[] processed = encrypt
				? _encryption.EncryptWithDek(note, dek, associatedData)
				: _encryption.DecryptWithDek(note, dek, associatedData);

			notes.Add(new NoteUpdate(
				item.Id,
				item.EntityType,
				processed));
		}

		return [.. notes];
	}

	/// <summary>
	/// Reports a failed cryptographic operation to the log and to the user.
	/// </summary>
	private void ReportCryptographicFailure(Exception exception, [CallerMemberName] string callerName = "")
	{
		if (exception is InvalidCredentialException)
		{
			_logger.LogWarning($"The password has been rejected: {callerName}");

			SendMessage(Strings.IncorrectPassword, SnackbarMessageLevel.Error);

			return;
		}

		_logger.LogException(exception);

		string message = exception is CryptographicException
			? Strings.EncryptedDataIsDamaged
			: Strings.FailedToProcessContents;

		SendMessage(message, SnackbarMessageLevel.Error);
	}

	/// <summary>
	/// Prompts for the password and unwraps the DEK bound to the keeper; the caller owns the key.
	/// </summary>
	/// <returns>
	/// The unwrapped DEK, or <c>null</c> when the prompt is cancelled or the password is rejected;
	/// a rejection is reported to the user.
	/// </returns>
	private async Task<byte[]?> RequestDekAsync(
		Guid keeperId,
		byte[] encryptedDek,
		string header,
		string? label = null,
		CancellationToken token = default,
		[CallerMemberName] string callerName = "")
	{
		using PinnedSecret password = await _dialogService
			.RequestPasswordAsync(header, label, token: token)
			.ConfigureAwait(false);

		if (password.IsEmpty)
		{
			return null;
		}

		using PinnedBuffer passwordBinary = password.ToUtf8Buffer();

		try
		{
			return _encryption.Decrypt(
				encryptedDek,
				passwordBinary,
				ContentIdentity.ForDek(keeperId).ToAssociatedData());
		}
		catch (Exception ex) when (ex is InvalidCredentialException or CryptographicException)
		{
			ReportCryptographicFailure(ex, callerName);

			return null;
		}
	}

	/// <summary>
	/// Sends <see cref="ShowSnackbarMessage" /> to recepient.
	/// </summary>
	private void SendMessage(string message, SnackbarMessageLevel level)
	{
		_messenger.Send(new ShowSnackbarMessage(message, level));
	}

	/// <summary>
	/// Shows file contents in folder.
	/// </summary>
	private bool ShowFolderContents(
		FolderModelDto folder,
		Guid keeperId,
		byte[] dek)
	{
		if (!_sessionKeyStore.Unlock(keeperId, dek))
		{
			return false;
		}

		folder
			.ToEnumerable()
			.Concat(folder.GetAllChildren())
			.ForEach(x => x.EncryptionStatus = EncryptionStatus.Decrypted);

		return true;
	}

	/// <summary>
	/// Sends <see cref="ShowProgressBarMessage" /> to display progress bar in the editor.
	/// </summary>
	private void ShowProgressBar() => _messenger.Send(new ShowProgressBarMessage(true));
	#endregion
}
