using DataOrganizer.Messages;
using System;

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
	#endregion

	#region Methods
	/// <summary>
	/// Shows a message for the given time.
	/// </summary>
	void Post(ShowSnackbarMessage message, TimeSpan duration);
	#endregion
}
