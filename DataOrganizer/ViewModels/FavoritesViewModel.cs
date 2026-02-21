using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using DataOrganizer.Windows;
using DynamicData;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using SharpHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="FavoritesWindow" />.
/// </summary>
public sealed partial class FavoritesViewModel : ViewModelBase, IDisposable
{
	#region Properties
	/// <inheritdoc cref="FavoritesViewSettings" />
	public FavoritesViewSettings FavoritesSettings { get; } = new();

	/// <summary>
	/// The window height.
	/// </summary>
	public double ViewHeight { get; set; }

	/// <summary>
	/// The window width.
	/// </summary>
	public double ViewWidth { get; set; }
	#endregion

	#region Auto-Generated Properties
	/// <summary>
	/// Returns <c>True</c> if popup should be fixed.
	/// </summary>
	[ObservableProperty]
	private bool _isPopupFixed;

	/// <summary>
	/// Controls the display of the popup panel.
	/// </summary>
	[ObservableProperty]
	private bool _isPopupOpen;

	/// <summary>
	/// The content of the popup panel.
	/// </summary>
	[ObservableProperty]
	private object? _popupContent;

	/// <inheritdoc cref="FavoritesWindowSettings.PopupHeight" />
	[ObservableProperty]
	private double _popupHeight;

	/// <inheritdoc cref="FavoritesWindowSettings.PopupWidth" />
	[ObservableProperty]
	private double _popupWidth;

	/// <summary>
	/// Controls the display of the content copy history in the popup panel.
	/// </summary>
	[ObservableProperty]
	private bool _showContentCopyHistory;

	/// <summary>
	/// Controls the display of the favorites in the popup panel.
	/// </summary>
	[ObservableProperty]
	private bool _showFavorites;
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="IsPopupFixed" /> changes.
	/// </summary>
	async partial void OnIsPopupFixedChanged(bool value)
	{
		if (!value)
		{
			IsPopupOpen = false;

			return;
		}

		// Without delay the window freezes.
		await Task
			.Delay(100)
			.ConfigureAwait(true);

		if (PopupContent is CopyHistoryView)
		{
			ShowContentCopyHistory = true;

			return;
		}

		ShowFavorites = true;
	}

	/// <summary>
	/// Called when <see cref="IsPopupOpen" /> changes.
	/// </summary>
	partial void OnIsPopupOpenChanged(bool value)
	{
		if (value)
		{
			if (PopupContent is null)
			{
				_logger.LogError(
					$"{nameof(PopupContent)} should not be null if {nameof(IsPopupOpen)} is True",
					isAssertDebug: false);
			}

			return;
		}

		ShowContentCopyHistory = false;

		ShowFavorites = false;
	}

	/// <summary>
	/// Called when <see cref="ShowContentCopyHistory" /> changes.
	/// </summary>
	partial void OnShowContentCopyHistoryChanged(bool value)
	{
		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		if (value)
		{
			DisplayCopyHistory();
		}
		else
		{
			SaveCopyHistory();
		}
	}

	/// <summary>
	/// Called when <see cref="ShowFavorites" /> changes.
	/// </summary>
	partial void OnShowFavoritesChanged(bool value)
	{
		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		if (value)
		{
			DisplayFavorites();
		}
		else
		{
			SaveFavorites();
		}
	}
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Closes the popup.
	/// </summary>
	[RelayCommand]
	public void ClosePopup()
	{
		IsPopupFixed = false;

		IsPopupOpen = false;
	}

	/// <summary>
	/// Closes window.
	/// </summary>
	[RelayCommand]
	private static void Close(Window? window) => window?.Close();

	/// <summary>
	/// <see cref="InputElement.PointerPressed" /> event handler of <see cref="Border" />.
	/// </summary>
	[RelayCommand]
	private static void PointerPressed(PointerPressedEventArgs? e)
	{
		if (e?.Source is not Visual visual || !e.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
		{
			return;
		}

		visual
			.FindLogicalParent<Window>()?
			.BeginMoveDrag(e);
	}

	/// <summary>
	/// Handles <see cref="Thumb.DragDelta" /> event on left side of the popup.
	/// </summary>
	[RelayCommand]
	private void ResizePopupByLeft(VectorEventArgs? e)
	{
		if (e is null)
		{
			return;
		}

		double width = PopupWidth - e.Vector.X;

		if (width <= CalculateMinimumPopupWidth())
		{
			return;
		}

		PopupWidth = width;
	}

	/// <summary>
	/// Handles <see cref="Thumb.DragDelta" /> event on top right side of the popup.
	/// </summary>
	[RelayCommand]
	private void ResizePopupByRight(VectorEventArgs? e)
	{
		if (e is null)
		{
			return;
		}

		double width = PopupWidth + e.Vector.X;

		if (width <= CalculateMinimumPopupWidth())
		{
			return;
		}

		PopupWidth = width;
	}

	/// <summary>
	/// Handles <see cref="Thumb.DragDelta" /> event on top side of the popup.
	/// </summary>
	[RelayCommand]
	private void ResizePopupByTop(VectorEventArgs? e)
	{
		if (e is null)
		{
			return;
		}

		double height = PopupHeight - e.Vector.Y;

		if (height <= ViewHeight)
		{
			return;
		}

		PopupHeight = height;
	}

	/// <summary>
	/// Handles <see cref="Thumb.DragDelta" /> event on top left corner of the popup.
	/// </summary>
	[RelayCommand]
	private void ResizePopupByTopLeftCorner(VectorEventArgs? e)
	{
		if (e is null)
		{
			return;
		}

		double width = PopupWidth - e.Vector.X;

		double height = PopupHeight - e.Vector.Y;

		if (width <= CalculateMinimumPopupWidth() || height <= ViewHeight)
		{
			return;
		}

		PopupWidth = width;

		PopupHeight = height;
	}

	/// <summary>
	/// Handles <see cref="Thumb.DragDelta" /> event on top right corner of the popup.
	/// </summary>
	[RelayCommand]
	private void ResizePopupByTopRightCorner(VectorEventArgs? e)
	{
		if (e is null)
		{
			return;
		}

		double width = PopupWidth + e.Vector.X;

		double height = PopupHeight - e.Vector.Y;

		if (width <= CalculateMinimumPopupWidth() || height <= ViewHeight)
		{
			return;
		}

		PopupWidth = width;

		PopupHeight = height;
	}

	/// <summary>
	/// Displays the "Editor" window.
	/// </summary>
	[RelayCommand]
	private Task ShowEditor(FavoritesWindow? window) => ShowInEditorAsync(window, default);
	#endregion

	#region Constructors
	public FavoritesViewModel(
		Application app,
		IAppSettingsManager settingsManager,
		IDbAccess dbAccess,
		IDispatcher dispatcher,
		IEncryptionService encryption,
		IEntityEcryption entityEcryption,
		IEventSimulator eventSimulator,
		IKeyboardInputHook keyboardInputHook,
		ILogger logger,
		IViewFactory viewFactory,
		IViewLauncher viewLauncher) : base(
			app,
			settingsManager,
			dbAccess,
			dispatcher,
			encryption,
			entityEcryption,
			eventSimulator,
			keyboardInputHook,
			logger,
			viewFactory,
			viewLauncher)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public override void AddHierarchy(IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		Hierarchy.AddRange(hierarchy);

		FavoritesSettings
			.Categories
			.AddRange(GetCategories(hierarchy));
	}

	/// <inheritdoc />
	public override void DisplayCopyHistory()
	{
		_logger.LogInformation($@"Show ""{nameof(CopyHistoryView)}""");

		ShowFavorites = false;

		CopyHistoryView view = _viewFactory.CreateUserControl<CopyHistoryView>();

		view.Focusable = true;

		view.ViewModel.Initialize(
			Hierarchy.FilterFilesById(CopyHistorySettings.CopyHistory),
			CopyHistorySettings.SelectedCopyHistoryItemId);

		PopupContent = view;

		IsPopupOpen = true;

		view.Focus();
	}

	/// <summary>
	/// Displays <see cref="SelectedFavoritesView" /> in <see cref="PopupContent" />.
	/// </summary>
	public void DisplayFavorites()
	{
		_logger.LogInformation($@"Show ""{nameof(SelectedFavoritesView)}""");

		ShowContentCopyHistory = false;

		SelectedFavoritesView view = _viewFactory.CreateUserControl<SelectedFavoritesView>();

		view.Focusable = true;

		view.ViewModel.Initialize(
			FavoritesSettings.NavigationColumnWidth,
			FavoritesSettings.SelectedCategoryId,
			FavoritesSettings.Categories,
			FavoritesSettings.OrderedCategories,
			FavoritesSettings.SelectedPairs);

		PopupContent = view;

		IsPopupOpen = true;

		view.Focus();
	}

	/// <inheritdoc />
	public void Dispose()
	{
		FavoritesSettings
			.Categories
			.ForEach(x => x.Children.Clear());

		FavoritesSettings
			.Categories
			.Clear();

		FavoritesSettings
			.OrderedCategories
			.Clear();

		FavoritesSettings
			.SelectedPairs
			.Clear();

		CopyHistorySettings
			.CopyHistory
			.Clear();

		PopupContent = null;
	}

	/// <summary>
	/// Performs initialization.
	/// </summary>
	public void Initialize(
		Window window,
		FavoritesWindowSettings windowSettings,
		FavoritesViewSettings favoritesSettings,
		CopyHistoryViewSettings copyHistorySettings)
	{
		if (windowSettings.X > 0 && windowSettings.Y > 0)
		{
			window.Position = new(windowSettings.X, windowSettings.Y);
		}
		else
		{
			IViewLauncher.SetDefaultLocation(window);
		}

		if (windowSettings.PopupWidth > 0.0 && windowSettings.PopupHeight > 0.0)
		{
			PopupWidth = windowSettings.PopupWidth;

			PopupHeight = windowSettings.PopupHeight;
		}
		else
		{
			IViewLauncher.SetDefaultPopupSize(this);
		}

		if (favoritesSettings.NavigationColumnWidth > default(double))
		{
			FavoritesSettings.NavigationColumnWidth = favoritesSettings.NavigationColumnWidth;
		}
		else
		{
			IViewLauncher.SetDefaultNavigationColumnWidth(this);
		}

		FavoritesSettings.SelectedCategoryId = favoritesSettings.SelectedCategoryId;

		FavoritesSettings.SelectedPairs = favoritesSettings.SelectedPairs;

		FavoritesSettings.OrderedCategories = favoritesSettings.OrderedCategories;

		CopyHistorySettings.CopyHistory = copyHistorySettings.CopyHistory;

		CopyHistorySettings.SelectedCopyHistoryItemId = copyHistorySettings.SelectedCopyHistoryItemId;

		IsInitialized = true;
	}

	/// <inheritdoc />
	public override void SaveCopyHistory()
	{
		if (PopupContent is not CopyHistoryView view)
		{
			return;
		}

		SaveCopyHistory(view.ViewModel);
	}

	/// <summary>
	/// Saves in <see cref="FavoritesSettings" /> values if <see cref="PopupContent" /> is <see cref="SelectedFavoritesView" />.
	/// </summary>
	public void SaveFavorites()
	{
		if (PopupContent is not SelectedFavoritesView view)
		{
			return;
		}

		FavoritesSettings.NavigationColumnWidth = view
			.ViewModel
			.NavigationColumnWidth
			.Value;

		if (view
			.ViewModel
			.SelectedCategory is { } category)
		{
			FavoritesSettings.SelectedCategoryId = category.Id;
		}

		FavoritesSettings
			.SelectedPairs
			.ClearAddRange(view.ViewModel.SelectedPairs);

		FavoritesSettings
			.OrderedCategories
			.ClearAddRange(view.ViewModel.OrderedCategories);

		view
			.ViewModel
			.Dispose();
	}

	/// <inheritdoc />
	public override Task ShowInEditorAsync(
		Window? window,
		Guid id,
		CancellationToken _ = default)
	{
		IsShutdown = false;

		ShowContentCopyHistory = false;

		ShowFavorites = false;

		window?.Close();

		_viewLauncher
			.ConfigureEditorWindow(Hierarchy, id)
			.Show();

		return Task.CompletedTask;
	}
	#endregion

	#region Service
	/// <summary>
	/// Return a flat sequence of <see cref="FavoriteCategory" />.
	/// </summary>
	private static IEnumerable<FavoriteCategory> GetCategories(IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		List<FileModelDto> files = [.. hierarchy
			.OfType<FileModelDto>()
			.Where(x => x.IsFavorite)];

		if (files.Count > 0)
		{
			FolderModelDto? parent = files[0].Parent;

			yield return new()
			{
				Children = files,
				EncryptionStatus = parent?.EncryptionStatus ?? EncryptionStatus.None,
				Id = parent is not null ? parent.Id : Guid.Parse("210B84EF-06EA-4B70-97E8-DC4BE4DD6195"),
				Name = parent?.Name ?? "Root"
			};
		}

		foreach (FolderModelDto folder in hierarchy.OfType<FolderModelDto>())
		{
			foreach (FavoriteCategory category in GetCategories(folder.Children))
			{
				yield return category;
			}
		}
	}

	/// <summary>
	/// Calculates the minimum allowed width of the popup panel.
	/// </summary>
	/// <remarks>
	/// The user must be given the option to reduce the width of the panel by a value less than the width of the window.
	/// There may be a situation when the user drags a window to the top edge of the monitor and the docked panel does not allow itself to be closed, even using the Escape key.
	/// </remarks>
	private double CalculateMinimumPopupWidth() => ViewWidth / 2.0;
	#endregion
}
