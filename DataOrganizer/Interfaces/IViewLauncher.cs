using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform;
using DataOrganizer.DTO.Entities;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

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
	/// Configures <see cref="CustomClipboardWindow" />.
	/// </summary>
	CustomClipboardWindow ConfigureCustomClipboardWindow(Window owner);

	/// <summary>
	/// Configures <see cref="EditorWindow" />.
	/// </summary>
	EditorWindow ConfigureEditorWindow(
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		IEnumerable<FileModelDto> editingFiles,
		IEnumerable<FileModelDto> executingFiles,
		in Guid showObjectId = default);

	/// <summary>
	/// Configures <see cref="FavoritesWindow" />.
	/// </summary>
	FavoritesWindow ConfigureFavoritesWindow(
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		IEnumerable<FileModelDto> editingFiles,
		IEnumerable<FileModelDto> executingFiles);

	/// <summary>
	/// Configures the main application window.
	/// </summary>
	Window ConfigureMainWindow(IEnumerable<ExplorerModelBaseDto> hierarchy);

	/// <summary>
	/// Saves <see cref="CustomClipboardWindow" /> settings to the file.
	/// </summary>
	void SaveCustomClipboardSettings(CustomClipboardWindow window);

	/// <summary>
	/// Saves <see cref="EditorWindow" /> settings to the file.
	/// </summary>
	Task SaveEditorSettingsAsync(EditorWindow window);

	/// <summary>
	/// Saves <see cref="FavoritesWindow" /> settings to the file.
	/// </summary>
	Task SaveFavoritesSettingsAsync(FavoritesWindow window);

	/// <summary>
	/// Prompts to unlock the persisted clipboard history when required, then opens
	/// <see cref="CustomClipboardWindow" />.
	/// </summary>
	Task ShowCustomClipboardWindowAsync(Window owner);

	/// <summary>
	/// Checks whether <paramref name="position" /> places the window's title bar
	/// area on a working area of any connected screen. Used to prevent restoring
	/// the window onto a disconnected monitor.
	/// </summary>
	internal static bool IsWindowPositionOnScreen(Window window, PixelPoint position)
	{
		IReadOnlyList<Screen>? screens = window
			.Screens?
			.All;

		if (screens is null || screens.Count == 0)
		{
			return true;
		}

		// Roughly title-bar sized area at the top-left of the window — must be
		// reachable by the user so they can drag the window back.
		PixelRect titleBarRect = new(position.X, position.Y, 100, 30);

		return screens.Any(x => x.WorkingArea.Intersects(titleBarRect));
	}

	/// <summary>
	/// Sets default <see cref="Window.WindowStartupLocation" /> to the window.
	/// </summary>
	internal static void SetDefaultLocation(Window window)
	{
		window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
	}

	/// <summary>
	/// Sets default <see cref="INavigationColumnViewModel.NavigationColumnWidth" />.
	/// </summary>
	internal static void SetDefaultNavigationColumnWidth(FavoritesViewModel viewModel)
	{
		viewModel
			.FavoritesSettings
			.NavigationColumnWidth = viewModel.PopupWidth / 2.0;
	}

	/// <summary>
	/// Sets default <see cref="INavigationColumnViewModel.NavigationColumnWidth" />.
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
