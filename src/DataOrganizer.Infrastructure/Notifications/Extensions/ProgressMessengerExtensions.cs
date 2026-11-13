using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Infrastructure.Notifications.Helpers;
using DataOrganizer.Infrastructure.Notifications.Messages;

namespace DataOrganizer.Infrastructure.Notifications.Extensions;

/// <summary>
/// Sends the notifications of the shell for the progress bar of the editor.
/// </summary>
public static class ProgressMessengerExtensions
{
	#region Methods
	/// <summary>
	/// Shows the progress bar of the editor; the returned scope hides it again.
	/// </summary>
	public static ProgressScope ShowProgress(this IMessenger messenger)
	{
		messenger.Send(new ShowProgressBarMessage(true));

		return new(messenger);
	}
	#endregion
}
