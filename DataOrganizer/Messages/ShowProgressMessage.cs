using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DataOrganizer.Messages;

/// <summary>
/// Notification raised to request displaying a progress bar in the editor.
/// </summary>
internal sealed class ShowProgressMessage : ValueChangedMessage<bool>
{
	#region Constructors
	public ShowProgressMessage(bool value) : base(value)
	{
	}
	#endregion
}
