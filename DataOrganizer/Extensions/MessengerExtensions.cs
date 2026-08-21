using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Enums;
using DataOrganizer.Helpers;
using DataOrganizer.Messages;

namespace DataOrganizer.Extensions;

/// <summary>
/// Sends the notifications of the shell: the snackbar and the progress bar of the editor.
/// </summary>
public static class MessengerExtensions
{
	#region Methods
	/// <summary>
	/// Shows a snackbar with the given text and level.
	/// </summary>
	public static void ShowSnackbar(
		this IMessenger messenger,
		string text,
		SnackbarMessageLevel level)
	{
		messenger.Send(new ShowSnackbarMessage(text, level));
	}

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
