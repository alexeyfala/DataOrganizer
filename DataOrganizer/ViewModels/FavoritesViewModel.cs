using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
/// View model for <c>FavoritesWindow</c>.
/// </summary>
public sealed partial class FavoritesViewModel : ViewModelBase, IDisposable
{
	#region Properties
	/// <inheritdoc cref="FavoritesViewSettings" />
	public FavoritesViewSettings FavoritesSettings { get; } = new();

	/// <summary>
	/// <c>True</c> when the popup should be fixed.
	/// </summary>
	[ObservableProperty]
	public partial bool IsPopupFixed { get; set; }

	/// <summary>
	/// Controls the display of the popup panel.
	/// </summary>
	[ObservableProperty]
	public partial bool IsPopupOpen { get; set; }

	/// <inheritdoc cref="FavoritesPopupContentType" />
	[ObservableProperty]
	public partial FavoritesPopupContentType PopupContent { get; set; }

	/// <inheritdoc cref="FavoritesWindowSettings.PopupHeight" />
	[ObservableProperty]
	public partial double PopupHeight { get; set; }

	/// <inheritdoc cref="FavoritesWindowSettings.PopupWidth" />
	[ObservableProperty]
	public partial double PopupWidth { get; set; }
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="IsPopupFixed" /> changes.
	/// </summary>
	partial void OnIsPopupFixedChanged(bool value)
	{
		if (!value)
		{
			IsPopupOpen = false;

			return;
		}

		// The RestorePopupContent method must be executed in DispatcherPriority.Background
		// otherwise the UI will freeze.
		_dispatcher.Post(RestorePopupContent, DispatcherPriority.Background);
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
	[RelayCommand(CanExecute = nameof(CanShowFavorites))]
	public void ShowFavorites()
	{
		_logger.LogInformation("Show favorites");

		SaveContent();

		ShowContentInPopup(FavoritesPopupContentType.Favorites);
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
	/// Displays the copy history in the popup panel.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanShowCopyHistory))]
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
	private Task ShowEditor(FavoritesWindow? window)
	{
		if (window is null)
		{
			return Task.CompletedTask;
		}

		return ShowInEditorAsync(default, window);
	}
	#endregion

	#region Data
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
		IDispatcherAccessor dispatcher,
		IEntityEncryption entityEncryption,
		IEventSimulator eventSimulator,
		IExecutionEngine executionEngine,
		ILogger logger,
		IMessenger messenger,
		ITaskExceptionHandler exceptionHandler,
		IViewLauncher viewLauncher,
		Lazy<IKeyboardInputHook> keyboardInputHook) : base(
			app,
			settingsManager,
			clipboard,
			dbAccess,
			dialogService,
			dispatcher,
			entityEncryption,
			eventSimulator,
			executionEngine,
			logger,
			messenger,
			exceptionHandler,
			viewLauncher,
			keyboardInputHook)
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

	/// <summary>
	/// Performs initialization.
	/// </summary>
	public void Initialize(
		Window window,
		FavoritesWindowSettings windowSettings,
		FavoritesViewSettings favoritesSettings,
		CopyHistoryViewSettings copyHistorySettings)
	{
		PixelPoint savedPosition = new(windowSettings.X, windowSettings.Y);

		if (windowSettings.X > 0
			&& windowSettings.Y > 0
			&& IViewLauncher.IsWindowPositionOnScreen(window, savedPosition))
		{
			window.Position = savedPosition;
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

		CopyHistorySettings.AddItems(copyHistorySettings.Items, Hierarchy);

		if (CopyHistorySettings
			.Items
			.Count > 0)
		{
			CopyHistorySettings.SelectedItemId = copyHistorySettings.SelectedItemId;
		}

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
		Guid id,
		Window window,
		CancellationToken _ = default)
	{
		IsShutdown = false;

		if (IsPopupFixed)
		{
			SaveContent();
		}

		window.Close();

		_viewLauncher.ConfigureEditorWindow(
			Hierarchy,
			OpenedInEditorFiles,
			ExecutingFiles,
			id).Show();

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		base.AfterDispose();

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
			.Items
			.Clear();

		PopupContent = FavoritesPopupContentType.None;

		_copyHistory?.Dispose();

		_favorites?.Dispose();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="ShowCopyHistoryCommand" />.
	/// </summary>
	private bool CanShowCopyHistory() => PopupContent != FavoritesPopupContentType.CopyHistory;

	/// <summary>
	/// Validates <see cref="ShowFavoritesCommand" />.
	/// </summary>
	private bool CanShowFavorites() => PopupContent != FavoritesPopupContentType.Favorites;

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
	/// Restores the content for popup.
	/// </summary>
	private void RestorePopupContent()
	{
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
	private void ShowContentInPopup(FavoritesPopupContentType content)
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
