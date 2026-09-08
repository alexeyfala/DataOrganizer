using DataOrganizer.DTO;
using Material.Styles.Controls;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Shows snackbar messages in the host of the active window.
/// </summary>
public interface ISnackbarPresenter
{
	#region Properties
	/// <summary>
	/// <c>True</c> while a host able to show messages exists.
	/// </summary>
	bool IsHostLoaded { get; }

	/// <summary>
	/// <c>True</c> while the pointer rests on the shown message.
	/// </summary>
	bool IsPointerOverMessage { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Takes the host messages are to be shown in.
	/// </summary>
	void AttachHost(SnackbarHost host);

	/// <summary>
	/// Gives up the host that has left the screen.
	/// </summary>
	void DetachHost(SnackbarHost host);

	/// <summary>
	/// Shows a message until it is removed.
	/// </summary>
	void Post(SnackbarContent content);

	/// <summary>
	/// Takes the shown message off the screen.
	/// </summary>
	void Remove();
	#endregion
}
