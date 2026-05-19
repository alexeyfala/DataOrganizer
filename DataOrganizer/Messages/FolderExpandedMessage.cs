using CommunityToolkit.Mvvm.Messaging.Messages;
using DataOrganizer.DTO.Entities.Models;

namespace DataOrganizer.Messages;

internal sealed class FolderExpandedMessage : ValueChangedMessage<FolderModelDto>
{
	#region Constructors
	public FolderExpandedMessage(FolderModelDto folder) : base(folder)
	{
	}
	#endregion
}
