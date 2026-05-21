using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DataOrganizer.Messages;

/// <summary>
/// Notification raised to request when read-only mode changes in the editor.
/// </summary>
public sealed class EditorReadOnlyModeChangedMessage : ValueChangedMessage<bool>
{
	#region Constructors
	public EditorReadOnlyModeChangedMessage(bool value) : base(value)
	{
	}
	#endregion
}
