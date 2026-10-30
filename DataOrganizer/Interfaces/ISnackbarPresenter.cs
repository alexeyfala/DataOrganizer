using Material.Styles.Controls;
using Notifications.Dto;
using Notifications.Interfaces;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Shows messages in the snackbar host that has been handed over to it.
/// </summary>
public interface ISnackbarPresenter : IMessagePresenter<SnackbarContent>
{
	#region Methods
	/// <summary>
	/// Takes the host messages are to be shown in.
	/// </summary>
	void AttachHost(SnackbarHost host);

	/// <summary>
	/// Gives up the host that has left the screen.
	/// </summary>
	void DetachHost(SnackbarHost host);
	#endregion
}
