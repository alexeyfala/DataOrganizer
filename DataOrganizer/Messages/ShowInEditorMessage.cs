using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace DataOrganizer.Messages;

/// <summary>
/// Payload that describes a data needed to show object in the editor.
/// </summary>
public sealed record ShowInEditorPayload(Guid Id, Window Window);

/// <summary>
/// Notification raised to show object in the editor.
/// </summary>
public sealed class ShowInEditorMessage : ValueChangedMessage<ShowInEditorPayload>
{
	#region Constructors
	public ShowInEditorMessage(ShowInEditorPayload value) : base(value)
	{
	}
	#endregion
}
