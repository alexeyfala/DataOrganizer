using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Size = System.Drawing.Size;

namespace DataOrganizer.Services;

/// <inheritdoc cref="IViewLauncher" />
public class ViewLauncher : IViewLauncher
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IExecutionEngine" />
	private readonly IExecutionEngine _executionEngine;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _handler;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	private readonly IJsonSerializerWrapper _jsonSerializer;

	/// <inheritdoc cref="IKeyboardInputHook" />
	private readonly Lazy<IKeyboardInputHook> _keyboardInputHook;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ServiceProvider" />
	private readonly IServiceProvider _serviceProvider;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;
	#endregion

	#region Constructors
	public ViewLauncher(
		Application app,
		IAppEnvironment appEnvironment,
		IExecutionEngine executionEngine,
		IFileSystem fileSystem,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger,
		IServiceProvider serviceProvider,
		ITaskExceptionHandler handler,
		IViewFactory viewFactory,
		Lazy<IKeyboardInputHook> keyboardInputHook)
	{
		_app = app;

		_appEnvironment = appEnvironment;

		_executionEngine = executionEngine;

		_fileSystem = fileSystem;

		_handler = handler;

		_jsonSerializer = jsonSerializer;

		_keyboardInputHook = keyboardInputHook;

		_logger = logger;

		_serviceProvider = serviceProvider;

		_viewFactory = viewFactory;
	}
	#endregion

	#region Event Handlers
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

		_handler.Watch(SaveEditorSettingsAsync(window));
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

		if (window.ViewModel.IsShutdown && window
			.ViewModel
			.IsPopupFixed)
		{
			window
				.ViewModel
				.SaveContent();
		}

		_handler.Watch(SaveFavoritesSettingsAsync(window));
	}

	/// <summary>
	/// <see cref="Window.Closing" /> event handler of <see cref="SystemClipboardWindow" />.
	/// </summary>
	private void SystemClipboardWindow_Closing(object? sender, WindowClosingEventArgs e)
	{
		if (sender is not SystemClipboardWindow window)
		{
			return;
		}

		window.Closing -= SystemClipboardWindow_Closing;

		_logger.LogInformation($@"Closing ""{nameof(SystemClipboardWindow)}"" and saving ""{nameof(SystemClipboardWindowSettings)}""");

		SaveSystemClipboardSettings(window);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public EditorWindow ConfigureEditorWindow(
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		IEnumerable<FileModelDto> editingFiles,
		IEnumerable<FileModelDto> executingFiles,
		in Guid showObjectId = default)
	{
		_logger.LogInformation($@"Opening ""{nameof(EditorWindow)}""");

		EditorViewModel viewModel = _viewFactory.CreateViewModel<EditorViewModel>();

		EditorWindow window = _viewFactory.CreateWindow<EditorWindow>(viewModel);

		window.Title = $"{_appEnvironment.GetAppInstanceName()} - {AppUtils.AppVersion}";

		viewModel.AddHierarchy(hierarchy);

		viewModel
			.OpenedInEditorFiles
			.AddRange(editingFiles);

		viewModel
			.ExecutingFiles
			.AddRange(executingFiles);

		viewModel
			.HideAllFileContentsCommand
			.NotifyCanExecuteChanged();

		if (showObjectId.IsNotDefault())
		{
			_handler.Watch(viewModel.ShowInEditorAsync(showObjectId, window));
		}
		else if (hierarchy.FindBy(x => x.IsSelected) is { } selected)
		{
			bool isReadOnly = viewModel.IsReadOnly;

			try
			{
				// To avoid saving the "IsSelected" object property in the database.
				viewModel.IsReadOnly = true;

				viewModel.SetSelectedObject(selected);
			}
			finally
			{
				viewModel.IsReadOnly = isReadOnly;
			}
		}

		string filePath = _appEnvironment.GetSettingsFilePath(nameof(EditorWindowSettings));

		if (_jsonSerializer.FromFile<EditorWindowSettings>(filePath) is { } windowSettings)
		{
			viewModel.Initialize(
				window,
				windowSettings,
				GetHistorySettingsFromFile());
		}
		else
		{
			IViewLauncher.SetDefaultSize(window);

			IViewLauncher.SetDefaultLocation(window);

			IViewLauncher.SetDefaultNavigationColumnWidth(window, viewModel);
		}

		window.Closing += EditorWindow_Closing;

		return window;
	}

	/// <inheritdoc />
	public FavoritesWindow ConfigureFavoritesWindow(
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		IEnumerable<FileModelDto> editingFiles,
		IEnumerable<FileModelDto> executingFiles)
	{
		_logger.LogInformation($@"Opening ""{nameof(FavoritesWindow)}""");

		FavoritesViewModel viewModel = _viewFactory.CreateViewModel<FavoritesViewModel>();

		FavoritesWindow window = _viewFactory.CreateWindow<FavoritesWindow>(viewModel);

		viewModel.AddHierarchy(hierarchy);

		viewModel
			.OpenedInEditorFiles
			.AddRange(editingFiles);

		viewModel
			.ExecutingFiles
			.AddRange(executingFiles);

		string filePath = _appEnvironment.GetSettingsFilePath(nameof(FavoritesWindowSettings));

		if (_jsonSerializer.FromFile<FavoritesWindowSettings>(filePath) is { } windowSettings)
		{
			viewModel.Initialize(
				window,
				windowSettings,
				GetFavoritesSettingsFromFile(),
				GetHistorySettingsFromFile());
		}
		else
		{
			IViewLauncher.SetDefaultLocation(window);

			IViewLauncher.SetDefaultPopupSize(viewModel);

			IViewLauncher.SetDefaultNavigationColumnWidth(viewModel);
		}

		window.Closing += FavoritesWindow_Closing;

		return window;
	}

	/// <inheritdoc />
	public Window ConfigureMainWindow(IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		string filePath = _appEnvironment.GetSettingsFilePath(nameof(CurrentWindow));

		if (_jsonSerializer.FromFile<CurrentWindow>(filePath) is { } settings)
		{
			return settings switch
			{
				CurrentWindow.Editor => ConfigureEditorWindow(hierarchy, [], []),
				CurrentWindow.Favorites => ConfigureFavoritesWindow(hierarchy, [], []),
				_ => throw new NotImplementedException()
			};
		}

		return ConfigureEditorWindow(hierarchy, [], []);
	}

	/// <inheritdoc />
	public SystemClipboardWindow ConfigureSystemClipboardWindow(Window owner)
	{
		_logger.LogInformation($@"Opening ""{nameof(SystemClipboardWindow)}""");

		SystemClipboardViewModel viewModel = _viewFactory.CreateViewModel<SystemClipboardViewModel>();

		SystemClipboardWindow window = _viewFactory.CreateWindow<SystemClipboardWindow>(viewModel);

		string filePath = _appEnvironment.GetSettingsFilePath(nameof(SystemClipboardWindowSettings));

		SystemClipboardWindowSettings? settings = _jsonSerializer.FromFile<SystemClipboardWindowSettings>(filePath);

		// window.Screens is only reliable after the window is attached to a toplevel,
		// so both branches run in Opened.
		window.Opened += (_, _) =>
		{
			if (settings is not null)
			{
				PixelPoint candidate = new(settings.X, settings.Y);

				if (IViewLauncher.IsWindowPositionOnScreen(window, candidate))
				{
					window.Position = candidate;

					return;
				}
			}

			PositionAtScreenBottomRight(window, owner);
		};

		window.Closing += SystemClipboardWindow_Closing;

		return window;
	}

	/// <inheritdoc />
	public async Task SaveEditorSettingsAsync(EditorWindow window)
	{
		try
		{
			Size size;

			if (window.WindowState == WindowState.Maximized)
			{
				size = new((int)window.PreviousBounds.Width, (int)window.PreviousBounds.Height);
			}
			else
			{
				size = new((int)window.Width, (int)window.Height);
			}

			EditorWindowSettings settings = new()
			{
				IsReadOnly = window.ViewModel.IsReadOnly,
				NavigationColumnWidth = window.ViewModel.NavigationColumnWidth.Value,
				Size = size,
				WindowState = window.WindowState,
				X = window.Position.X,
				Y = window.Position.Y
			};

			_fileSystem.SerializeToJsonFile(
				settings,
				_appEnvironment.GetSettingsFilePath(nameof(EditorWindowSettings)),
				false);

			_fileSystem.SerializeToJsonFile(
				window.ViewModel.CopyHistorySettings,
				_appEnvironment.GetSettingsFilePath(nameof(CopyHistoryViewSettings)),
				false);

			_fileSystem.SerializeToJsonFile(
				CurrentWindow.Editor,
				_appEnvironment.GetSettingsFilePath(nameof(CurrentWindow)),
				false);

			if (!window
				.ViewModel
				.IsShutdown)
			{
				return;
			}

			await ShutdownAppAsync(window.ViewModel.Hierarchy).ConfigureAwait(false);
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

	/// <inheritdoc />
	public async Task SaveFavoritesSettingsAsync(FavoritesWindow window)
	{
		try
		{
			FavoritesWindowSettings settings = new()
			{
				PopupHeight = window.ViewModel.PopupHeight,
				PopupWidth = window.ViewModel.PopupWidth,
				X = window.Position.X,
				Y = window.Position.Y
			};

			_fileSystem.SerializeToJsonFile(
				settings,
				_appEnvironment.GetSettingsFilePath(nameof(FavoritesWindowSettings)),
				false);

			_fileSystem.SerializeToJsonFile(
				window.ViewModel.FavoritesSettings,
				_appEnvironment.GetSettingsFilePath(nameof(FavoritesViewSettings)),
				false);

			_fileSystem.SerializeToJsonFile(
				window.ViewModel.CopyHistorySettings,
				_appEnvironment.GetSettingsFilePath(nameof(CopyHistoryViewSettings)),
				false);

			_fileSystem.SerializeToJsonFile(
				CurrentWindow.Favorites,
				_appEnvironment.GetSettingsFilePath(nameof(CurrentWindow)),
				false);

			if (!window
				.ViewModel
				.IsShutdown)
			{
				return;
			}

			await ShutdownAppAsync(window.ViewModel.Hierarchy).ConfigureAwait(false);
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

	/// <inheritdoc />
	public void SaveSystemClipboardSettings(SystemClipboardWindow window)
	{
		try
		{
			SystemClipboardWindowSettings settings = new()
			{
				X = window.Position.X,
				Y = window.Position.Y,
			};

			_fileSystem.SerializeToJsonFile(
				settings,
				_appEnvironment.GetSettingsFilePath(nameof(SystemClipboardWindowSettings)),
				false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Places <paramref name="popup" /> at the bottom-right corner of the screen
	/// that <paramref name="owner" /> currently lives on.
	/// </summary>
	private static void PositionAtScreenBottomRight(Window popup, Window owner)
	{
		if ((owner.Screens?.ScreenFromWindow(owner) ?? owner.Screens?.Primary) is not { } screen)
		{
			return;
		}

		// 16 device-independent pixels of padding from the screen edge.
		const int marginDip = 16;

		PixelRect workingArea = screen.WorkingArea;

		int widthPx = (int)(popup.Width * screen.Scaling);

		int heightPx = (int)(popup.Height * screen.Scaling);

		int marginPx = (int)(marginDip * screen.Scaling);

		popup.Position = new PixelPoint(
			workingArea.X + workingArea.Width - widthPx - marginPx,
			workingArea.Y + workingArea.Height - heightPx - marginPx);
	}

	/// <summary>
	/// Tries to delete directory multiple times.
	/// </summary>
	private async Task DeleteDirectoryAsync(
		string directoryPath,
		int maxAttepmts,
		int currentAttepmt = 0)
	{
		if (!_fileSystem.IsDirectoryExists(directoryPath))
		{
			_logger.LogInformation($@"Folder ""{directoryPath}"" does not exist");

			return;
		}

		if (currentAttepmt >= maxAttepmts)
		{
			_logger.LogInformation($@"Can't delete folder ""{directoryPath}"" with {currentAttepmt} attepmts");

			return;
		}

		currentAttepmt++;

		await Task
			.Delay(300)
			.ConfigureAwait(false);

		_logger.LogInformation(
			$@"Trying to delete folder ""{directoryPath}"". Attepmt №{currentAttepmt}");

		try
		{
			_fileSystem.DeleteDirectoryRecursively(directoryPath, true);

			_logger.LogInformation($@"Folder ""{directoryPath}"" is deleted");
		}
		catch (IOException)
		{
			await DeleteDirectoryAsync(
				directoryPath,
				maxAttepmts,
				currentAttepmt).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Returns <see cref="FavoritesViewSettings" /> settings from file.
	/// </summary>
	private FavoritesViewSettings GetFavoritesSettingsFromFile()
	{
		return _jsonSerializer.FromFile<FavoritesViewSettings>(
			_appEnvironment.GetSettingsFilePath(nameof(FavoritesViewSettings))) ?? new();
	}

	/// <summary>
	/// Returns <see cref="CopyHistoryViewSettings" /> settings from file.
	/// </summary>
	private CopyHistoryViewSettings GetHistorySettingsFromFile()
	{
		return _jsonSerializer.FromFile<CopyHistoryViewSettings>(
				_appEnvironment.GetSettingsFilePath(nameof(CopyHistoryViewSettings))) ?? new();
	}

	/// <summary>
	/// Shutdowns the application.
	/// </summary>
	private async Task ShutdownAppAsync(IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		if (_keyboardInputHook.IsValueCreated)
		{
			_keyboardInputHook.Value.Dispose();
		}

		if (_app.IsDesktop(out IClassicDesktopStyleApplicationLifetime? desktop))
		{
			await ShutdownAsync(hierarchy);

			desktop.Shutdown();
		}
		else
		{
			await ShutdownAsync(hierarchy);

			if (!AppDomain
				.CurrentDomain
				.IsRunningFromNUnit())
			{
				Environment.Exit(0);
			}
		}

		async Task ShutdownAsync(IEnumerable<ExplorerModelBaseDto> hierarchy)
		{
			if (_app.FindWindow<ConsoleWindow>(x => !x.ViewModel.IsSaved) is { } console)
			{
				console.Close();

				while (_app.IsAnyWindow<ConsoleWindow>())
				{
					await Task
						.Delay(300)
						.ConfigureAwait(true);
				}
			}

			Guid[] executingFiles = [.. hierarchy
				.GetFilesBy(x => x.IsExecuting)
				.Select(x => x.Id)];

			if (executingFiles.IsNotEmpty())
			{
				await executingFiles
					.ForEachAsync(x => _executionEngine.CloseAsync(x))
					.ConfigureAwait(false);
			}

			await DeleteDirectoryAsync(
				directoryPath: _appEnvironment.SandboxDirectoryPath,
				maxAttepmts: 10).ConfigureAwait(false);

			if (_app is App app)
			{
				app
					.AppLifetimeTimer
					.Stop();

				_logger.LogInformationWithTemplate(
					$"App life time is: {app.AppLifetimeTimer.GetElapsedTime()}{Environment.NewLine}{Environment.NewLine}");
			}

			if (_serviceProvider is not IAsyncDisposable asyncDisposable)
			{
				return;
			}

			await asyncDisposable
				.DisposeAsync()
				.ConfigureAwait(false);
		}
	}
	#endregion
}
