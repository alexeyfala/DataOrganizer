using Avalonia;
using Avalonia.Controls;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Windows;
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

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	private readonly IJsonSerializerWrapper _jsonSerializer;

	/// <inheritdoc cref="IKeyboardInputHook" />
	private readonly IKeyboardInputHook _keyboardInputHook;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

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
		IKeyboardInputHook keyboardInputHook,
		ILogger logger,
		IViewFactory viewFactory)
	{
		_app = app;

		_appEnvironment = appEnvironment;

		_executionEngine = executionEngine;

		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;

		_keyboardInputHook = keyboardInputHook;

		_logger = logger;

		_viewFactory = viewFactory;
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="AvaloniaObject.PropertyChanged" /> event handler of <see cref="EditorWindow" />.
	/// </summary>
	private static void EditorWindow_PropertyChanged(
		object? sender,
		AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property != Visual.BoundsProperty
			|| sender is not EditorWindow window
			|| e.OldValue is not Rect value)
		{
			return;
		}

		window.PreviousBounds = value;
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

		window.PropertyChanged -= EditorWindow_PropertyChanged;

		_logger.LogInformation($@"Closing ""{nameof(EditorWindow)}"" and saving ""{nameof(EditorWindowSettings)}""");

		_ = SaveEditorSettingsAsync(window);
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

		_ = SaveFavoritesSettingsAsync(window);
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

		EditorWindow window = _viewFactory.CreateWindow<EditorWindow>();

		window.Title = $"{_appEnvironment.GetAppInstanceName()} - {AppUtils.AppVersion}";

		window
			.ViewModel
			.AddHierarchy(hierarchy);

		window
			.ViewModel
			.AddEditingFiles(editingFiles);

		window
			.ViewModel
			.ExecutingFiles
			.AddRange(executingFiles);

		window
			.ViewModel
			.HideAllFileContentsCommand
			.NotifyCanExecuteChanged();

		if (showObjectId.IsNotDefault())
		{
			_ = window
				.ViewModel
				.ShowInEditorAsync(window, showObjectId);
		}
		else if (hierarchy.FindBy(x => x.IsSelected) is { } selected)
		{
			bool isReadOnly = window
				.ViewModel
				.IsReadOnly;

			try
			{
				// To avoid saving the "IsSelected" object property in the database.
				window
					.ViewModel
					.IsReadOnly = true;

				window
					.ViewModel
					.SetSelectedObject(selected);
			}
			finally
			{
				window
					.ViewModel
					.IsReadOnly = isReadOnly;
			}
		}

		string filePath = _appEnvironment.GetSettingsFilePath(nameof(EditorWindowSettings));

		if (_jsonSerializer.FromFile<EditorWindowSettings>(filePath) is { } windowSettings)
		{
			CopyHistoryViewSettings copyHistorySettings = _jsonSerializer.FromFile<CopyHistoryViewSettings>(
				_appEnvironment.GetSettingsFilePath(nameof(CopyHistoryViewSettings))) ?? new();

			window.ViewModel.Initialize(
				window,
				windowSettings,
				copyHistorySettings);
		}
		else
		{
			IViewLauncher.SetDefaultSize(window);

			IViewLauncher.SetDefaultLocation(window);

			IViewLauncher.SetDefaultNavigationColumnWidth(window, window.ViewModel);
		}

		window.Closing += EditorWindow_Closing;

		window.PropertyChanged += EditorWindow_PropertyChanged;

		return window;
	}

	/// <inheritdoc />
	public FavoritesWindow ConfigureFavoritesWindow(
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		IEnumerable<FileModelDto> editingFiles,
		IEnumerable<FileModelDto> executingFiles)
	{
		_logger.LogInformation($@"Opening ""{nameof(FavoritesWindow)}""");

		FavoritesWindow window = _viewFactory.CreateWindow<FavoritesWindow>();

		window
			.ViewModel
			.AddHierarchy(hierarchy);

		window
			.ViewModel
			.OpenedInEditorFiles
			.AddRange(editingFiles);

		window
			.ViewModel
			.ExecutingFiles
			.AddRange(executingFiles);

		string filePath = _appEnvironment.GetSettingsFilePath(nameof(FavoritesWindowSettings));

		if (_jsonSerializer.FromFile<FavoritesWindowSettings>(filePath) is { } windowSettings)
		{
			FavoritesViewSettings favoritesSettings = _jsonSerializer.FromFile<FavoritesViewSettings>(
				_appEnvironment.GetSettingsFilePath(nameof(FavoritesViewSettings))) ?? new();

			CopyHistoryViewSettings copyHistorySettings = _jsonSerializer.FromFile<CopyHistoryViewSettings>(
				_appEnvironment.GetSettingsFilePath(nameof(CopyHistoryViewSettings))) ?? new();

			window.ViewModel.Initialize(
				window,
				windowSettings,
				favoritesSettings,
				copyHistorySettings);
		}
		else
		{
			IViewLauncher.SetDefaultLocation(window);

			IViewLauncher.SetDefaultPopupSize(window.ViewModel);

			IViewLauncher.SetDefaultNavigationColumnWidth(window.ViewModel);
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
	public Task SaveEditorSettingsAsync(EditorWindow window, CancellationToken token = default)
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
				return Task.CompletedTask;
			}

			return TryShutdownAppAsync(window.ViewModel.Hierarchy, token);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return Task.CompletedTask;
		}
	}

	/// <inheritdoc />
	public Task SaveFavoritesSettingsAsync(FavoritesWindow window, CancellationToken token = default)
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
				return Task.CompletedTask;
			}

			return TryShutdownAppAsync(window.ViewModel.Hierarchy, token);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return Task.CompletedTask;
		}
		finally
		{
			window
				.ViewModel
				.Dispose();
		}
	}
	#endregion

	#region Service
	/// <summary>
	/// Tries to delete directory multiple times.
	/// </summary>
	private async Task DeleteDirectoryAsync(
		string directoryPath,
		int maxAttepmts,
		int currentAttepmt = 0,
		CancellationToken token = default)
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
			.Delay(300, token)
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
				currentAttepmt,
				token).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Tries to shutdown the application.
	/// </summary>
	private async Task TryShutdownAppAsync(
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default)
	{
		_keyboardInputHook.Dispose();

		if (!_app.IsDesktop(out var desktop))
		{
			return;
		}

		if (_app.FindWindow<ConsoleWindow>(x => !x.ViewModel.IsSaved) is { } console)
		{
			console.Close();

			while (_app.IsAnyWindow<ConsoleWindow>())
			{
				await Task
					.Delay(300, token)
					.ConfigureAwait(true);
			}
		}

		Guid[] executingFiles = [.. hierarchy
			.GetFilesBy(x => x.IsExecuting)
			.Select(x => x.Id)];

		if (executingFiles.IsNotEmpty())
		{
			await executingFiles
				.ForEachAsync(x => _executionEngine.CloseAsync(x, token))
				.ConfigureAwait(false);
		}

		await DeleteDirectoryAsync(
			directoryPath: _appEnvironment.SandboxDirectoryPath,
			maxAttepmts: 10,
			token: token).ConfigureAwait(false);

		desktop.TryShutdown();
	}
	#endregion
}
