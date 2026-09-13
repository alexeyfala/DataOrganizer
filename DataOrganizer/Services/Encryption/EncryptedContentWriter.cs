using DataOrganizer.Dto.Encryption;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notifications;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using Repository.Exceptions;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Encryption;

public sealed class EncryptedContentWriter : IEncryptedContentWriter
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public EncryptedContentWriter(
		IDbAccess dbAccess,
		IDbFailureReporter dbFailureReporter,
		ILogger logger,
		INotificationService notification)
	{
		_dbAccess = dbAccess;

		_dbFailureReporter = dbFailureReporter;

		_logger = logger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<UpdateDatabaseOutcome> UpdateDatabaseAsync(
		UpdateDatabaseParameters parameters,
		CancellationToken token = default)
	{
		try
		{
			DateTime updatedAt = DateTime.Now;

			Dictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]> updates = parameters
				.Contents
				.ToDictionary(x => x.Id, pair =>
			{
				return new Action<UpdateSettersBuilder<FileEntity>>[]
				{
					builder => builder.SetProperty(x => x.Contents, pair.Contents),
					builder => builder.SetProperty(x => x.UpdatedAt, updatedAt)
				};
			});

			// A note of a file is stored in the same transaction as its contents.
			foreach (NoteUpdate note in parameters.Notes.Where(x => !x.IsFolderNote()))
			{
				if (!updates.TryGetValue(note.Id, out Action<UpdateSettersBuilder<FileEntity>>[]? setters))
				{
					continue;
				}

				updates[note.Id] = [.. setters, builder => builder.SetProperty(x => x.Note, note.Note)];
			}

			Dictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]> folderUpdates = parameters
				.Notes
				.Where(x => x.IsFolderNote())
				.ToDictionary(x => x.Id, note =>
			{
				return new Action<UpdateSettersBuilder<FolderEntity>>[]
				{
					builder => builder.SetProperty(x => x.Note, note.Note),
					builder => builder.SetProperty(x => x.UpdatedAt, updatedAt)
				};
			});

			Action<UpdateSettersBuilder<FolderEntity>>[] noteSetters = folderUpdates.GetValueOrDefault(parameters.Folder.Id, []);

			folderUpdates[parameters.Folder.Id] = [.. noteSetters, SetDek];

			await _dbAccess
				.UpdateFileAndFolderPropertiesAsync(updates, folderUpdates, token)
				.ConfigureAwait(false);

			ExplorerItemDtoBase[] objects =
			[
				.. parameters.Folder.WithSubfolders(),
				.. parameters.Files
			];

			objects.ForEach(x => x.EncryptionStatus = parameters.NewStatus);

			ApplyNotes(objects, parameters.Notes);

			parameters
				.Folder
				.EncryptedDek = parameters.EncryptedDek;

			return UpdateDatabaseOutcome.Saved;

			void SetDek(UpdateSettersBuilder<FolderEntity> builder)
			{
				builder.SetProperty(x => x.EncryptedDek, parameters.EncryptedDek);
			}
		}
		catch (DatabaseNotWritableException ex)
		{
			// The database turned the transaction down, which is a refusal and not a bug.
			_logger.LogException(ex, breakInDebugger: false);

			return await RestoreAsync(parameters.BackupFilePath, UpdateDatabaseOutcome.SaveFailed)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return await RestoreAsync(
				parameters.BackupFilePath,
				UpdateDatabaseOutcome.ExceptionThrown).ConfigureAwait(false);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Applies the processed notes to the objects, wiping the replaced buffers.
	/// </summary>
	private static void ApplyNotes(ExplorerItemDtoBase[] objects, NoteUpdate[] notes)
	{
		if (notes.Length == 0)
		{
			return;
		}

		Dictionary<Guid, byte[]> processed = notes.ToDictionary(x => x.Id, x => x.Note);

		foreach (ExplorerItemDtoBase item in objects)
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
	/// Reports the failure and rolls the database back to the copy taken before the conversion.
	/// </summary>
	private async Task<UpdateDatabaseOutcome> RestoreAsync(string backupFilePath, UpdateDatabaseOutcome result)
	{
		_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);

		try
		{
			// The rollback has to run even when the operation was cancelled.
			await _dbAccess
				.RestoreFromBackupAsync(backupFilePath, CancellationToken.None)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			// A rollback that did not happen leaves the conversion half-done, which has to be said out loud.
			_dbFailureReporter.Report(ex, Strings.FailedToRestoreDatabase);
		}

		return result;
	}
	#endregion
}
