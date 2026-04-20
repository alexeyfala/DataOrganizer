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

	/// <inheritdoc cref="FavoritesPopupContentType" />
	[ObservableProperty]
	private FavoritesPopupContentType _popupContent;

	/// <inheritdoc cref="FavoritesWindowSettings.PopupHeight" />
	[ObservableProperty]
	private double _popupHeight;

	/// <inheritdoc cref="FavoritesWindowSettings.PopupWidth" />
	[ObservableProperty]
	private double _popupWidth;
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

		if (_previousPopupContent != FavoritesPopupContentType.None)
		{
			switch (_previousPopupContent)
			{
				case FavoritesPopupContentType.CopyHistory:
					ShowCopyHistory();
					break;

				case FavoritesPopupContentType.Favorites:
					ShowFavorites();
					break;
			}
		}
		else
		{
			ShowFavorites();
		}
	}

	/// <summary>
	/// Called when <see cref="IsPopupOpen" /> changes.
	/// </summary>
	partial void OnIsPopupOpenChanged(bool value)
	{
		if (value)
		{
			return;
		}

		SaveContent();

		PopupContent = FavoritesPopupContentType.None;

		UpdateCommands();

		if (_app.FindDialogHost() is not { } dialogHost || !dialogHost.IsOpen)
		{
			return;
		}

		dialogHost.IsOpen = false;
	}

	/// <summary>
	/// Called when <see cref="PopupContent" /> changes.
	/// </summary>
	partial void OnPopupContentChanged(
		FavoritesPopupContentType oldValue,
		FavoritesPopupContentType newValue) => _previousPopupContent = oldValue;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Closes the popup by Esc key.
	/// </summary>
	[RelayCommand]
	public void ClosePopupByEsc()
	{
		IsPopupFixed = false;

		IsPopupOpen = false;
	}

	/// <summary>
	/// Displays the favorites in the popup panel.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteShowFavorites))]
	public void ShowFavorites()
	{
		_logger.LogInformation("Show favorites");

		SaveContent();

		ShowContentInPopup(FavoritesPopupContentType.Favorites);
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
	/// Handles the display of the favorites.
	/// </summary>
	[RelayCommand]
	private void FavoritesDisplayed(SelectedFavoritesViewModel? viewModel)
	{
		_favorites = viewModel;

		viewModel?.Initialize(
			FavoritesSettings.NavigationColumnWidth,
			FavoritesSettings.SelectedCategoryId,
			FavoritesSettings.Categories,
			FavoritesSettings.OrderedCategories,
			FavoritesSettings.SelectedPairs);
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

		if (width <= MinimumPopupSize)
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

		if (width <= MinimumPopupSize)
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

		if (height <= MinimumPopupSize)
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

		if (width <= MinimumPopupSize || height <= MinimumPopupSize)
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

		if (width <= MinimumPopupSize || height <= MinimumPopupSize)
		{
			return;
		}

		PopupWidth = width;

		PopupHeight = height;
	}

	/// <summary>
	/// Displays the copy history in the popup panel.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteShowCopyHistory))]
	private void ShowCopyHistory()
	{
		_logger.LogInformation("Show copy history");

		SaveContent();

		ShowContentInPopup(FavoritesPopupContentType.CopyHistory);
	}

	/// <summary>
	/// Displays the "Editor" window.
	/// </summary>
	[RelayCommand]
	private Task ShowEditor(FavoritesWindow? window) => ShowInEditorAsync(window, default);
	#endregion

	#region Data
	/// <summary>
	/// Minimum size of the popup.
	/// </summary>
	private const double MinimumPopupSize = 100.0;

	/// <inheritdoc cref="SelectedFavoritesViewModel" />
	private SelectedFavoritesViewModel? _favorites;

	/// <summary>
	/// Previous <see cref="PopupContent" /> value.
	/// </summary>
	private FavoritesPopupContentType _previousPopupContent;
	#endregion

	#region Constructors
	public FavoritesViewModel(
		Application app,
		IAppSettingsManager settingsManager,
		IClipboardService clipboard,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IDispatcher dispatcher,
		IEntityEcryption entityEcryption,
		IEventSimulator eventSimulator,
		IKeyboardInputHook keyboardInputHook,
		ILogger logger,
		IViewFactory viewFactory,
		IViewLauncher viewLauncher) : base(
			app,
			settingsManager,
			clipboard,
			dbAccess,
			dialogService,
			dispatcher,
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

		PopupContent = FavoritesPopupContentType.None;

		_copyHistory = null;

		_favorites = null;
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

	/// <summary>
	/// Saves current content in popup.
	/// </summary>
	public void SaveContent()
	{
		if (PopupContent == FavoritesPopupContentType.None)
		{
			return;
		}

		if (PopupContent == FavoritesPopupContentType.Favorites)
		{
			SaveFavorites();
		}
		else if (PopupContent == FavoritesPopupContentType.CopyHistory)
		{
			SaveCopyHistory();
		}
	}

	/// <inheritdoc />
	public override Task ShowInEditorAsync(
		Window? window,
		Guid id,
		CancellationToken _ = default)
	{
		IsShutdown = false;

		if (IsPopupFixed)
		{
			SaveContent();
		}

		window?.Close();

		_viewLauncher.ConfigureEditorWindow(
			Hierarchy,
			OpenedInEditorFiles,
			ExecutingFiles,
			id).Show();

		return Task.CompletedTask;
	}
	#endregion

	#region Service
	/// <summary>
	/// Validates <see cref="ShowCopyHistoryCommand" />.
	/// </summary>
	private bool CanExecuteShowCopyHistory() => PopupContent != FavoritesPopupContentType.CopyHistory;

	/// <summary>
	/// Validates <see cref="ShowFavoritesCommand" />.
	/// </summary>
	private bool CanExecuteShowFavorites() => PopupContent != FavoritesPopupContentType.Favorites;

	/// <summary>
	/// Return a flat sequence of <see cref="FavoriteCategory" />.
	/// </summary>
	private IEnumerable<FavoriteCategory> GetCategories(IEnumerable<ExplorerModelBaseDto> hierarchy)
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
				Index = FavoritesSettings.Categories.Count,
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
	/// Saves in <see cref="FavoritesSettings" /> values.
	/// </summary>
	private void SaveFavorites()
	{
		if (_favorites is null)
		{
			return;
		}

		_logger.LogInformation("Save favorites");

		FavoritesSettings.NavigationColumnWidth = _favorites
			.NavigationColumnWidth
			.Value;

		if (_favorites.SelectedCategory is { } category)
		{
			FavoritesSettings.SelectedCategoryId = category.Id;
		}

		FavoritesSettings
			.SelectedPairs
			.ClearAddRange(_favorites.SelectedPairs);

		FavoritesSettings
			.OrderedCategories
			.ClearAddRange(_favorites.OrderedCategories);

		_favorites.Dispose();
	}

	/// <summary>
	/// Sets <see cref="PopupContent" /> from <paramref name="content"/>,
	/// <see cref="IsPopupOpen" /> to <c>True</c> and updates commands.
	/// </summary>
	private void ShowContentInPopup(in FavoritesPopupContentType content)
	{
		PopupContent = content;

		IsPopupOpen = true;

		UpdateCommands();
	}

	/// <summary>
	/// Updates commands.
	/// </summary>
	private void UpdateCommands()
	{
		ShowFavoritesCommand.NotifyCanExecuteChanged();

		ShowCopyHistoryCommand.NotifyCanExecuteChanged();
	}
	#endregion
}
