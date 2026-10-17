using Avalonia.Controls;
using Avalonia.Layout;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Settings;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Views;

/// <summary>
/// Manages the life cycle of application windows and user controls.
/// </summary>
public interface IViewLauncher
{
	#region Properties
	/// <summary>
	/// The default size of the window.
	/// </summary>
	static System.Drawing.Size DefaultWindowSize { get; } = new(900, 600);
	#endregion

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
	/// Brings the open <see cref="NotepadWindow" /> to the center of the screen of <paramref name="owner" />.
	/// </summary>
	void CenterNotepadWindow(Window owner);

	/// <summary>
	/// Configures <see cref="ClipboardLogWindow" />.
	/// </summary>
	ClipboardLogWindow CreateClipboardLogWindow(Window owner);

	/// <summary>
	/// Configures <see cref="EditorWindow" />.
	/// </summary>
	EditorWindow CreateEditorWindow(
		IEnumerable<FileDto> executingFiles,
		in Guid showObjectId = default);

	/// <summary>
	/// Configures <see cref="FavoritesWindow" />.
	/// </summary>
	FavoritesWindow CreateFavoritesWindow(IEnumerable<FileDto> executingFiles);

	/// <summary>
	/// Configures the main application window, which shows the tree of the work session.
	/// </summary>
	Window CreateMainWindow();

	/// <summary>
	/// Configures <see cref="NotepadWindow" />.
	/// </summary>
	NotepadWindow CreateNotepadWindow(Window owner);

	/// <summary>
	/// Saves <see cref="ClipboardLogWindow" /> settings to the file.
	/// </summary>
	void SaveClipboardLogSettings(ClipboardLogWindow window);

	/// <summary>
	/// Saves <see cref="EditorWindow" /> settings to the file.
	/// </summary>
	Task SaveEditorSettingsAsync(EditorWindow window);

	/// <summary>
	/// Saves <see cref="FavoritesWindow" /> settings to the file.
	/// </summary>
	Task SaveFavoritesSettingsAsync(FavoritesWindow window);

	/// <summary>
	/// Saves <see cref="NotepadWindow" /> settings to the file.
	/// </summary>
	void SaveNotepadSettings(NotepadWindow window);

	/// <summary>
	/// Prompts to unlock the persisted clipboard history when required, then opens
	/// <see cref="ClipboardLogWindow" />.
	/// </summary>
	Task ShowClipboardLogWindowAsync(Window owner);

	/// <summary>
	/// Activates the open <see cref="NotepadWindow" /> or opens a new one.
	/// </summary>
	void ShowNotepadWindow(Window owner);

	/// <summary>
	/// Shows a notice in a window of its own and waits until it is closed; needs no other window.
	/// </summary>
	Task ShowNoticeAsync(NoticeParameters parameters);

	/// <summary>
	/// Reports a database that cannot be worked with and ends the launch once the report is closed.
	/// </summary>
	Task ShowStartupErrorAsync(string databaseFilePath);

	/// <summary>
	/// Sets default <see cref="Window.WindowStartupLocation" /> to the window.
	/// </summary>
	internal static void SetDefaultLocation(Window window)
	{
		window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
	}

	/// <summary>
	/// Sets default <see cref="FavoritesViewSettings.NavigationColumnWidth" />.
	/// </summary>
	internal static void SetDefaultNavigationColumnWidth(FavoritesViewModel viewModel)
	{
		viewModel
			.FavoritesSettings
			.NavigationColumnWidth = viewModel.PopupWidth / 2.0;
	}

	/// <summary>
	/// Sets default <see cref="EditorViewModel.NavigationColumnWidth" />.
	/// </summary>
	internal static void SetDefaultNavigationColumnWidth(Window window, EditorViewModel viewModel)
	{
		viewModel.NavigationColumnWidth = new GridLength(window.Width / 3.0);
	}

	/// <summary>
	/// Sets default <see cref="FavoritesViewModel.PopupHeight" />, <see cref="FavoritesViewModel.PopupWidth" />.
	/// </summary>
	internal static void SetDefaultPopupSize(FavoritesViewModel viewModel)
	{
		viewModel.PopupHeight = 250.0;

		viewModel.PopupWidth = viewModel.PopupHeight * 2.0;
	}

	/// <summary>
	/// Sets default <see cref="Layoutable.Width" />, <see cref="Layoutable.Width" /> to the window.
	/// </summary>
	internal static void SetDefaultSize(Window window)
	{
		window.Width = DefaultWindowSize.Width;

		window.Height = DefaultWindowSize.Height;
	}
	#endregion
}
