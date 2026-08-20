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

			Dictionary<Guid, Action<UpdateSettersBuilder<FolderModel>>[]> folderUpdates = parameters
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

			Action<UpdateSettersBuilder<FolderModel>>[] noteSetters = folderUpdates.GetValueOrDefault(parameters.Folder.Id, []);

			folderUpdates[parameters.Folder.Id] = [.. noteSetters, SetDek];

			if (!await _dbAccess
				.UpdateFileAndFolderPropertiesAsync(updates, folderUpdates, token)
				.ConfigureAwait(false))
			{
				return await RestoreAsync(parameters.BackupFilePath, UpdateDatabaseResult.FailedToSaveInDb)
					.ConfigureAwait(false);
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

			void SetDek(UpdateSettersBuilder<FolderModel> builder)
			{
				builder.SetProperty(x => x.EncryptedDek, parameters.EncryptedDek);
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return await RestoreAsync(
				parameters.BackupFilePath,
				UpdateDatabaseResult.ExceptionThrown).ConfigureAwait(false);
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
	/// Reports the failure and rolls the database back to the copy taken before the conversion.
	/// </summary>
	private async Task<UpdateDatabaseResult> RestoreAsync(string backupFilePath, UpdateDatabaseResult result)
	{
		SendMessage(Strings.FailedToProcessContents, SnackbarMessageLevel.Error);

		// The rollback has to run even when the operation was cancelled.
		await _dbAccess
			.RestoreFromBackupAsync(backupFilePath, CancellationToken.None)
			.ConfigureAwait(false);

		return result;
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
