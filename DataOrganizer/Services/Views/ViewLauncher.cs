using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Enums.Dialogs;
using DataOrganizer.Enums.Views;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Runtime;
using DataOrganizer.ViewModels;
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
public class ViewLauncher : IMainWindowLauncher, INoticeLauncher, ISecondaryWindowLauncher
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IClipboardLogPersistenceCoordinator" />
	private readonly IClipboardLogPersistenceCoordinator _clipboardLogPersistence;

	/// <inheritdoc cref="ICopyHistory" />
	private readonly ICopyHistory _copyHistory;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="IPasswordPrompt" />
	private readonly IPasswordPrompt _passwordPrompt;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;

	/// <inheritdoc cref="IWindowSettingsStore" />
	private readonly IWindowSettingsStore _windowSettings;
	#endregion

	#region Constructors
	public ViewLauncher(
		Application app,
		IAppEnvironment appEnvironment,
		IClipboardLogPersistenceCoordinator clipboardLogPersistence,
		ICopyHistory copyHistory,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		IPasswordPrompt passwordPrompt,
		ITaskExceptionHandler exceptionHandler,
		IViewFactory viewFactory,
		IWindowSettingsStore windowSettings)
	{
		_app = app;

		_appEnvironment = appEnvironment;

		_clipboardLogPersistence = clipboardLogPersistence;

		_copyHistory = copyHistory;

		_exceptionHandler = exceptionHandler;

		_logger = logger;

		_messenger = messenger;

		_notification = notification;

		_passwordPrompt = passwordPrompt;

		_viewFactory = viewFactory;

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

		ExitUnlessSwitching(window.ViewModel);
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

		ExitUnlessSwitching(window.ViewModel);
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
		if (_app.FindMainWindow() is not { } window)
		{
			return;
		}

		window.RestoreAndActivate();
	}

	/// <inheritdoc />
	public void CenterMainWindow(Window owner)
	{
		if (_app.FindMainWindow() is not { } window)
		{
			return;
		}

		BringToScreenCenter(window, owner);
	}

	/// <inheritdoc />
	public void CenterNotepadWindow(Window owner)
	{
		if (_app.FindWindow<NotepadWindow>() is not { } window)
		{
			return;
		}

		BringToScreenCenter(window, owner);
	}

	/// <inheritdoc />
	public async Task CloseSecondaryWindowsAsync()
	{
		if (_app.FindWindow<ConsoleWindow>(x => !x.ViewModel.IsSaved) is { } console)
		{
			console.Close();

			while (_app.HasWindow<ConsoleWindow>())
			{
				await Task
					.Delay(300)
					.ConfigureAwait(true);
			}
		}

		// Closed here, so that it saves its settings while the services are still alive.
		if (_app.FindWindow<NotepadWindow>() is { } notepad)
		{
			notepad.Close();
		}
	}

	/// <inheritdoc />
	public EditorWindow CreateEditorWindow(in Guid showObjectId = default)
	{
		_logger.LogInformation($@"Opening ""{nameof(EditorWindow)}""");

		EditorViewModel viewModel = _viewFactory.CreateViewModel<EditorViewModel>();

		EditorWindow window = _viewFactory.CreateWindow<EditorWindow>(viewModel);

		window.Title = $"{_appEnvironment.GetAppInstanceName()} - {AppInfo.AppVersion}";

		viewModel.NotifyDecryptedContentsChanged();

		// The selection of the last session comes back first: selecting it again saves nothing, and an object shown then
		// takes its place as any other choice does.
		if (viewModel.Hierarchy.FindBy(x => x.IsSelected) is { } selected)
		{
			viewModel.SetSelectedObject(selected);
		}

		if (showObjectId.IsNotDefault())
		{
			_exceptionHandler.Watch(viewModel.ShowInEditorAsync(showObjectId, window));
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

	/// <inheritdoc />
	public FavoritesWindow CreateFavoritesWindow()
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
	public Window CreateMainWindow()
	{
		if (_windowSettings.Read<WindowKind>() is { } settings)
		{
			return settings switch
			{
				WindowKind.Editor => CreateEditorWindow(),
				WindowKind.Favorites => CreateFavoritesWindow(),
				_ => throw new NotImplementedException()
			};
		}

		return CreateEditorWindow();
	}

	/// <inheritdoc />
	public async Task ShowClipboardLogWindowAsync(Window owner)
	{
		if (_app.FindWindow<ClipboardLogWindow>() is { } existing)
		{
			PositionAtScreenBottomRight(existing, owner);

			existing.Activate();

			return;
		}

		await UnlockClipboardHistoryIfRequiredAsync().ConfigureAwait(true);

		CreateClipboardLogWindow(owner).Show();
	}

	/// <inheritdoc />
	public void ShowFavorites()
	{
		if (_app.FindDataContext<FavoritesViewModel>() is not { } favorites)
		{
			return;
		}

		favorites.ShowFavorites();
	}

	/// <inheritdoc />
	public void ShowInEditor(Guid id)
	{
		if (_app.FindDataContext<ViewModelBase>(out Window? window) is not { } viewModel || window is null)
		{
			return;
		}

		_exceptionHandler.Watch(viewModel.ShowInEditorAsync(id, window));
	}

	/// <inheritdoc />
	public void ShowNotepadWindow(Window owner)
	{
		if (_app.FindWindow<NotepadWindow>() is { } existing)
		{
			existing.RestoreAndActivate();

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
	#endregion

	#region Helpers
	/// <summary>
	/// Restores and activates <paramref name="target" />, then places it in the center of the screen
	/// of <paramref name="owner" />.
	/// </summary>
	private static void BringToScreenCenter(Window target, Window owner)
	{
		target.RestoreAndActivate();

		// A maximized window fills its own screen and stays there.
		if (target.WindowState == WindowState.Maximized)
		{
			return;
		}

		PositionAtScreenCenter(target, owner);
	}

	/// <summary>
	/// Places <paramref name="target" /> at the bottom-right corner of the screen
	/// that <paramref name="owner" /> currently lives on.
	/// </summary>
	private static void PositionAtScreenBottomRight(Window target, Window owner)
	{
		if ((owner.Screens?.ScreenFromWindow(owner) ?? owner.Screens?.Primary) is not { } screen)
		{
			return;
		}

		// 16 device-independent pixels of padding from the screen edge.
		const int marginDip = 16;

		target.Position = WindowPlacementHelper.GetLowerRightPosition(
			screen.WorkingArea,
			new PixelSize((int)(target.Width * screen.Scaling), (int)(target.Height * screen.Scaling)),
			(int)(marginDip * screen.Scaling));
	}

	/// <summary>
	/// Places <paramref name="target" /> in the center of the working area of the screen
	/// that <paramref name="owner" /> currently lives on.
	/// </summary>
	private static void PositionAtScreenCenter(Window target, Window owner)
	{
		if ((owner.Screens?.ScreenFromWindow(owner) ?? owner.Screens?.Primary) is not { } screen)
		{
			return;
		}

		// A shown window knows its outer size, a new one only the size it asks for.
		PixelSize size = PixelSize.FromSize(
			target.FrameSize ?? new Avalonia.Size(target.Width, target.Height),
			screen.Scaling);

		target.Position = screen
			.WorkingArea
			.CenterRect(new PixelRect(size))
			.Position;
	}

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

			WindowPlacementHelper.Restore(window, settings, x => PositionAtScreenBottomRight(x, owner));
		}
		else
		{
			PositionAtScreenBottomRight(window, owner);
		}

		window.Closing += ClipboardLogWindow_Closing;

		return window;
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
			WindowPlacementHelper.Restore(window, settings, x => PositionAtScreenCenter(x, owner));
		}
		else
		{
			PositionAtScreenCenter(window, owner);
		}

		window.Closing += NotepadWindow_Closing;

		return window;
	}

	/// <summary>
	/// Asks for the exit of the application when a main window closes for any reason but a switch to the other one.
	/// </summary>
	private void ExitUnlessSwitching(ViewModelBase viewModel)
	{
		if (viewModel.IsSwitchingWindow)
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
				NavigationColumnWidth = window.ViewModel.NavigationColumnWidth.Value,
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

	/// <summary>
	/// Prompts for the clipboard-history password while one is required, unlocking and merging the
	/// saved history. Cancelling the prompt leaves the session in-memory only.
	/// </summary>
	private async Task UnlockClipboardHistoryIfRequiredAsync()
	{
		string? label = null;

		while (_clipboardLogPersistence.RequiresUnlock)
		{
			// The first password of the history is created here, every later one is only checked.
			bool hasPassword = _clipboardLogPersistence.HasPassword;

			label ??= hasPassword
				? Strings.Password
				: Strings.NewPassword;

			using PinnedSecret password = await _passwordPrompt.RequestPasswordAsync(
				header: Strings.ClipboardHistory,
				label: label,
				description: hasPassword
					? Strings.SavedHistoryPasswordDescription
					: Strings.NewSavedHistoryPasswordDescription,
				mode: hasPassword
					? PasswordPromptMode.Verify
					: PasswordPromptMode.Create).ConfigureAwait(true);

			if (password.IsEmpty)
			{
				return;
			}

			using PinnedBuffer passwordBytes = password.ToUtf8Buffer();

			ClipboardLogStatus status = await _clipboardLogPersistence
				.TryUnlockAndMergeAsync(passwordBytes)
				.ConfigureAwait(true);

			if (status == ClipboardLogStatus.Unlocked)
			{
				return;
			}

			// Only a wrong password is worth asking again; the rest no password can fix.
			if (status != ClipboardLogStatus.WrongPassword)
			{
				string text = status == ClipboardLogStatus.Damaged
					? Strings.EncryptedDataIsDamaged
					: Strings.FailedToUnlockClipboardHistory;

				_notification.ShowErrorSnackbar(text);

				return;
			}

			label = $"{Strings.IncorrectPassword}. {Strings.TryAgain}";
		}
	}
	#endregion
}
