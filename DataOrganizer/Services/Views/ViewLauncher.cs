using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Views;
using DataOrganizer.Extensions.Views;
using DataOrganizer.Helpers.Views;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Runtime;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Properties;
using System;
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

	#region Event Handlers
	/// <summary>
	/// <see cref="Window.Closing" /> event handler of <see cref="ClipboardLogWindow" />.
	/// </summary>
	private void ClipboardLogWindow_Closing(object? sender, WindowClosingEventArgs e)
	{
		if (sender is not ClipboardLogWindow window)
		{
			return;
		}

		window.Closing -= ClipboardLogWindow_Closing;

		_logger.LogInformation($@"Closing ""{nameof(ClipboardLogWindow)}"" and saving ""{nameof(ClipboardLogWindowSettings)}""");

		SaveClipboardLogSettings(window);

		window
			.ViewModel
			.Dispose();
	}

	/// <summary>
	/// <see cref="Window.Closing" /> event handler of <see cref="EditorWindow" />.
	/// </summary>
	private void EditorWindow_Closing(object? sender, WindowClosingEventArgs e)
	{
		if (sender is not EditorWindow window)
		{
			return;
		}

		window.Closing -= EditorWindow_Closing;

		_logger.LogInformation($@"Closing ""{nameof(EditorWindow)}"" and saving ""{nameof(EditorWindowSettings)}""");

		SaveEditorSettings(window);

		ExitUnlessSwitching(window);
	}

	/// <summary>
	/// <see cref="Window.Closing" /> event handler of <see cref="FavoritesWindow" />.
	/// </summary>
	private void FavoritesWindow_Closing(object? sender, WindowClosingEventArgs e)
	{
		if (sender is not FavoritesWindow window)
		{
			return;
		}

		window.Closing -= FavoritesWindow_Closing;

		_logger.LogInformation($@"Closing ""{nameof(FavoritesWindow)}"" and saving ""{nameof(FavoritesWindowSettings)}""");

		// The popup is still open here when it is fixed or when a command from inside it closes the window, so its
		// content is saved before the settings are written.
		window
			.ViewModel
			.SaveContent();

		SaveFavoritesSettings(window);

		ExitUnlessSwitching(window);
	}

	/// <summary>
	/// <see cref="Window.Closing" /> event handler of <see cref="NotepadWindow" />.
	/// </summary>
	private void NotepadWindow_Closing(object? sender, WindowClosingEventArgs e)
	{
		if (sender is not NotepadWindow window)
		{
			return;
		}

		window.Closing -= NotepadWindow_Closing;

		_logger.LogInformation($@"Closing ""{nameof(NotepadWindow)}"" and saving ""{nameof(NotepadWindowSettings)}""");

		SaveNotepadSettings(window);

		window
			.ViewModel
			.Dispose();
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
			console.Close();

			while (_windowFinder.FindWindow<ConsoleWindow>() is not null)
			{
				await Task
					.Delay(300)
					.ConfigureAwait(true);
			}
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

		if (_windowSettings.Read<EditorWindowSettings>() is { } windowSettings)
		{
			// The stored size, when there is one, replaces the default.
			WindowPlacementHelper.SetDefaultSize(window);

			WindowPlacementHelper.Restore(window, windowSettings, WindowPlacementHelper.SetDefaultLocation);

			if (windowSettings.NavigationColumnWidth <= default(double))
			{
				viewModel.SetDefaultNavigationColumnWidth(window.Width);
			}

			viewModel.Initialize(windowSettings);
		}
		else
		{
			WindowPlacementHelper.SetDefaultSize(window);

			WindowPlacementHelper.SetDefaultLocation(window);

			viewModel.SetDefaultNavigationColumnWidth(window.Width);
		}

		window.Closing += EditorWindow_Closing;

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

		if (_windowSettings.Read<FavoritesWindowSettings>() is { } windowSettings)
		{
			WindowPlacementHelper.Restore(window, windowSettings, WindowPlacementHelper.SetDefaultLocation);

			viewModel.Initialize(windowSettings, _windowSettings.Read<FavoritesViewSettings>() ?? new());
		}
		else
		{
			WindowPlacementHelper.SetDefaultLocation(window);

			viewModel.SetDefaultPopupSize();

			viewModel.SetDefaultNavigationColumnWidth();
		}

		window.Closing += FavoritesWindow_Closing;

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

		if (_windowSettings.Read<ClipboardLogWindowSettings>() is { } settings)
		{
			viewModel.ActiveFilter = settings.ActiveFilter;

			viewModel.KeepOpen = settings.KeepOpen;

			WindowPlacementHelper.Restore(window, settings, x => WindowPlacementHelper.PositionAtScreenBottomRight(x, owner));
		}
		else
		{
			WindowPlacementHelper.PositionAtScreenBottomRight(window, owner);
		}

		window.Closing += ClipboardLogWindow_Closing;

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

		if (_windowSettings.Read<NotepadWindowSettings>() is { } settings)
		{
			WindowPlacementHelper.Restore(window, settings, x => WindowPlacementHelper.PositionAtScreenCenter(x, owner));
		}
		else
		{
			WindowPlacementHelper.PositionAtScreenCenter(window, owner);
		}

		window.Closing += NotepadWindow_Closing;

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
	/// Saves <see cref="ClipboardLogWindow" /> settings to the file.
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
	}

	/// <summary>
	/// Saves <see cref="EditorWindow" /> settings to the file.
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
	/// Saves <see cref="FavoritesWindow" /> settings to the file.
	/// </summary>
	private void SaveFavoritesSettings(FavoritesWindow window)
	{
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
	/// Saves <see cref="NotepadWindow" /> settings to the file.
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
