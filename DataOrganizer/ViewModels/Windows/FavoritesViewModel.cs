using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Enums.Views;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Updates;
using DataOrganizer.Windows;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels.Windows;

/// <summary>
/// View model for <c>FavoritesWindow</c>.
/// </summary>
public sealed partial class FavoritesViewModel : ViewModelBase, IRecipient<ConfirmUpdateMessage>
{
	#region Properties
	/// <inheritdoc cref="FavoritesViewSettings" />
	public FavoritesViewSettings FavoritesSettings { get; } = new();

	/// <summary>
	/// <c>True</c> when the popup should be fixed.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(ShowPopupOnHoverCommand))]
	public partial bool IsPopupFixed { get; set; }

	/// <summary>
	/// Controls the display of the popup panel.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(ShowPopupOnHoverCommand))]
	public partial bool IsPopupOpen { get; set; }

	/// <summary>
	/// <c>True</c> when opening the popup on hover is enabled in settings.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(ShowPopupOnHoverCommand))]
	public partial bool IsShowOnHoverEnabled { get; set; }

	/// <inheritdoc cref="FavoritesPopupContentKind" />
	[ObservableProperty]
	public partial FavoritesPopupContentKind PopupContent { get; set; }

	/// <inheritdoc cref="FavoritesWindowSettings.PopupHeight" />
	[ObservableProperty]
	public partial double PopupHeight { get; set; }

	/// <inheritdoc cref="FavoritesWindowSettings.PopupWidth" />
	[ObservableProperty]
	public partial double PopupWidth { get; set; }

	/// <summary>
	/// The favorites while the popup shows them.
	/// </summary>
	[ObservableProperty]
	public partial SelectedFavoritesViewModel? SelectedFavorites { get; private set; }
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

		// A popup opened unfixed holds a light-dismiss overlay that is released only on close, and the
		// overlay stops dismissing the popup once it is fixed, swallowing every press over the window.
		IsPopupOpen = false;

		// The RestorePopupContent method must be executed in UiPriority.Background
		// otherwise the UI will freeze.
		_dispatcher.Post(RestorePopupContent, UiPriority.Background);
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

		PopupContent = FavoritesPopupContentKind.None;

		UpdateCommands();

		if (_app.FindDialogHost(DialogHostIdentifiers.Main) is not { } dialogHost || !dialogHost.IsOpen)
		{
			return;
		}

		dialogHost.IsOpen = false;
	}

	/// <summary>
	/// Called when <see cref="PopupContent" /> changes.
	/// </summary>
	partial void OnPopupContentChanged(
		FavoritesPopupContentKind oldValue,
		FavoritesPopupContentKind newValue)
	{
		_previousPopupContent = oldValue;

		if (oldValue == FavoritesPopupContentKind.CopyHistory)
		{
			CloseCopyHistoryList();
		}
		else if (oldValue == FavoritesPopupContentKind.Favorites)
		{
			CloseSelectedFavorites();
		}

		if (newValue == FavoritesPopupContentKind.CopyHistory)
		{
			OpenCopyHistoryList();
		}
		else if (newValue == FavoritesPopupContentKind.Favorites)
		{
			OpenSelectedFavorites();
		}
	}
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Closes the popup by Esc key.
	/// </summary>
	[RelayCommand]
	internal void ClosePopupByEsc()
	{
		IsPopupFixed = false;

		IsPopupOpen = false;
	}

	/// <summary>
	/// Displays the favorites in the popup panel.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanShowFavorites))]
	internal void ShowFavorites()
	{
		_logger.LogInformation("Show favorites");

		SaveContent();

		ShowContentInPopup(FavoritesPopupContentKind.Favorites);
	}

	/// <summary>
	/// Opens the popup with the restored content on hovering over the fix toggle.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanShowPopupOnHover))]
	internal void ShowPopupOnHover() => _dispatcher.Post(RestorePopupContent, UiPriority.Background);

	/// <summary>
	/// Displays the copy history in the popup panel.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanShowCopyHistory))]
	private void ShowCopyHistory()
	{
		_logger.LogInformation("Show copy history");

		SaveContent();

		ShowContentInPopup(FavoritesPopupContentKind.CopyHistory);
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
	/// <inheritdoc cref="IMainWindowLauncher" />
	private readonly IMainWindowLauncher _mainWindowLauncher;

	/// <summary>
	/// Previous <see cref="PopupContent" /> value.
	/// </summary>
	private FavoritesPopupContentKind _previousPopupContent;
	#endregion

	#region Constructors
	public FavoritesViewModel(
		Application app,
		IAppSettingsStore settingsStore,
		IContentVisibility contentVisibility,
		ICopyHistory copyHistory,
		IDialogService dialogService,
		IDispatcherAccessor dispatcher,
		IExecutingFiles executingFiles,
		ILogger logger,
		IMainWindowLauncher mainWindowLauncher,
		IMessenger messenger,
		INotificationService notification,
		ISecondaryWindowLauncher secondaryWindowLauncher,
		ISessionEditorTabs sessionEditorTabs,
		ISessionHierarchy sessionHierarchy,
		ITaskExceptionHandler exceptionHandler,
		IViewFactory viewFactory) : base(
			app,
			settingsStore,
			contentVisibility,
			copyHistory,
			dialogService,
			dispatcher,
			executingFiles,
			logger,
			messenger,
			notification,
			secondaryWindowLauncher,
			sessionEditorTabs,
			sessionHierarchy,
			exceptionHandler,
			viewFactory)
	{
		_mainWindowLauncher = mainWindowLauncher;

		IsShowOnHoverEnabled = settingsStore.Settings is { ShowFavoritesOnHover: true };

		FavoritesSettings
			.Categories
			.AddRange(GetCategories(Hierarchy));
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Receive(ConfirmUpdateMessage message) => message.Reply(ConfirmUpdateAsync(message.Text, message.Token));

	/// <summary>
	/// Restores the popup size and the favorites from the stored settings.
	/// </summary>
	public void Initialize(FavoritesWindowSettings windowSettings, FavoritesViewSettings favoritesSettings)
	{
		if (windowSettings.PopupWidth > 0.0 && windowSettings.PopupHeight > 0.0)
		{
			PopupWidth = windowSettings.PopupWidth;

			PopupHeight = windowSettings.PopupHeight;
		}
		else
		{
			SetDefaultPopupSize();
		}

		if (favoritesSettings.NavigationColumnWidth > default(double))
		{
			FavoritesSettings.NavigationColumnWidth = favoritesSettings.NavigationColumnWidth;
		}
		else
		{
			SetDefaultNavigationColumnWidth();
		}

		FavoritesSettings.SelectedCategoryId = favoritesSettings.SelectedCategoryId;

		FavoritesSettings.SelectedPairs = favoritesSettings.SelectedPairs;

		FavoritesSettings.OrderedCategoryIds = favoritesSettings.OrderedCategoryIds;

		IsInitialized = true;
	}

	/// <summary>
	/// Saves the favorites while the popup shows them.
	/// </summary>
	public void SaveContent()
	{
		if (PopupContent != FavoritesPopupContentKind.Favorites)
		{
			return;
		}

		SaveFavorites();
	}

	/// <inheritdoc />
	public override Task ShowInEditorAsync(
		Guid id,
		Window window,
		CancellationToken _ = default)
	{
		window.Close();

		_mainWindowLauncher
			.CreateEditorWindow(id)
			.Show();

		return Task.CompletedTask;
	}

	/// <summary>
	/// Sets the default <see cref="FavoritesViewSettings.NavigationColumnWidth" />.
	/// </summary>
	internal void SetDefaultNavigationColumnWidth()
	{
		FavoritesSettings.NavigationColumnWidth = PopupWidth / 2.0;
	}

	/// <summary>
	/// Sets the default <see cref="PopupHeight" /> and <see cref="PopupWidth" />.
	/// </summary>
	internal void SetDefaultPopupSize()
	{
		PopupHeight = 250.0;

		PopupWidth = PopupHeight * 2.0;
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
			.OrderedCategoryIds
			.Clear();

		FavoritesSettings
			.SelectedPairs
			.Clear();

		// Leaving the content disposes its view model.
		PopupContent = FavoritesPopupContentKind.None;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="ShowCopyHistoryCommand" />.
	/// </summary>
	private bool CanShowCopyHistory() => PopupContent != FavoritesPopupContentKind.CopyHistory;

	/// <summary>
	/// Validates <see cref="ShowFavoritesCommand" />.
	/// </summary>
	private bool CanShowFavorites() => PopupContent != FavoritesPopupContentKind.Favorites;

	/// <summary>
	/// Validates <see cref="ShowPopupOnHoverCommand" />.
	/// </summary>
	private bool CanShowPopupOnHover() => IsShowOnHoverEnabled && !IsPopupFixed && !IsPopupOpen;

	/// <summary>
	/// Disposes the favorites that the popup no longer shows.
	/// </summary>
	private void CloseSelectedFavorites()
	{
		SelectedFavorites?.Dispose();

		SelectedFavorites = null;
	}

	/// <summary>
	/// Asks whether to open the page of a newer application version, over the favorites shown in the popup.
	/// </summary>
	private Task<bool> ConfirmUpdateAsync(string text, CancellationToken token)
	{
		bool wasPopupOpen = IsPopupOpen;

		ShowContentInPopup(FavoritesPopupContentKind.Favorites);

		return _dispatcher.PostAsync(async () =>
		{
			try
			{
				return await _dialogService
					.RequestYesNoAsync(text, token: token)
					.ConfigureAwait(true);
			}
			finally
			{
				if (!wasPopupOpen)
				{
					IsPopupOpen = false;
				}
			}
		}, UiPriority.Background);
	}

	/// <summary>
	/// Returns a flat sequence of <see cref="FavoriteCategory" />.
	/// </summary>
	private IEnumerable<FavoriteCategory> GetCategories(IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		List<FileDto> files = [.. hierarchy
			.OfType<FileDto>()
			.Where(x => x.IsFavorite)];

		if (files.Count > 0)
		{
			FolderDto? parent = files[0].Parent;

			yield return new()
			{
				Children = files,
				EncryptionStatus = parent?.EncryptionStatus ?? EncryptionStatus.None,
				Id = parent is not null ? parent.Id : Guid.Parse("210B84EF-06EA-4B70-97E8-DC4BE4DD6195"),
				Index = FavoritesSettings.Categories.Count,
				Name = parent?.Name ?? "Root"
			};
		}

		foreach (FolderDto folder in hierarchy.OfType<FolderDto>())
		{
			foreach (FavoriteCategory category in GetCategories(folder.Children))
			{
				yield return category;
			}
		}
	}

	/// <summary>
	/// Creates the favorites that the popup starts to show, from the stored settings.
	/// </summary>
	private void OpenSelectedFavorites()
	{
		SelectedFavoritesViewModel favorites = _viewFactory.CreateViewModel<SelectedFavoritesViewModel>();

		favorites.Initialize(
			FavoritesSettings.NavigationColumnWidth,
			FavoritesSettings.SelectedCategoryId,
			FavoritesSettings.Categories,
			FavoritesSettings.OrderedCategoryIds,
			FavoritesSettings.SelectedPairs);

		SelectedFavorites = favorites;
	}

	/// <summary>
	/// Restores the content for popup.
	/// </summary>
	private void RestorePopupContent()
	{
		if (_previousPopupContent != FavoritesPopupContentKind.None)
		{
			switch (_previousPopupContent)
			{
				case FavoritesPopupContentKind.CopyHistory:
					ShowCopyHistory();
					break;

				case FavoritesPopupContentKind.Favorites:
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
	/// Stores the current state of the favorites view in <see cref="FavoritesSettings" />.
	/// </summary>
	private void SaveFavorites()
	{
		if (SelectedFavorites is not { } favorites)
		{
			return;
		}

		_logger.LogInformation("Save favorites");

		FavoritesSettings.NavigationColumnWidth = favorites
			.NavigationColumnWidth
			.Value;

		if (favorites.SelectedCategory is { } category)
		{
			FavoritesSettings.SelectedCategoryId = category.Id;
		}

		FavoritesSettings
			.SelectedPairs
			.ClearAddRange(favorites.SelectedPairs);

		FavoritesSettings
			.OrderedCategoryIds
			.ClearAddRange(favorites.OrderedCategoryIds);
	}

	/// <summary>
	/// Sets <see cref="PopupContent" /> from <paramref name="content"/>,
	/// <see cref="IsPopupOpen" /> to <c>True</c> and updates commands.
	/// </summary>
	private void ShowContentInPopup(FavoritesPopupContentKind content)
	{
		PopupContent = content;

		IsPopupOpen = true;

		UpdateCommands();
	}

	/// <summary>
	/// Re-evaluates the commands that switch the popup content.
	/// </summary>
	private void UpdateCommands()
	{
		ShowFavoritesCommand.NotifyCanExecuteChanged();

		ShowCopyHistoryCommand.NotifyCanExecuteChanged();
	}
	#endregion
}
