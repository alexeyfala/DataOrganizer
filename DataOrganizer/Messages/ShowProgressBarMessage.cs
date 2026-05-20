using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DataOrganizer.Messages;

/// <summary>
/// Notification raised to request displaying a progress bar in the editor.
/// </summary>
internal sealed class ShowProgressBarMessage : ValueChangedMessage<bool>
{
	#region Constructors
	public ShowProgressBarMessage(bool value) : base(value)
	{
	}
	#endregion
}
