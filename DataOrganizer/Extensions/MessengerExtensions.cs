using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Helpers;
using DataOrganizer.Messages.Editor;

namespace DataOrganizer.Extensions;

/// <summary>
/// Sends the notifications of the shell: the progress bar of the editor.
/// </summary>
public static class MessengerExtensions
{
	#region Methods
	/// <summary>
	/// Shows the progress bar of the editor; the returned scope hides it again.
	/// </summary>
	internal static ProgressScope ShowProgress(this IMessenger messenger)
	{
		messenger.Send(new ShowProgressBarMessage(true));

		return new(messenger);
	}
	#endregion
}
