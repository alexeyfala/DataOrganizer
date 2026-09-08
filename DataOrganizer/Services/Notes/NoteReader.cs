using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Notes;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;

namespace DataOrganizer.Services.Notes;

public sealed class NoteReader : INoteReader
{
	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INoteCipher" />
	private readonly INoteCipher _noteCipher;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public NoteReader(
		ILogger logger,
		INoteCipher noteCipher,
		INotificationService notification)
	{
		_logger = logger;

		_noteCipher = noteCipher;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public string? ReadNote(object? item)
	{
		// An encrypted note is not a failure: the object is shown with a tooltip instead of the popup.
		if (item is not ExplorerModelBaseDto dto
			|| dto.Note is not { } note
			|| note.IsEmpty()
			|| dto.EncryptionStatus == EncryptionStatus.Encrypted)
		{
			return null;
		}

		try
		{
			if (_noteCipher.Decode(dto) is { } text)
			{
				return text;
			}

			_logger.LogError($"{Strings.FailedToReadNote}:{dto.GetPropertyValues(
				true,
				nameof(ExplorerModelBaseDto.Id),
				nameof(ExplorerModelBaseDto.Name),
				nameof(ExplorerModelBaseDto.EncryptionStatus))}");
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}

		_notification.ShowErrorSnackbar(Strings.FailedToReadNote);

		return null;
	}
	#endregion
}
