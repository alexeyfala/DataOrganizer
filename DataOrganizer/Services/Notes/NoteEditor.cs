using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Messages;
using Entities.Enums;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Notes;

public sealed class NoteEditor : INoteEditor
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="INoteCipher" />
	private readonly INoteCipher _noteCipher;
	#endregion

	#region Constructors
	public NoteEditor(
		IDbAccess dbAccess,
		ILogger logger,
		IMessenger messenger,
		INoteCipher noteCipher)
	{
		_dbAccess = dbAccess;

		_logger = logger;

		_messenger = messenger;

		_noteCipher = noteCipher;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> EditAsync(
		ExplorerModelBaseDto item,
		string? note,
		DateTime updatedDate,
		CancellationToken token = default)
	{
		_logger.LogInformation("Editing a note of an object");

		bool isRemoved = string.IsNullOrWhiteSpace(note);

		byte[]? encoded;

		try
		{
			encoded = _noteCipher.Encode(item, note);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return Fail(item);
		}

		// Blank text is stored as null, any other text has to produce bytes.
		if (encoded is null && !isRemoved)
		{
			return Fail(item);
		}

		Task<bool> task = item.EntityType switch
		{
			EntityType.Folder => _dbAccess.UpdateFolderPropertiesAsync(item.Id,
			[
				x => x.SetProperty(x => x.Note, encoded),
				x => x.SetProperty(x => x.UpdatedDate, updatedDate)
			], token),
			EntityType.File or EntityType.DataSet => _dbAccess.UpdateFilePropertiesAsync(item.Id,
			[
				x => x.SetProperty(x => x.Note, encoded),
				x => x.SetProperty(x => x.UpdatedDate, updatedDate)
			], token),
			_ => throw new NotImplementedException()
		};

		if (!await task.ConfigureAwait(false))
		{
			return Fail(item);
		}

		byte[]? replaced = item.Note;

		item.Note = encoded;

		item.UpdatedDate = updatedDate;

		// The replaced buffer holds the note itself as long as the object is not encrypted.
		replaced?.ZeroMemory();

		string successText = isRemoved
			? Strings.NoteHasBeenDeleted
			: Strings.NoteHasBeenSaved;

		Notify(successText, SnackbarMessageLevel.Information);

		_logger.LogInformation(successText);

		return true;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reports a note that could not be stored.
	/// </summary>
	private bool Fail(ExplorerModelBaseDto item)
	{
		_logger.LogError($"{Strings.FailedToSaveNote}:{item.GetPropertyValues(
			true,
			nameof(ExplorerModelBaseDto.Id),
			nameof(ExplorerModelBaseDto.Name),
			nameof(ExplorerModelBaseDto.EncryptionStatus))}");

		Notify(Strings.FailedToSaveNote, SnackbarMessageLevel.Error);

		return false;
	}

	/// <summary>
	/// Sends a snackbar notification.
	/// </summary>
	private void Notify(string text, SnackbarMessageLevel level) => _messenger.Send(new ShowSnackbarMessage(text, level));
	#endregion
}
