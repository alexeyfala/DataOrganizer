using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Messages;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using Repository.Interfaces;
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

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	public EncryptedContentWriter(
		IDbAccess dbAccess,
		ILogger logger,
		IMessenger messenger)
	{
		_dbAccess = dbAccess;

		_logger = logger;

		_messenger = messenger;
	}
	#endregion

	#region Methods
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

			ExplorerModelBaseDto[] objects =
			[
				.. parameters.Folder.WithSubfolders(),
				.. parameters.Files
			];

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
	/// Sends <see cref="ShowSnackbarMessage" /> to recepient.
	/// </summary>
	private void SendMessage(string message, SnackbarMessageLevel level)
	{
		_messenger.Send(new ShowSnackbarMessage(message, level));
	}
	#endregion
}
