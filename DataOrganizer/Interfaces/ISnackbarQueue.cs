using DataOrganizer.Messages;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Shows snackbar messages one after another, so that a message is not cut short by the next one.
/// </summary>
public interface ISnackbarQueue
{
	#region Methods
	/// <summary>
	/// Shows a message as soon as the messages queued before it have gone.
	/// Returns <c>false</c> when the message will not be shown at all.
	/// </summary>
	bool Show(ShowSnackbarMessage message);
	#endregion
}
