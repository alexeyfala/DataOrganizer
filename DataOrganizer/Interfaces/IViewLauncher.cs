using Avalonia.Controls;
using Avalonia.Layout;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
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
	static Size DefaultWindowSize { get; } = new(900, 600);
	#endregion

	#region Methods
	/// <summary>
	/// Configures <see cref="EditorWindow" />.
	/// </summary>
	EditorWindow ConfigureEditorWindow(
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		IEnumerable<FileModelDto> editFiles,
		IEnumerable<FileModelDto> executedFiles,
		in Guid showObjectId = default);

	/// <summary>
	/// Configures <see cref="FavoritesWindow" />.
	/// </summary>
	FavoritesWindow ConfigureFavoritesWindow(
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		IEnumerable<FileModelDto> editFiles,
		IEnumerable<FileModelDto> executedFiles);

	/// <summary>
	/// Configures the main application window.
	/// </summary>
	Window ConfigureMainWindow(IEnumerable<ExplorerModelBaseDto> hierarchy);

	/// <summary>
	/// Saves <see cref="EditorWindow" /> settings to the file.
	/// </summary>
	Task SaveEditorSettingsAsync(EditorWindow window, CancellationToken token = default);

	/// <summary>
	/// Saves <see cref="FavoritesWindow" /> settings to the file.
	/// </summary>
	Task SaveFavoritesSettingsAsync(FavoritesWindow window, CancellationToken token = default);

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
