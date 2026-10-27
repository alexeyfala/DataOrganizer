using AppInstance.Interfaces;
using AppInstance.Messages;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Views;
using DataOrganizer.Extensions.Views;
using DataOrganizer.Helpers.Views;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Views;

/// <summary>
/// Creates, shows and closes the windows of the application and saves their settings.
/// </summary>
public class ViewLauncher :
	IMainWindowActivator,
	IMainWindowLauncher,
	INoticeLauncher,
	ISecondaryWindowCloser,
	ISecondaryWindowLauncher
{
	#region Data
	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IClipboardHistoryUnlocker" />
	private readonly IClipboardHistoryUnlocker _clipboardHistoryUnlocker;

	/// <inheritdoc cref="ICopyHistoryStore" />
	private readonly ICopyHistoryStore _copyHistory;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;

	/// <inheritdoc cref="IWindowFinder" />
	private readonly IWindowFinder _windowFinder;

	/// <inheritdoc cref="IWindowSettingsStore" />
	private readonly IWindowSettingsStore _windowSettings;
	#endregion

	#region Constructors
	public ViewLauncher(
		IAppEnvironment appEnvironment,
		IClipboardHistoryUnlocker clipboardHistoryUnlocker,
		ICopyHistoryStore copyHistory,
		ILogger logger,
		IMessenger messenger,
		IViewFactory viewFactory,
		IWindowFinder windowFinder,
		IWindowSettingsStore windowSettings)
	{
		_appEnvironment = appEnvironment;

		_clipboardHistoryUnlocker = clipboardHistoryUnlocker;

		_copyHistory = copyHistory;

		_logger = logger;

		_messenger = messenger;

		_viewFactory = viewFactory;

		_windowFinder = windowFinder;

		_windowSettings = windowSettings;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void ActivateMainWindow()
	{
		if (_windowFinder.FindMainWindow() is not { } window)
		{
			return;
		}

		window.RestoreAndActivate();
	}

	/// <inheritdoc />
	public void CenterMainWindow()
	{
		if (_windowFinder.FindMainWindow() is not { } window || _windowFinder.FindWindow<NotepadWindow>() is not { } owner)
		{
			return;
		}

		WindowPlacementHelper.BringToScreenCenter(window, owner);
	}

	/// <inheritdoc />
	public void CenterNotepadWindow()
	{
		if (_windowFinder.FindWindow<NotepadWindow>() is not { } window || _windowFinder.FindMainWindow() is not { } owner)
		{
			return;
		}

		WindowPlacementHelper.BringToScreenCenter(window, owner);
	}

	/// <inheritdoc />
	public async Task CloseSecondaryWindowsAsync()
	{
		if (_windowFinder.FindWindow<ConsoleWindow>(x => !x.ViewModel.IsSaved) is { } console)
		{
			// The console may put its closing off, so the wait follows the window rather than the call.
			Task closed = Observable
				.FromEventPattern<EventHandler, EventArgs>(
					x => console.Closed += x,
					x => console.Closed -= x)
				.FirstAsync()
				.ToTask();

			console.Close();

			await closed.ConfigureAwait(true);
		}

		// Closed here, so that it saves its settings while the services are still alive.
		if (_windowFinder.FindWindow<NotepadWindow>() is { } notepad)
		{
			notepad.Close();
		}
	}

	/// <summary>
	/// Configures <see cref="EditorWindow" />.
	/// </summary>
	internal EditorWindow CreateEditorWindow(in Guid showObjectId = default)
	{
		_logger.LogInformation($@"Opening ""{nameof(EditorWindow)}""");

		EditorViewModel viewModel = _viewFactory.CreateViewModel<EditorViewModel>();

		EditorWindow window = _viewFactory.CreateWindow<EditorWindow>(viewModel);

		window.Title = $"{_appEnvironment.GetAppInstanceName()} - {AppInfo.AppVersion}";

		viewModel.NotifyDecryptedContentsChanged();

		// The selection of the last session comes back first: selecting it again saves nothing, and an object shown then
		// takes its place as any other choice does.
		viewModel.RestoreSelection();

		if (showObjectId.IsNotDefault())
		{
			viewModel.ShowObject(showObjectId);
		}

		EditorWindowSettings? windowSettings = _windowSettings.Read<EditorWindowSettings>();

		// The stored size, when there is one, replaces the default.
		WindowPlacementHelper.SetDefaultSize(window);

		WindowPlacementHelper.Restore(window, windowSettings, WindowPlacementHelper.SetDefaultLocation);

		if (windowSettings is null || windowSettings.NavigationColumnWidth <= default(double))
		{
			viewModel.SetDefaultNavigationColumnWidth(window.Width);
		}

		if (windowSettings is not null)
		{
			viewModel.Initialize(windowSettings);
		}

		SaveOnClosing<EditorWindow, EditorWindowSettings>(window, SaveEditorSettings);

		return window;
	}

	/// <summary>
	/// Configures <see cref="FavoritesWindow" />.
	/// </summary>
	internal FavoritesWindow CreateFavoritesWindow()
	{
		_logger.LogInformation($@"Opening ""{nameof(FavoritesWindow)}""");

		FavoritesViewModel viewModel = _viewFactory.CreateViewModel<FavoritesViewModel>();

		FavoritesWindow window = _viewFactory.CreateWindow<FavoritesWindow>(viewModel);

		FavoritesWindowSettings? windowSettings = _windowSettings.Read<FavoritesWindowSettings>();

		WindowPlacementHelper.Restore(window, windowSettings, WindowPlacementHelper.SetDefaultLocation);

		if (windowSettings is not null)
		{
			viewModel.Initialize(windowSettings, _windowSettings.Read<FavoritesViewSettings>() ?? new());
		}
		else
		{
			viewModel.SetDefaultPopupSize();

			viewModel.SetDefaultNavigationColumnWidth();
		}

		SaveOnClosing<FavoritesWindow, FavoritesWindowSettings>(window, SaveFavoritesSettings);

		return window;
	}

	/// <inheritdoc />
	public async Task ShowClipboardLogWindowAsync()
	{
		if (_windowFinder.FindMainWindow() is not { } owner)
		{
			return;
		}

		if (_windowFinder.FindWindow<ClipboardLogWindow>() is { } existing)
		{
			WindowPlacementHelper.PositionAtScreenBottomRight(existing, owner);

			existing.Activate();

			return;
		}

		await _clipboardHistoryUnlocker
			.UnlockIfRequiredAsync()
			.ConfigureAwait(true);

		CreateClipboardLogWindow(owner).Show();
	}

	/// <inheritdoc />
	public void ShowMainWindow()
	{
		Window window = _windowSettings.Read<WindowKind>() switch
		{
			WindowKind.Editor => CreateEditorWindow(),
			WindowKind.Favorites => CreateFavoritesWindow(),
			_ => throw new NotImplementedException()
		};

		window.Show();
	}

	/// <inheritdoc />
	public void ShowNotepadWindow()
	{
		if (_windowFinder.FindWindow<NotepadWindow>() is { } existing)
		{
			existing.RestoreAndActivate();

			return;
		}

		if (_windowFinder.FindMainWindow() is not { } owner)
		{
			return;
		}

		CreateNotepadWindow(owner).Show();
	}

	/// <inheritdoc />
	public async Task ShowStartupErrorAsync(string databaseFilePath)
	{
		try
		{
			await ShowNoticeAsync(new()
			{
				ActionCaption = Strings.OpenDatabaseFolder,
				FilePath = databaseFilePath,
				IsTopmost = true,
				Message = Strings.DatabaseSchemaMismatch
			}).ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <inheritdoc />
	public void SwitchToEditor(Guid showObjectId = default)
	{
		CloseMainWindowToSwitch();

		CreateEditorWindow(showObjectId).Show();
	}

	/// <inheritdoc />
	public void SwitchToFavorites()
	{
		CloseMainWindowToSwitch();

		CreateFavoritesWindow().Show();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Configures <see cref="ClipboardLogWindow" />.
	/// </summary>
	private ClipboardLogWindow CreateClipboardLogWindow(Window owner)
	{
		_logger.LogInformation($@"Opening ""{nameof(ClipboardLogWindow)}""");

		ClipboardLogViewModel viewModel = _viewFactory.CreateViewModel<ClipboardLogViewModel>();

		ClipboardLogWindow window = _viewFactory.CreateWindow<ClipboardLogWindow>(viewModel);

		ClipboardLogWindowSettings? settings = _windowSettings.Read<ClipboardLogWindowSettings>();

		if (settings is not null)
		{
			viewModel.ActiveFilter = settings.ActiveFilter;

			viewModel.KeepOpen = settings.KeepOpen;
		}

		WindowPlacementHelper.Restore(window, settings, x => WindowPlacementHelper.PositionAtScreenBottomRight(x, owner));

		SaveOnClosing<ClipboardLogWindow, ClipboardLogWindowSettings>(window, SaveClipboardLogSettings);

		return window;
	}

	/// <summary>
	/// Closes the main window to give way to the other one; such a close asks for no exit.
	/// </summary>
	private void CloseMainWindowToSwitch()
	{
		if (_windowFinder.FindMainWindow() is not IMainWindow window)
		{
			return;
		}

		window.IsClosingToSwitch = true;

		window.Close();
	}

	/// <summary>
	/// Configures <see cref="NotepadWindow" />.
	/// </summary>
	private NotepadWindow CreateNotepadWindow(Window owner)
	{
		_logger.LogInformation($@"Opening ""{nameof(NotepadWindow)}""");

		NotepadViewModel viewModel = _viewFactory.CreateViewModel<NotepadViewModel>();

		viewModel.LoadTabs();

		NotepadWindow window = _viewFactory.CreateWindow<NotepadWindow>(viewModel);

		window.Title = $"{_appEnvironment.GetAppInstanceName()} - {Strings.Notepad}";

		// The stored size, when there is one, replaces the default.
		WindowPlacementHelper.SetDefaultSize(window);

		WindowPlacementHelper.Restore(
			window,
			_windowSettings.Read<NotepadWindowSettings>(),
			x => WindowPlacementHelper.PositionAtScreenCenter(x, owner));

		SaveOnClosing<NotepadWindow, NotepadWindowSettings>(window, SaveNotepadSettings);

		return window;
	}

	/// <summary>
	/// Asks for the exit of the application when a main window closes for any reason but a switch to the other one.
	/// </summary>
	private void ExitUnlessSwitching(IMainWindow window)
	{
		if (window.IsClosingToSwitch)
		{
			return;
		}

		_messenger.Send(new ExitAppMessage());
	}

	/// <summary>
	/// Saves <see cref="ClipboardLogWindow" /> settings to the file, then disposes its view model.
	/// </summary>
	private void SaveClipboardLogSettings(ClipboardLogWindow window)
	{
		try
		{
			ClipboardLogWindowSettings settings = new()
			{
				ActiveFilter = window.ViewModel.ActiveFilter,
				KeepOpen = window.ViewModel.KeepOpen,
				Size = window.Placement.Size,
				X = window.Placement.Position.X,
				Y = window.Placement.Position.Y,
			};

			_windowSettings.Write(settings);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			window
				.ViewModel
				.Dispose();
		}
	}

	/// <summary>
	/// Saves <see cref="EditorWindow" /> settings to the file, then disposes its view model.
	/// </summary>
	private void SaveEditorSettings(EditorWindow window)
	{
		try
		{
			EditorWindowSettings settings = new()
			{
				IsReadOnly = window.ViewModel.IsReadOnly,
				IsTopmost = window.Topmost,
				NavigationColumnWidth = window.ViewModel.NavigationColumnWidth,
				Size = window.Placement.Size,
				WindowState = window.Placement.ShowState,
				X = window.Placement.Position.X,
				Y = window.Placement.Position.Y
			};

			_windowSettings.Write(settings);

			_copyHistory.Save();

			_windowSettings.Write(WindowKind.Editor);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			window
				.ViewModel
				.Dispose();
		}
	}

	/// <summary>
	/// Saves <see cref="FavoritesWindow" /> settings to the file, then disposes its view model.
	/// </summary>
	private void SaveFavoritesSettings(FavoritesWindow window)
	{
		// The popup is still open here when it is fixed or when a command from inside it closes the window, so its
		// content is saved before the settings are written.
		window
			.ViewModel
			.SaveContent();

		try
		{
			FavoritesWindowSettings settings = new()
			{
				PopupHeight = window.ViewModel.PopupHeight,
				PopupWidth = window.ViewModel.PopupWidth,
				X = window.Placement.Position.X,
				Y = window.Placement.Position.Y
			};

			_windowSettings.Write(settings);

			_windowSettings.Write(window.ViewModel.FavoritesSettings);

			_copyHistory.Save();

			_windowSettings.Write(WindowKind.Favorites);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			window
				.ViewModel
				.Dispose();
		}
	}

	/// <summary>
	/// Saves <see cref="NotepadWindow" /> settings to the file, then disposes its view model.
	/// </summary>
	private void SaveNotepadSettings(NotepadWindow window)
	{
		try
		{
			NotepadWindowSettings settings = new()
			{
				IsTopmost = window.Topmost,
				Size = window.Placement.Size,
				WindowState = window.Placement.ShowState,
				X = window.Placement.Position.X,
				Y = window.Placement.Position.Y
			};

			_windowSettings.Write(settings);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			window
				.ViewModel
				.Dispose();
		}
	}

	/// <summary>
	/// Saves the settings of <paramref name="window" /> by <paramref name="save" /> once, as the window closes; a main
	/// window then asks for the exit of the application.
	/// </summary>
	private void SaveOnClosing<TWindow, TSettings>(TWindow window, Action<TWindow> save)
		where TWindow : Window
	{
		Observable
			.FromEventPattern<EventHandler<WindowClosingEventArgs>, WindowClosingEventArgs>(
				x => window.Closing += x,
				x => window.Closing -= x)
			.Take(1)
			.Subscribe(_ =>
			{
				_logger.LogInformation($@"Closing ""{typeof(TWindow).Name}"" and saving ""{typeof(TSettings).Name}""");

				save(window);

				if (window is IMainWindow mainWindow)
				{
					ExitUnlessSwitching(mainWindow);
				}
			});
	}

	/// <summary>
	/// Shows a notice in a window of its own and waits until it is closed; needs no other window.
	/// </summary>
	private Task ShowNoticeAsync(NoticeParameters parameters)
	{
		NoticeViewModel viewModel = _viewFactory.CreateViewModel<NoticeViewModel>();

		viewModel.ActionCaption = parameters.ActionCaption;

		viewModel.FilePath = parameters.FilePath;

		viewModel.IsTopmost = parameters.IsTopmost;

		viewModel.Message = parameters.Message;

		viewModel.Title = parameters.Title;

		NoticeWindow window = _viewFactory.CreateWindow<NoticeWindow>(viewModel);

		TaskCompletionSource closed = new();

		window.Closed += (_, _) => closed.TrySetResult();

		window.Show();

		return closed.Task;
	}
	#endregion
}
