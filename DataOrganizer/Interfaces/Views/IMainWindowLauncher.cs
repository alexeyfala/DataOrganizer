using System;

namespace DataOrganizer.Interfaces.Views;

/// <summary>
/// Opens the main window, the editor or the favorites window: at launch and in place of the other one.
/// </summary>
public interface IMainWindowLauncher
{
	#region Methods
	/// <summary>
	/// Configures and shows the main application window, which shows the tree of the work session.
	/// </summary>
	void ShowMainWindow();

	/// <summary>
	/// Closes the main window and shows the editor in its place, with the object <paramref name="showObjectId" /> shown
	/// in the tree when one is given.
	/// </summary>
	void SwitchToEditor(Guid showObjectId = default);

	/// <summary>
	/// Closes the main window and shows the favorites window in its place.
	/// </summary>
	void SwitchToFavorites();
	#endregion
}
