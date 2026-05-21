using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace DataOrganizer.Messages;

/// <summary>
/// Payload that describes properties when folder expanded or collapsed.
/// </summary>
public sealed record FolderExpandedChangedPayload(Guid Id, bool IsExpanded);

/// <summary>
/// Notification raised to request when folder expanded or collapsed.
/// </summary>
public sealed class FolderExpandedChangedMessage : ValueChangedMessage<FolderExpandedChangedPayload>
{
	#region Constructors
	public FolderExpandedChangedMessage(FolderExpandedChangedPayload payload) : base(payload)
	{
	}
	#endregion
}
