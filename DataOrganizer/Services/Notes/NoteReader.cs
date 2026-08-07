using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Messages;
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

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="INoteCipher" />
	private readonly INoteCipher _noteCipher;
	#endregion

	#region Constructors
	public NoteReader(
		ILogger logger,
		IMessenger messenger,
		INoteCipher noteCipher)
	{
		_logger = logger;

		_messenger = messenger;

		_noteCipher = noteCipher;
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

		_messenger.Send(new ShowSnackbarMessage(Strings.FailedToReadNote, SnackbarMessageLevel.Error));

		return null;
	}
	#endregion
}
