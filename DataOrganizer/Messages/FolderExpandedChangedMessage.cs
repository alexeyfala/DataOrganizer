using CommunityToolkit.Mvvm.Messaging.Messages;
using DataOrganizer.DTO.Entities.Models;

namespace DataOrganizer.Messages;

/// <summary>
/// Notification raised to request when folder expanded or collapsed.
/// </summary>
internal sealed class FolderExpandedChangedMessage : ValueChangedMessage<FolderModelDto>
{
	#region Constructors
	public FolderExpandedChangedMessage(FolderModelDto folder) : base(folder)
	{
	}
	#endregion
}
