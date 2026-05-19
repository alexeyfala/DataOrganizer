using CommunityToolkit.Mvvm.Messaging.Messages;
using DataOrganizer.DTO.Entities.Models;

namespace DataOrganizer.Messages;

/// <summary>
/// Payload published by <see cref="Services.FileChangeTracker" /> when the
/// background change-tracking of an executing file cannot continue and the file should be closed.
/// </summary>
internal sealed record FileTrackingFailedPayload(FileModelDto File, string Message);


/// <summary>
/// Notification raised when file-change tracking fails.
/// </summary>
internal sealed class FileTrackingFailedMessage : ValueChangedMessage<FileTrackingFailedPayload>
{
	#region Constructors
	public FileTrackingFailedMessage(FileTrackingFailedPayload payload) : base(payload)
	{
	}
	#endregion
}
