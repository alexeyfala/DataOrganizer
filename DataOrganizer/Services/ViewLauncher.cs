using Avalonia;
using Avalonia.Controls;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using DataOrganizer.Windows;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Size = System.Drawing.Size;

namespace DataOrganizer.Services;

/// <inheritdoc cref="IViewLauncher" />
public class ViewLauncher : IViewLauncher
{
	#region Data
	/// <inheritdoc cref="App" />
	private readonly App _app;

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
		App app,
		IFileSystem fileSystem,
		IJsonSerializerWrapper jsonSerializer,
		IKeyboardInputHook keyboardInputHook,
		ILogger logger,
		IViewFactory viewFactory)
	{
		_app = app;

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

		if (window
			.ViewModel
			.RightSideSheetContentType == EditorRightSideSheetContentType.CopyHistory)
		{
			window
				.ViewModel
				.SaveCopyHistory();
		}

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

		if (window
			.ViewModel
			.IsPopupFixed)
		{
			window
				.ViewModel
				.SaveCopyHistory();

			window
				.ViewModel
				.SaveFavorites();
		}

		_ = SaveFavoritesSettingsAsync(window);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public EditorWindow ConfigureEditorWindow(
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		in Guid showObjectId = default)
	{
		_logger.LogInformation($@"Opening ""{nameof(EditorWindow)}""");

		EditorWindow window = _viewFactory.CreateWindow<EditorWindow>();

		window.Title = $"{AppUtils.AppName} ({AppUtils.AppVersion})";

		window
			.ViewModel
			.AddHierarchy(hierarchy);

		if (showObjectId.IsNotDefault())
		{
			_ = window
				.ViewModel
				.ShowInEditorAsync(window, showObjectId);
		}
		else if (hierarchy.FindRecursively(x => x.IsSelected) is { } selected)
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

		string filePath = AppUtils.GetSettingsFilePath(nameof(EditorWindowSettings));

		if (_jsonSerializer.FromFile<EditorWindowSettings>(filePath) is { } windowSettings)
		{
			CopyHistoryViewSettings copyHistorySettings = _jsonSerializer.FromFile<CopyHistoryViewSettings>(
				AppUtils.GetSettingsFilePath(nameof(CopyHistoryViewSettings))) ?? new();

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

		AttachDevTools(window);

		return window;
	}

	/// <inheritdoc />
	public EntityCreationView ConfigureEntityCreationView()
	{
		EntityCreationView view = _viewFactory.CreateUserControl<EntityCreationView>();

		string filePath = AppUtils.GetSettingsFilePath(nameof(EntityCreationViewSettings));

		EntityCreationViewSettings settings = _jsonSerializer.FromFile<EntityCreationViewSettings>(filePath);

		view
			.ViewModel
			.Initialize(settings);

		return view;
	}

	/// <inheritdoc />
	public FavoritesWindow ConfigureFavoritesWindow(IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		_logger.LogInformation($@"Opening ""{nameof(FavoritesWindow)}""");

		FavoritesWindow window = _viewFactory.CreateWindow<FavoritesWindow>();

		window
			.ViewModel
			.AddHierarchy(hierarchy);

		string filePath = AppUtils.GetSettingsFilePath(nameof(FavoritesWindowSettings));

		if (_jsonSerializer.FromFile<FavoritesWindowSettings>(filePath) is { } windowSettings)
		{
			FavoritesViewSettings favoritesSettings = _jsonSerializer.FromFile<FavoritesViewSettings>(
				AppUtils.GetSettingsFilePath(nameof(FavoritesViewSettings))) ?? new();

			CopyHistoryViewSettings copyHistorySettings = _jsonSerializer.FromFile<CopyHistoryViewSettings>(
				AppUtils.GetSettingsFilePath(nameof(CopyHistoryViewSettings))) ?? new();

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
	public KeyValueInputView ConfigureKeyValueInputView(
		string defaultButtonText,
		string? key = null,
		string? keyHint = null,
		string? value = null,
		string? valueHint = null)
	{
		KeyValueInputView view = _viewFactory.CreateUserControl<KeyValueInputView>();

		view.ViewModel.Initialize(
			defaultButtonText: defaultButtonText,
			key: key,
			keyHint: keyHint,
			value: value,
			valueHint: valueHint);

		return view;
	}

	/// <inheritdoc />
	public Window ConfigureMainWindow(IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		string filePath = AppUtils.GetSettingsFilePath(nameof(CurrentWindow));

		if (_jsonSerializer.FromFile<CurrentWindow>(filePath) is { } settings)
		{
			return settings switch
			{
				CurrentWindow.Editor => ConfigureEditorWindow(hierarchy),
				CurrentWindow.Favorites => ConfigureFavoritesWindow(hierarchy),
				_ => throw new NotImplementedException()
			};
		}

		return ConfigureEditorWindow(hierarchy);
	}

	/// <inheritdoc />
	public MultilineTextEditView ConfigureMultilineTextEditView(string? text)
	{
		MultilineTextEditView view = _viewFactory.CreateUserControl<MultilineTextEditView>();

		view
			.ViewModel
			.Text = text;

		return view;
	}

	/// <inheritdoc />
	public YesNoQuestionBox ConfigureYesNoQuestionBox(string text)
	{
		YesNoQuestionBox view = _viewFactory.CreateUserControl<YesNoQuestionBox>();

		view
			.ViewModel
			.Text = text;

		return view;
	}

	/// <inheritdoc />
	public Task SaveEditorSettingsAsync(EditorWindow window)
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
				AppUtils.GetSettingsFilePath(nameof(EditorWindowSettings)),
				false);

			_fileSystem.SerializeToJsonFile(
				window.ViewModel.CopyHistorySettings,
				AppUtils.GetSettingsFilePath(nameof(CopyHistoryViewSettings)),
				false);

			_fileSystem.SerializeToJsonFile(
				CurrentWindow.Editor,
				AppUtils.GetSettingsFilePath(nameof(CurrentWindow)),
				false);

			if (!window
				.ViewModel
				.IsShutdown)
			{
				return Task.CompletedTask;
			}

			return TryShutdownAppAsync();
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return Task.CompletedTask;
		}
	}

	/// <inheritdoc />
	public Task SaveFavoritesSettingsAsync(FavoritesWindow window)
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
				AppUtils.GetSettingsFilePath(nameof(FavoritesWindowSettings)),
				false);

			_fileSystem.SerializeToJsonFile(
				window.ViewModel.FavoritesSettings,
				AppUtils.GetSettingsFilePath(nameof(FavoritesViewSettings)),
				false);

			_fileSystem.SerializeToJsonFile(
				window.ViewModel.CopyHistorySettings,
				AppUtils.GetSettingsFilePath(nameof(CopyHistoryViewSettings)),
				false);

			_fileSystem.SerializeToJsonFile(
				CurrentWindow.Favorites,
				AppUtils.GetSettingsFilePath(nameof(CurrentWindow)),
				false);

			if (!window
				.ViewModel
				.IsShutdown)
			{
				return Task.CompletedTask;
			}

			return TryShutdownAppAsync();
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

	/// <summary>
	/// Adds developer tools to the window that can be opened by pressing F12 while in debug mode.
	/// </summary>
	/// <remarks>
	/// Nuget: Avalonia.Diagnostics
	/// </remarks>
	internal static void AttachDevTools(Window window)
	{
#if DEBUG
		window.AttachDevTools();
#endif
	}
	#endregion

	#region Service
	/// <summary>
	/// Tries to shutdown the application.
	/// </summary>
	private async Task TryShutdownAppAsync()
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
					.Delay(300)
					.ConfigureAwait(true);
			}
		}

		desktop.TryShutdown();
	}
	#endregion
}
