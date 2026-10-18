using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions.Text;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Interfaces.Notifications;
using Repository.Interfaces.Database;
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

	/// <inheritdoc cref="INoteCipher" />
	private readonly INoteCipher _noteCipher;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;
	#endregion

	#region Constructors
	public NoteEditor(
		IDbAccess dbAccess,
		ILogger logger,
		INoteCipher noteCipher,
		INotificationService notification,
		TimeProvider timeProvider)
	{
		_dbAccess = dbAccess;

		_logger = logger;

		_noteCipher = noteCipher;

		_notification = notification;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> EditAsync(
		ExplorerItemDtoBase item,
		string? note,
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

		DateTime updatedAt = _timeProvider
			.GetLocalNow()
			.LocalDateTime;

		bool isSaved;

		try
		{
			isSaved = await _dbAccess.UpdateNoteAsync(
				item.Kind,
				item.Id,
				encoded,
				updatedAt,
				token).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return Fail(item);
		}

		if (!isSaved)
		{
			return Fail(item);
		}

		byte[]? replaced = item.Note;

		item.Note = encoded;

		item.UpdatedAt = updatedAt;

		// The replaced buffer holds the note itself as long as the object is not encrypted.
		replaced?.ZeroMemory();

		string successText = isRemoved
			? Strings.NoteHasBeenDeleted
			: Strings.NoteHasBeenSaved;

		_notification.ShowInformationSnackbar(successText);

		_logger.LogInformation(successText);

		return true;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reports a note that could not be stored.
	/// </summary>
	private bool Fail(ExplorerItemDtoBase item)
	{
		_logger.LogError($"{Strings.FailedToSaveNote}:{item.GetPropertyValues(
			true,
			nameof(ExplorerItemDtoBase.Id),
			nameof(ExplorerItemDtoBase.Name),
			nameof(ExplorerItemDtoBase.EncryptionStatus))}");

		_notification.ShowErrorSnackbar(Strings.FailedToSaveNote);

		return false;
	}
	#endregion
}
