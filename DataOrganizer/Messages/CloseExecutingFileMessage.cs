using CommunityToolkit.Mvvm.Messaging.Messages;
using DataOrganizer.DTO.Entities.Models;

namespace DataOrganizer.Messages;

/// <summary>
/// Notification raised to request closing a file currently executing in the operating system.
/// </summary>
internal sealed class CloseExecutingFileMessage : ValueChangedMessage<FileModelDto>
{
	#region Constructors
	public CloseExecutingFileMessage(FileModelDto file) : base(file)
	{
	}
	#endregion
}
