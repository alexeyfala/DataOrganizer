using Avalonia.Controls;
using DataOrganizer.Windows;
using System;

namespace DataOrganizer.Interfaces.Views;

/// <summary>
/// Opens the main window, the editor or the favorites, and acts on the one that is shown.
/// </summary>
public interface IMainWindowLauncher
{
	#region Methods
	/// <summary>
	/// Brings the main window back from the minimized state and activates it.
	/// </summary>
	void ActivateMainWindow();

	/// <summary>
	/// Brings the main window to the center of the screen of <paramref name="owner" />.
	/// </summary>
	void CenterMainWindow(Window owner);

	/// <summary>
	/// Configures <see cref="EditorWindow" />.
	/// </summary>
	EditorWindow CreateEditorWindow(in Guid showObjectId = default);

	/// <summary>
	/// Configures <see cref="FavoritesWindow" />.
	/// </summary>
	FavoritesWindow CreateFavoritesWindow();

	/// <summary>
	/// Opens the favorites in the popup of the main window when the main window is the favorites window.
	/// </summary>
	void ShowFavorites();

	/// <summary>
	/// Shows the object in the editor: in the tree of the editor window, or in a new one that replaces the favorites
	/// window.
	/// </summary>
	void ShowInEditor(Guid id);

	/// <summary>
	/// Configures and shows the main application window, which shows the tree of the work session.
	/// </summary>
	void ShowMainWindow();
	#endregion
}
