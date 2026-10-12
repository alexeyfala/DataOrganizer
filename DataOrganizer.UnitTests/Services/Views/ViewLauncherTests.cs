using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.NUnit;
using Avalonia.Platform;
using Avalonia.Threading;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Enums.Dialogs;
using DataOrganizer.Enums.Views;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Notepad;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Text;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Editor;
using DataOrganizer.Services.Settings;
using DataOrganizer.Services.Views;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using NSubstitute;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Services.Views;

[TestFixture(Description = $@"Tests of ""{nameof(ViewLauncher)}"" type")]
internal class ViewLauncherTests
{
	#region Methods
	/// <summary>
	/// <see cref="ViewLauncher.ActivateMainWindow" />: the minimized main window comes back from the taskbar on top of the
	/// notepad.
	/// </summary>
	[AvaloniaTest]
	public void ActivateMainWindow_Restores_The_Main_Window()
	{
		// Arrange
		using AutoMock windowMock = AutoMock.GetLoose();

		Window mainWindow = new()
		{
			DataContext = windowMock.Create<FavoritesViewModel>(),
			WindowState = WindowState.Minimized
		};

		NotepadWindow notepad = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<IViewCache>(),
			Substitute.For<IViewLauncher>(),
			TimeProvider.System));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([notepad, mainWindow]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		mainWindow.Show();

		notepad.Show();

		// Act
		sut.ActivateMainWindow();

		WindowState state = mainWindow.WindowState;

		Window[] windows = [mainWindow, notepad];

		Window.SortWindowsByZOrder(windows);

		// Closed before the assertions: a closed notepad takes its dialog host out of the list every headless test shares.
		notepad.Close();

		// Assert
		state
			.Should()
			.Be(WindowState.Normal);

		windows[^1]
			.Should()
			.BeSameAs(mainWindow);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CenterMainWindow" />: the main window moves to the center of the working area of the
	/// screen of the owner.
	/// </summary>
	[AvaloniaTest]
	public void CenterMainWindow_Centers_The_Main_Window_On_The_Screen_Of_The_Owner()
	{
		// Arrange
		using AutoMock windowMock = AutoMock.GetLoose();

		Window mainWindow = new()
		{
			DataContext = windowMock.Create<FavoritesViewModel>(),
			Height = 300.0,
			Width = 400.0
		};

		NotepadWindow notepad = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<IViewCache>(),
			Substitute.For<IViewLauncher>(),
			TimeProvider.System));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([notepad, mainWindow]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		mainWindow.Show();

		// Act
		sut.CenterMainWindow(notepad);

		// Assert
		PixelRect bounds = new(mainWindow.Position, new PixelSize(400, 300));

		bounds.Center
			.Should()
			.Be(notepad.Screens!.Primary!.WorkingArea.Center);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CenterMainWindow" />: a maximized main window stays on its own screen.
	/// </summary>
	[AvaloniaTest]
	public void CenterMainWindow_Leaves_A_Maximized_Main_Window_In_Place()
	{
		// Arrange
		PixelPoint position = new(5, 5);

		using AutoMock windowMock = AutoMock.GetLoose();

		Window mainWindow = new()
		{
			DataContext = windowMock.Create<FavoritesViewModel>(),
			Position = position,
			WindowState = WindowState.Maximized
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([mainWindow]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		mainWindow.Show();

		// Act
		sut.CenterMainWindow(new Window());

		// Assert
		mainWindow.Position
			.Should()
			.Be(position);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CenterNotepadWindow" />: the open notepad moves to the center of the working area of the
	/// screen of the owner.
	/// </summary>
	[AvaloniaTest]
	public void CenterNotepadWindow_Centers_The_Notepad_On_The_Screen_Of_The_Owner()
	{
		// Arrange
		NotepadWindow notepad = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<IViewCache>(),
			Substitute.For<IViewLauncher>(),
			TimeProvider.System))
		{
			Height = 300.0,
			Width = 400.0
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([notepad]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		Window owner = new();

		notepad.Show();

		// Act
		sut.CenterNotepadWindow(owner);

		PixelRect bounds = new(notepad.Position, new PixelSize(400, 300));

		// Closed before the assertion: a closed notepad takes its dialog host out of the list every headless test shares.
		notepad.Close();

		// Assert
		bounds.Center
			.Should()
			.Be(owner.Screens!.Primary!.WorkingArea.Center);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CenterNotepadWindow" />: a maximized notepad stays on its own screen.
	/// </summary>
	[AvaloniaTest]
	public void CenterNotepadWindow_Leaves_A_Maximized_Notepad_In_Place()
	{
		// Arrange
		PixelPoint position = new(5, 5);

		NotepadWindow notepad = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<IViewCache>(),
			Substitute.For<IViewLauncher>(),
			TimeProvider.System))
		{
			Position = position,
			WindowState = WindowState.Maximized
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([notepad]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		notepad.Show();

		// Act
		sut.CenterNotepadWindow(new Window());

		PixelPoint actual = notepad.Position;

		// Closed before the assertion: a closed notepad takes its dialog host out of the list every headless test shares.
		notepad.Close();

		// Assert
		actual
			.Should()
			.Be(position);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateClipboardLogWindow" />: saved size and position settings are applied to the window.
	/// </summary>
	[AvaloniaTest]
	public void CreateClipboardLogWindow_Applies_Saved_Settings()
	{
		// Arrange
		int positiveValue = RandomValues.CreateInt(100, 300);

		ClipboardLogWindowSettings settings = new()
		{
			ActiveFilter = ClipboardLogEntryFilter.Image,
			KeepOpen = true,
			Size = new(positiveValue, positiveValue),
			X = 10,
			Y = 10
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder =>
			{
				IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

				clipboardLogService
					.Entries
					.Returns([]);

				windowBuilder.RegisterInstance(clipboardLogService);
			});

			ClipboardLogViewModel viewModel = windowMock.Create<ClipboardLogViewModel>();

			ClipboardLogWindow clipboardWindow = windowMock.Create<ClipboardLogWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<ClipboardLogWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<ClipboardLogViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<ClipboardLogWindow>(Arg.Any<object[]>())
				.Returns(clipboardWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		ClipboardLogWindow window = sut.CreateClipboardLogWindow(new Window());

		// Assert
		window.Width
			.Should()
			.Be(positiveValue);

		window.Height
			.Should()
			.Be(positiveValue);

		window.Position
			.Should()
			.Be(new PixelPoint(settings.X, settings.Y));

		window.ViewModel
			.KeepOpen
			.Should()
			.BeTrue();

		window.ViewModel
			.ActiveFilter
			.Should()
			.Be(ClipboardLogEntryFilter.Image);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateClipboardLogWindow" />: a window saved without a size keeps the size its markup gives
	/// it.
	/// </summary>
	[AvaloniaTest]
	public void CreateClipboardLogWindow_Keeps_Its_Own_Size_Without_A_Saved_One()
	{
		// Arrange
		ClipboardLogWindow markup = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ClipboardLogWindowSettings settings = new()
			{
				Size = new(0, 0),
				X = 10,
				Y = 10
			};

			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder =>
			{
				IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

				clipboardLogService
					.Entries
					.Returns([]);

				windowBuilder.RegisterInstance(clipboardLogService);
			});

			ClipboardLogViewModel viewModel = windowMock.Create<ClipboardLogViewModel>();

			ClipboardLogWindow clipboardWindow = windowMock.Create<ClipboardLogWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<ClipboardLogWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<ClipboardLogViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<ClipboardLogWindow>(Arg.Any<object[]>())
				.Returns(clipboardWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		ClipboardLogWindow window = sut.CreateClipboardLogWindow(new Window());

		// Assert
		window.Width
			.Should()
			.Be(markup.Width);

		window.Height
			.Should()
			.Be(markup.Height);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateClipboardLogWindow" />: without saved settings the window opens in the lower right
	/// corner of the screen of the owner.
	/// </summary>
	[AvaloniaTest]
	public void CreateClipboardLogWindow_Opens_In_The_Corner_For_The_First_Launch()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder =>
			{
				IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

				clipboardLogService
					.Entries
					.Returns([]);

				windowBuilder.RegisterInstance(clipboardLogService);
			});

			ClipboardLogViewModel viewModel = windowMock.Create<ClipboardLogViewModel>();

			ClipboardLogWindow clipboardWindow = windowMock.Create<ClipboardLogWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<ClipboardLogViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<ClipboardLogWindow>(Arg.Any<object[]>())
				.Returns(clipboardWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		Window owner = new();

		// Act
		ClipboardLogWindow window = sut.CreateClipboardLogWindow(owner);

		// Assert
		Screen screen = owner.Screens!.Primary!;

		PixelRect bounds = new(window.Position, PixelSize.FromSize(new Size(window.Width, window.Height), screen.Scaling));

		screen.WorkingArea.Contains(bounds)
			.Should()
			.BeTrue();

		bounds.Center.X
			.Should()
			.BeGreaterThan(screen.WorkingArea.Center.X);

		bounds.Center.Y
			.Should()
			.BeGreaterThan(screen.WorkingArea.Center.Y);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateClipboardLogWindow" />: a window saved off every screen opens in the lower right corner
	/// of the screen of the owner.
	/// </summary>
	[AvaloniaTest]
	public void CreateClipboardLogWindow_Places_A_Window_Saved_Off_The_Screens_In_The_Corner()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ClipboardLogWindowSettings settings = new()
			{
				Size = new(300, 300),
				X = 50000,
				Y = 50000
			};

			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder =>
			{
				IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

				clipboardLogService
					.Entries
					.Returns([]);

				windowBuilder.RegisterInstance(clipboardLogService);
			});

			ClipboardLogViewModel viewModel = windowMock.Create<ClipboardLogViewModel>();

			ClipboardLogWindow clipboardWindow = windowMock.Create<ClipboardLogWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<ClipboardLogWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<ClipboardLogViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<ClipboardLogWindow>(Arg.Any<object[]>())
				.Returns(clipboardWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		Window owner = new();

		// Act
		ClipboardLogWindow window = sut.CreateClipboardLogWindow(owner);

		// Assert
		Screen screen = owner.Screens!.Primary!;

		PixelRect bounds = new(window.Position, PixelSize.FromSize(new Size(window.Width, window.Height), screen.Scaling));

		screen.WorkingArea.Contains(bounds)
			.Should()
			.BeTrue();

		bounds.Center.X
			.Should()
			.BeGreaterThan(screen.WorkingArea.Center.X);

		bounds.Center.Y
			.Should()
			.BeGreaterThan(screen.WorkingArea.Center.Y);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: default size, centered location and navigation column width are used on first launch.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Creates_Window_With_Default_Settings_For_The_First_Launch()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		EditorWindow window = sut.CreateEditorWindow([], null, []);

		// Assert
		window.Width
			.Should()
			.Be(IViewLauncher.DefaultWindowSize.Width);

		window.Height
			.Should()
			.Be(IViewLauncher.DefaultWindowSize.Height);

		window.WindowStartupLocation
			.Should()
			.Be(WindowStartupLocation.CenterScreen);

		window.ViewModel.NavigationColumnWidth.Value
			.Should()
			.Be(window.Width / 3.0);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: the editor view model is initialized from saved settings.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Initializes_The_ViewModel_From_Saved_Settings()
	{
		// Arrange
		int positiveValue = RandomValues.CreateInt(100, 300);

		EditorWindowSettings settings = new()
		{
			IsReadOnly = true,
			IsTopmost = true,
			NavigationColumnWidth = positiveValue - 20,
			Size = new(positiveValue, positiveValue),
			WindowState = WindowState.Normal,
			X = positiveValue,
			Y = positiveValue
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<EditorWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		EditorWindow window = sut.CreateEditorWindow([], null, []);

		// Assert
		window.ViewModel.IsInitialized
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: selecting again the object selected before leaves the read-only mode
	/// of the open editors alone.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Leaves_The_Read_Only_Mode_Alone_While_Selecting()
	{
		// Arrange
		List<EditorReadOnlyModeChangedMessage> received = [];

		FileDto selected = ItemDtoFactory.CreateFileDto();

		selected.IsSelected = true;

		// The editor lives as long as the test, as it follows its tree while it does.
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IMessenger messenger = new StrongReferenceMessenger();

			messenger.Register<EditorReadOnlyModeChangedMessage>(
				received,
				static (recipient, message) => ((List<EditorReadOnlyModeChangedMessage>)recipient).Add(message));

			builder.RegisterInstance(messenger);
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		sut.CreateEditorWindow([selected], null, []);

		// Assert
		received
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: the editor gets the tabs to open.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Passes_The_Editor_Tabs()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		EditorTabsState tabs = new()
		{
			Files = [file],
			PreviousFile = null,
			SelectedFile = file
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		EditorWindow window = sut.CreateEditorWindow([], tabs, []);

		// Assert
		window.ViewModel.EditorTabs
			.Should()
			.BeSameAs(tabs);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: a switch to the favorites saves the item chosen in the copy history of
	/// the right side sheet.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Saves_The_Chosen_Copy_History_Item_When_Switching_To_The_Favorites()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		FavoritesWindow favoritesWindow = new();

		Guid savedItemId = default;

		// The editor lives as long as the test, as the window saves it when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

			viewLauncher.CreateFavoritesWindow(
				Arg.Any<IEnumerable<ExplorerItemDtoBase>>(),
				Arg.Any<EditorTabsState?>(),
				Arg.Any<IEnumerable<FileDto>>())
			.Returns(favoritesWindow);

			builder.RegisterInstance(viewLauncher);
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<CopyHistoryViewSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => savedItemId = call.Arg<CopyHistoryViewSettings>().SelectedItemId);

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = sut.CreateEditorWindow(files, null, []);

		CopyHistoryViewModel copyHistory = windowMock.Create<CopyHistoryViewModel>();

		window
			.ViewModel
			.CopyHistorySettings
			.AddItemIds(files.Select(x => x.Id), files);

		window
			.ViewModel
			.ShowCopyHistoryCommand
			.Execute(null);

		// The view of the side sheet hands its view model over once loaded, and the stored selection comes back through
		// the dispatcher before the choice.
		window
			.ViewModel
			.CopyHistoryDisplayedCommand
			.Execute(copyHistory);

		Dispatcher.UIThread.RunJobs();

		copyHistory.SelectedItem = files[1];

		// Act
		window
			.ViewModel
			.ShowFavorites(window);

		// Closed before the assertions: its dialog host would stay in the list every headless test shares.
		favoritesWindow.Close();

		// Assert
		savedItemId
			.Should()
			.Be(files[1].Id);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: closing the window saves the item chosen in the copy history of the
	/// right side sheet.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Saves_The_Chosen_Copy_History_Item_When_The_Window_Closes()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		Guid savedItemId = default;

		// The editor lives as long as the test, as the window saves it when it closes.
		using AutoMock windowMock = AutoMock.GetLoose();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<CopyHistoryViewSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => savedItemId = call.Arg<CopyHistoryViewSettings>().SelectedItemId);

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = sut.CreateEditorWindow(files, null, []);

		CopyHistoryViewModel copyHistory = windowMock.Create<CopyHistoryViewModel>();

		window
			.ViewModel
			.CopyHistorySettings
			.AddItemIds(files.Select(x => x.Id), files);

		window
			.ViewModel
			.ShowCopyHistoryCommand
			.Execute(null);

		// The view of the side sheet hands its view model over once loaded, and the stored selection comes back through
		// the dispatcher before the choice.
		window
			.ViewModel
			.CopyHistoryDisplayedCommand
			.Execute(copyHistory);

		Dispatcher.UIThread.RunJobs();

		copyHistory.SelectedItem = files[1];

		// Act
		window.Close();

		// Assert
		savedItemId
			.Should()
			.Be(files[1].Id);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: an object shown when the editor opens takes the place of the object
	/// selected before, and both are saved from the loaded window.
	/// </summary>
	[AvaloniaTest]
	public async Task CreateEditorWindow_Saves_The_Object_Shown_In_Place_Of_The_Object_Selected_Before(
		[Values] bool isSelectedHidden)
	{
		// Arrange
		List<(Guid Id, bool IsSelected, bool IsLoaded)> saved = [];

		EditorWindow? window = null;

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto selected = ItemDtoFactory.CreateFileDto();

		FileDto shown = ItemDtoFactory.CreateFileDto();

		selected.IsSelected = true;

		List<ExplorerItemDtoBase> hierarchy = [folder, shown];

		// A row in a collapsed folder is never shown, so the tree cannot clear it.
		if (isSelectedHidden)
		{
			selected.Parent = folder;

			folder.Children.Add(selected);
		}
		else
		{
			hierarchy.Insert(0, selected);
		}

		// The editor lives as long as the test, as it follows its tree while it does.
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

			propertyWriter.UpdateIsSelectedAsync(
				Arg.Do<ExplorerItemDtoBase>(x => saved.Add((x.Id, x.IsSelected, window?.IsLoaded ?? false))),
				Arg.Any<CancellationToken>());

			builder.RegisterInstance(propertyWriter);
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		window = sut.CreateEditorWindow(hierarchy, null, [], shown.Id);

		window.Show();

		await WaitUntilAsync(() => window.ViewModel.SelectedObject == shown);

		window.Close();

		// Assert
		selected.IsSelected
			.Should()
			.BeFalse();

		shown.IsSelected
			.Should()
			.BeTrue();

		// The tree may clear and mark the object shown once more while it moves its selection, so the last write of
		// each object counts.
		saved.Where(x => x.Id == selected.Id).Select(x => x.IsSelected)
			.Should()
			.EndWith(false);

		saved.Where(x => x.Id == shown.Id).Select(x => x.IsSelected)
			.Should()
			.EndWith(true);

		saved
			.Should()
			.OnlyContain(x => x.IsLoaded);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: the object selected before is selected again also when another object
	/// is to be shown, so the object shown takes its place as any other choice does.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Selects_The_Object_Selected_Before_Also_When_Another_Is_To_Be_Shown()
	{
		// Arrange
		FileDto selected = ItemDtoFactory.CreateFileDto();

		selected.IsSelected = true;

		// The editor lives as long as the test, as it follows its tree while it does.
		using AutoMock windowMock = AutoMock.GetLoose();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		// An object the tree does not hold, so that the show ends at once.
		EditorWindow window = sut.CreateEditorWindow([selected], null, [], Guid.NewGuid());

		// Assert
		window.ViewModel.SelectedObject
			.Should()
			.BeSameAs(selected);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: the object selected when the editor was open last is selected again
	/// without being saved once more.
	/// </summary>
	[AvaloniaTest]
	public async Task CreateEditorWindow_Selects_The_Object_Selected_Before_Without_Saving_It()
	{
		// Arrange
		FileDto selected = ItemDtoFactory.CreateFileDto();

		selected.IsSelected = true;

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		// The editor lives as long as the test, as it follows its tree while it does.
		using AutoMock windowMock = AutoMock.GetLoose(builder => builder.RegisterInstance(propertyWriter));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		EditorWindow window = sut.CreateEditorWindow([selected], null, []);

		// Assert
		window.ViewModel.SelectedObject
			.Should()
			.BeSameAs(selected);

		await propertyWriter.DidNotReceive().UpdateIsSelectedAsync(
			Arg.Any<ExplorerItemDtoBase>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: default popup size, navigation column width and empty selected category are used on first launch.
	/// </summary>
	[AvaloniaTest]
	public void CreateFavoritesWindow_Creates_Window_With_Default_Settings_For_The_First_Launch()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		FavoritesWindow window = sut.CreateFavoritesWindow([], null, []);

		// Assert
		window.WindowStartupLocation
			.Should()
			.Be(WindowStartupLocation.CenterScreen);

		window.ViewModel.PopupHeight
			.Should()
			.Be(250.0);

		window.ViewModel.PopupWidth
			.Should()
			.Be(window.ViewModel.PopupHeight * 2.0);

		window.ViewModel.FavoritesSettings.NavigationColumnWidth
			.Should()
			.Be(window.ViewModel.PopupWidth / 2.0);

		window.ViewModel.FavoritesSettings.SelectedCategoryId
			.Should()
			.Be(Guid.Empty);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: the favorites view model is initialized from saved settings.
	/// </summary>
	[AvaloniaTest]
	public void CreateFavoritesWindow_Initializes_The_ViewModel_From_Saved_Settings()
	{
		// Arrange
		int positiveValue = RandomValues.CreateInt(100, 300);

		FavoritesWindowSettings settings = new()
		{
			PopupHeight = positiveValue,
			PopupWidth = positiveValue,
			X = positiveValue,
			Y = positiveValue
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<FavoritesWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		FavoritesWindow window = sut.CreateFavoritesWindow([], null, []);

		// Assert
		window.ViewModel.IsInitialized
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: the favorites keep the tabs of the editor.
	/// </summary>
	[AvaloniaTest]
	public void CreateFavoritesWindow_Passes_The_Editor_Tabs()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		EditorTabsState tabs = new()
		{
			Files = [file],
			PreviousFile = null,
			SelectedFile = file
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		FavoritesWindow window = sut.CreateFavoritesWindow([], tabs, []);

		// Assert
		window.ViewModel.EditorTabs
			.Should()
			.BeSameAs(tabs);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: a switch to the editor saves the category chosen in the popup,
	/// fixed or not, and keeps the favorite selected in each category.
	/// </summary>
	[AvaloniaTest]
	public async Task CreateFavoritesWindow_Saves_The_Chosen_Category_When_Switching_To_The_Editor(
		[Values] bool isPopupFixed)
	{
		// Arrange
		FavoriteCategory[] categories = [.. FavoriteFactory.CreateFavoriteCategories(2)];

		FavoriteSelection[] pairs = [.. FavoriteFactory.CreateFavoriteSelections(2)];

		EditorWindow editorWindow = new();

		Guid savedCategoryId = default;

		FavoriteSelection[] savedPairs = [];

		// The favorites live as long as the test, as the window saves them when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

			viewLauncher.CreateEditorWindow(
				Arg.Any<IEnumerable<ExplorerItemDtoBase>>(),
				Arg.Any<EditorTabsState?>(),
				Arg.Any<IEnumerable<FileDto>>())
			.Returns(editorWindow);

			builder.RegisterInstance(viewLauncher);
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			// The settings are cleared once written, so they are read at the write.
			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<FavoritesViewSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call =>
				{
					FavoritesViewSettings settings = call.Arg<FavoritesViewSettings>();

					savedCategoryId = settings.SelectedCategoryId;

					savedPairs = [.. settings.SelectedPairs];
				});

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		FavoritesWindow window = sut.CreateFavoritesWindow([], null, []);

		SelectedFavoritesViewModel favorites = windowMock.Create<SelectedFavoritesViewModel>();

		window
			.ViewModel
			.FavoritesSettings
			.Categories
			.AddRange(categories);

		window
			.ViewModel
			.FavoritesSettings
			.SelectedPairs
			.AddRange(pairs);

		window.ViewModel.IsPopupFixed = isPopupFixed;

		window
			.ViewModel
			.ShowFavorites();

		// The view of the popup hands its view model over once loaded, and the stored selection comes back through the
		// dispatcher before the choice.
		window
			.ViewModel
			.FavoritesDisplayedCommand
			.Execute(favorites);

		Dispatcher.UIThread.RunJobs();

		favorites.SelectedCategory = categories[1];

		// Act
		await window
			.ViewModel
			.ShowInEditorAsync(default, window);

		// Closed before the assertions: its dialog host would stay in the list every headless test shares.
		editorWindow.Close();

		// Assert
		savedCategoryId
			.Should()
			.Be(categories[1].Id);

		savedPairs
			.Should()
			.Equal(pairs);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: closing the window saves the category chosen in the popup, fixed
	/// or not.
	/// </summary>
	[AvaloniaTest]
	public void CreateFavoritesWindow_Saves_The_Chosen_Category_When_The_Window_Closes([Values] bool isPopupFixed)
	{
		// Arrange
		FavoriteCategory[] categories = [.. FavoriteFactory.CreateFavoriteCategories(2)];

		Guid savedCategoryId = default;

		// The favorites live as long as the test, as the window saves them when it closes.
		using AutoMock windowMock = AutoMock.GetLoose();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<FavoritesViewSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => savedCategoryId = call.Arg<FavoritesViewSettings>().SelectedCategoryId);

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		FavoritesWindow window = sut.CreateFavoritesWindow([], null, []);

		SelectedFavoritesViewModel favorites = windowMock.Create<SelectedFavoritesViewModel>();

		window
			.ViewModel
			.FavoritesSettings
			.Categories
			.AddRange(categories);

		window.ViewModel.IsPopupFixed = isPopupFixed;

		window
			.ViewModel
			.ShowFavorites();

		// The view of the popup hands its view model over once loaded, and the stored selection comes back through the
		// dispatcher before the choice.
		window
			.ViewModel
			.FavoritesDisplayedCommand
			.Execute(favorites);

		Dispatcher.UIThread.RunJobs();

		favorites.SelectedCategory = categories[1];

		// Act
		window.Close();

		// Assert
		savedCategoryId
			.Should()
			.Be(categories[1].Id);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: a switch to the editor saves the item chosen in the copy history
	/// of the popup, fixed or not.
	/// </summary>
	[AvaloniaTest]
	public async Task CreateFavoritesWindow_Saves_The_Chosen_Copy_History_Item_When_Switching_To_The_Editor(
		[Values] bool isPopupFixed)
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		EditorWindow editorWindow = new();

		Guid savedItemId = default;

		// The favorites live as long as the test, as the window saves them when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

			viewLauncher.CreateEditorWindow(
				Arg.Any<IEnumerable<ExplorerItemDtoBase>>(),
				Arg.Any<EditorTabsState?>(),
				Arg.Any<IEnumerable<FileDto>>())
			.Returns(editorWindow);

			builder.RegisterInstance(viewLauncher);
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<CopyHistoryViewSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => savedItemId = call.Arg<CopyHistoryViewSettings>().SelectedItemId);

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		FavoritesWindow window = sut.CreateFavoritesWindow(files, null, []);

		CopyHistoryViewModel copyHistory = windowMock.Create<CopyHistoryViewModel>();

		window
			.ViewModel
			.CopyHistorySettings
			.AddItemIds(files.Select(x => x.Id), files);

		window.ViewModel.IsPopupFixed = isPopupFixed;

		window
			.ViewModel
			.ShowCopyHistoryCommand
			.Execute(null);

		// The view of the popup hands its view model over once loaded, and the stored selection comes back through the
		// dispatcher before the choice.
		window
			.ViewModel
			.CopyHistoryDisplayedCommand
			.Execute(copyHistory);

		Dispatcher.UIThread.RunJobs();

		copyHistory.SelectedItem = files[1];

		// Act
		await window
			.ViewModel
			.ShowInEditorAsync(default, window);

		// Closed before the assertions: its dialog host would stay in the list every headless test shares.
		editorWindow.Close();

		// Assert
		savedItemId
			.Should()
			.Be(files[1].Id);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateMainWindow" />: an editor window is created as the main window.
	/// </summary>
	[AvaloniaTest]
	public void CreateMainWindow_Configures_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		Window window = sut.CreateMainWindow([]);

		// Assert
		window
			.Should()
			.BeOfType<EditorWindow>();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateMainWindow" />: an editor window is created when no saved window setting exists.
	/// </summary>
	[AvaloniaTest]
	public void CreateMainWindow_Configures_Editor_If_No_Settings()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		Window window = sut.CreateMainWindow([]);

		// Assert
		window
			.Should()
			.BeOfType<EditorWindow>();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateMainWindow" />: a favorites window is created when the saved window setting is Favorites.
	/// </summary>
	[AvaloniaTest]
	public void CreateMainWindow_Configures_Favorites()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<WindowKind>(Arg.Any<string>())
				.Returns(WindowKind.Favorites);

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		Window window = sut.CreateMainWindow([]);

		// Assert
		window
			.Should()
			.BeOfType<FavoritesWindow>();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateNotepadWindow" />: the saved size, position and topmost flag are applied to the window.
	/// </summary>
	[AvaloniaTest]
	public void CreateNotepadWindow_Applies_Saved_Settings()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			NotepadWindowSettings settings = new()
			{
				IsTopmost = true,
				Size = new(500, 400),
				WindowState = WindowState.Normal,
				X = 30,
				Y = 40
			};

			NotepadViewModel viewModel = new(
				Substitute.For<IDialogService>(),
				Substitute.For<IDispatcherAccessor>(),
				Substitute.For<INotepadSessionState>(),
				Substitute.For<INotepadStore>(),
				Substitute.For<INotificationService>(),
				Substitute.For<ISyntaxRegistry>(),
				Substitute.For<IViewCache>(),
				Substitute.For<IViewLauncher>(),
				TimeProvider.System);

			NotepadWindow notepadWindow = new(viewModel);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<NotepadWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(notepadWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		NotepadWindow window = sut.CreateNotepadWindow(new Window());

		// Assert
		window.Width
			.Should()
			.Be(500.0);

		window.Height
			.Should()
			.Be(400.0);

		window.Position
			.Should()
			.Be(new PixelPoint(30, 40));

		window.Topmost
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateNotepadWindow" />: a window saved off every screen opens in the center of the screen
	/// of the owner.
	/// </summary>
	[AvaloniaTest]
	public void CreateNotepadWindow_Centers_A_Window_Saved_Off_The_Screens()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			NotepadWindowSettings settings = new()
			{
				IsTopmost = false,
				Size = new(500, 400),
				WindowState = WindowState.Normal,
				X = -20000,
				Y = -20000
			};

			NotepadViewModel viewModel = new(
				Substitute.For<IDialogService>(),
				Substitute.For<IDispatcherAccessor>(),
				Substitute.For<INotepadSessionState>(),
				Substitute.For<INotepadStore>(),
				Substitute.For<INotificationService>(),
				Substitute.For<ISyntaxRegistry>(),
				Substitute.For<IViewCache>(),
				Substitute.For<IViewLauncher>(),
				TimeProvider.System);

			NotepadWindow notepadWindow = new(viewModel);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<NotepadWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(notepadWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		Window owner = new();

		// Act
		NotepadWindow window = sut.CreateNotepadWindow(owner);

		// Assert
		PixelRect bounds = new(window.Position, new PixelSize(500, 400));

		bounds.Center
			.Should()
			.Be(owner.Screens!.Primary!.WorkingArea.Center);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateNotepadWindow" />: the view model of the window is disposed when the window closes, so
	/// the editors of its tabs go with it.
	/// </summary>
	[AvaloniaTest]
	public void CreateNotepadWindow_Disposes_The_View_Model_When_The_Window_Closes()
	{
		// Arrange
		NotepadViewModel viewModel = new(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<IViewCache>(),
			Substitute.For<IViewLauncher>(),
			TimeProvider.System);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			NotepadWindow notepadWindow = new(viewModel);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(notepadWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		NotepadWindow window = sut.CreateNotepadWindow(new Window());

		window.Show();

		// Act
		window.Close();

		// Assert
		viewModel.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateNotepadWindow" />: without saved settings the window takes the default size and
	/// opens in the center of the screen of the owner.
	/// </summary>
	[AvaloniaTest]
	public void CreateNotepadWindow_Opens_Centered_With_The_Default_Size_For_The_First_Launch()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			NotepadViewModel viewModel = new(
				Substitute.For<IDialogService>(),
				Substitute.For<IDispatcherAccessor>(),
				Substitute.For<INotepadSessionState>(),
				Substitute.For<INotepadStore>(),
				Substitute.For<INotificationService>(),
				Substitute.For<ISyntaxRegistry>(),
				Substitute.For<IViewCache>(),
				Substitute.For<IViewLauncher>(),
				TimeProvider.System);

			NotepadWindow notepadWindow = new(viewModel);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(notepadWindow);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		Window owner = new();

		// Act
		NotepadWindow window = sut.CreateNotepadWindow(owner);

		// Assert
		window.Width
			.Should()
			.Be(IViewLauncher.DefaultWindowSize.Width);

		window.Height
			.Should()
			.Be(IViewLauncher.DefaultWindowSize.Height);

		PixelRect bounds = new(
			window.Position,
			new PixelSize(IViewLauncher.DefaultWindowSize.Width, IViewLauncher.DefaultWindowSize.Height));

		bounds.Center
			.Should()
			.Be(owner.Screens!.Primary!.WorkingArea.Center);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateNotepadWindow" />: the window opens the saved tabs and selects the saved one.
	/// </summary>
	[AvaloniaTest]
	public void CreateNotepadWindow_Opens_The_Saved_Tabs()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			NotepadViewSettings settings = new()
			{
				SelectedTabNumber = 1,
				Tabs =
				[
					new()
					{
						EditorState = null,
						Name = null,
						Number = 3,
						Split = null
					},
					new()
					{
						EditorState = null,
						Name = null,
						Number = 1,
						Split = null
					},
					new()
					{
						EditorState = null,
						Name = null,
						Number = 2,
						Split = null
					}
				]
			};

			INotepadStore store = Substitute.For<INotepadStore>();

			store
				.ReadSettings()
				.Returns(settings);

			NotepadViewModel viewModel = new(
				Substitute.For<IDialogService>(),
				Substitute.For<IDispatcherAccessor>(),
				Substitute.For<INotepadSessionState>(),
				store,
				Substitute.For<INotificationService>(),
				Substitute.For<ISyntaxRegistry>(),
				Substitute.For<IViewCache>(),
				Substitute.For<IViewLauncher>(),
				TimeProvider.System);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(viewModel);

			// Created on the call, as by the real factory, so the window binds to the tabs restored before it.
			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(_ => new NotepadWindow(viewModel));

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		NotepadWindow window = sut.CreateNotepadWindow(new Window());

		window.Show();

		object? selected = window.Tabs.SelectedItem;

		// Closed before the assertions: a closed notepad takes its dialog host out of the list every headless test shares.
		window.Close();

		// Assert
		window.ViewModel.Tabs.Select(x => x.Number)
			.Should()
			.Equal(3, 1, 2);

		selected
			.Should()
			.BeSameAs(window.ViewModel.Tabs[1]);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateNotepadWindow" />: the tabs of the window get their texts from the disk.
	/// </summary>
	[AvaloniaTest]
	public void CreateNotepadWindow_Reads_The_Texts_Of_The_Tabs()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INotepadStore store = Substitute.For<INotepadStore>();

			store
				.Read(1)
				.Returns(Encoding.UTF8.GetBytes("Text"));

			NotepadViewModel viewModel = new(
				Substitute.For<IDialogService>(),
				Substitute.For<IDispatcherAccessor>(),
				Substitute.For<INotepadSessionState>(),
				store,
				Substitute.For<INotificationService>(),
				Substitute.For<ISyntaxRegistry>(),
				Substitute.For<IViewCache>(),
				Substitute.For<IViewLauncher>(),
				TimeProvider.System);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(_ => new NotepadWindow(viewModel));

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		NotepadWindow window = sut.CreateNotepadWindow(new Window());

		// Assert
		window.ViewModel.Tabs[0].Document.Text
			.Should()
			.Be("Text");
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateNotepadWindow" />: the window opens in the saved state, a minimized one as a normal one.
	/// </summary>
	[AvaloniaTest]
	[TestCase(WindowState.Maximized, WindowState.Maximized)]
	[TestCase(WindowState.Minimized, WindowState.Normal)]
	public void CreateNotepadWindow_Restores_The_Saved_State(WindowState saved, WindowState expected)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			NotepadWindowSettings settings = new()
			{
				IsTopmost = false,
				Size = new(500, 400),
				WindowState = saved,
				X = 30,
				Y = 40
			};

			NotepadViewModel viewModel = new(
				Substitute.For<IDialogService>(),
				Substitute.For<IDispatcherAccessor>(),
				Substitute.For<INotepadSessionState>(),
				Substitute.For<INotepadStore>(),
				Substitute.For<INotificationService>(),
				Substitute.For<ISyntaxRegistry>(),
				Substitute.For<IViewCache>(),
				Substitute.For<IViewLauncher>(),
				TimeProvider.System);

			NotepadWindow notepadWindow = new(viewModel);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<NotepadWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(notepadWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		NotepadWindow window = sut.CreateNotepadWindow(new Window());

		// Assert
		window.WindowState
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateNotepadWindow" />: the window saves its settings when it closes.
	/// </summary>
	[AvaloniaTest]
	public void CreateNotepadWindow_Saves_The_Settings_When_The_Window_Closes()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			NotepadViewModel viewModel = new(
				Substitute.For<IDialogService>(),
				Substitute.For<IDispatcherAccessor>(),
				Substitute.For<INotepadSessionState>(),
				Substitute.For<INotepadStore>(),
				Substitute.For<INotificationService>(),
				Substitute.For<ISyntaxRegistry>(),
				Substitute.For<IViewCache>(),
				Substitute.For<IViewLauncher>(),
				TimeProvider.System);

			NotepadWindow notepadWindow = new(viewModel);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(notepadWindow);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(fileSystem);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		NotepadWindow window = sut.CreateNotepadWindow(new Window());

		window.Show();

		// Act
		window.Close();

		// Assert
		fileSystem.Received(1).SerializeToJsonFile(
			Arg.Any<NotepadWindowSettings>(),
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.SaveClipboardLogSettings" />: the active type filter is persisted.
	/// </summary>
	[AvaloniaTest]
	public void SaveClipboardLogSettings_Persists_ActiveFilter()
	{
		// Arrange
		ClipboardLogWindowSettings? captured = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			clipboardLogService
				.Entries
				.Returns([]);

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<ClipboardLogWindowSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => captured = call.Arg<ClipboardLogWindowSettings>());

			builder.RegisterInstance(clipboardLogService);

			builder.RegisterInstance(fileSystem);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		ClipboardLogWindow window = mock.Create<ClipboardLogWindow>();

		window.ViewModel.ActiveFilter = ClipboardLogEntryFilter.Image;

		// Act
		sut.SaveClipboardLogSettings(window);

		// Assert
		captured
			.Should()
			.NotBeNull();

		captured.ActiveFilter
			.Should()
			.Be(ClipboardLogEntryFilter.Image);
	}

	/// <summary>
	/// <see cref="ViewLauncher.SaveClipboardLogSettings" />: the keep-open flag is persisted.
	/// </summary>
	[AvaloniaTest]
	public void SaveClipboardLogSettings_Persists_KeepOpen()
	{
		// Arrange
		ClipboardLogWindowSettings? captured = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			clipboardLogService
				.Entries
				.Returns([]);

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<ClipboardLogWindowSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => captured = call.Arg<ClipboardLogWindowSettings>());

			builder.RegisterInstance(clipboardLogService);

			builder.RegisterInstance(fileSystem);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		ClipboardLogWindow window = mock.Create<ClipboardLogWindow>();

		window.ViewModel.KeepOpen = true;

		// Act
		sut.SaveClipboardLogSettings(window);

		// Assert
		captured
			.Should()
			.NotBeNull();

		captured.KeepOpen
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.SaveClipboardLogSettings" />: clipboard window settings are serialized to a JSON file.
	/// </summary>
	[AvaloniaTest]
	public void SaveClipboardLogSettings_Saves_Settings()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

			clipboardLogService
				.Entries
				.Returns([]);

			builder.RegisterInstance(clipboardLogService);

			builder.RegisterInstance(fileSystem);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		sut.SaveClipboardLogSettings(mock.Create<ClipboardLogWindow>());

		// Assert
		fileSystem.Received(1).SerializeToJsonFile(
			Arg.Any<ClipboardLogWindowSettings>(),
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.SaveEditorSettingsAsync" />: the shutdown closes the notepad, so that it saves its settings.
	/// </summary>
	[AvaloniaTest]
	public async Task SaveEditorSettingsAsync_Closes_The_Notepad_On_Shutdown()
	{
		// Arrange
		bool isClosed = false;

		NotepadWindow notepad = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<IViewCache>(),
			Substitute.For<IViewLauncher>(),
			TimeProvider.System));

		notepad.Closed += (_, _) => isClosed = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([notepad]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = mock.Create<EditorWindow>();

		notepad.Show();

		// Act
		await sut.SaveEditorSettingsAsync(window);

		// Assert
		isClosed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.SaveEditorSettingsAsync" />: editor settings and the current window kind are serialized to JSON files.
	/// </summary>
	[AvaloniaTest]
	public async Task SaveEditorSettingsAsync_Saves_Settings()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(fileSystem);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = mock.Create<EditorWindow>();

		window.Topmost = true;

		// Act
		await sut.SaveEditorSettingsAsync(window);

		// Assert
		fileSystem.Received(1).SerializeToJsonFile(
			Arg.Is<EditorWindowSettings>(x => x.IsTopmost),
			Arg.Any<string>(),
			Arg.Any<bool>());

		fileSystem.Received(1).SerializeToJsonFile(
			WindowKind.Editor,
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.SaveFavoritesSettingsAsync" />: favorites collections are cleared and settings with the current window kind are serialized to JSON files.
	/// </summary>
	[AvaloniaTest]
	public async Task SaveFavoritesSettingsAsync_Saves_Settings()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(fileSystem);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		FavoritesWindow window = mock.Create<FavoritesWindow>();

		window
			.ViewModel
			.FavoritesSettings
			.Categories
			.AddRange(FavoriteFactory.CreateFavoriteCategories(5));

		window
			.ViewModel
			.FavoritesSettings
			.SelectedPairs
			.AddRange(FavoriteFactory.CreateFavoriteSelections(5));

		// Act
		await sut.SaveFavoritesSettingsAsync(window);

		// Assert
		window.ViewModel.FavoritesSettings.Categories
			.Should()
			.BeEmpty();

		window.ViewModel.FavoritesSettings.OrderedCategoryIds
			.Should()
			.BeEmpty();

		window.ViewModel.FavoritesSettings.SelectedPairs
			.Should()
			.BeEmpty();

		fileSystem.Received(1).SerializeToJsonFile(
			Arg.Any<FavoritesWindowSettings>(),
			Arg.Any<string>(),
			Arg.Any<bool>());

		fileSystem.Received(1).SerializeToJsonFile(
			WindowKind.Favorites,
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.SaveNotepadSettings" />: the position, size, state and topmost flag of the window are saved.
	/// </summary>
	[AvaloniaTest]
	public void SaveNotepadSettings_Saves_Settings()
	{
		// Arrange
		NotepadWindowSettings? captured = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<NotepadWindowSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => captured = call.Arg<NotepadWindowSettings>());

			builder.RegisterInstance(fileSystem);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		NotepadWindow window = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<IViewCache>(),
			Substitute.For<IViewLauncher>(),
			TimeProvider.System))
		{
			Height = 400.0,
			Position = new PixelPoint(30, 40),
			Topmost = true,
			Width = 500.0
		};

		// Act
		sut.SaveNotepadSettings(window);

		// Assert
		captured
			.Should()
			.BeEquivalentTo(new NotepadWindowSettings
			{
				IsTopmost = true,
				Size = new(500, 400),
				WindowState = WindowState.Normal,
				X = 30,
				Y = 40
			});
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowClipboardLogWindowAsync" />: the first password of the saved
	/// history is created with a confirmation, a later one is only checked.
	/// </summary>
	[AvaloniaTest]
	[TestCase(false, PasswordPromptMode.Create)]
	[TestCase(true, PasswordPromptMode.Verify)]
	public async Task ShowClipboardLogWindowAsync_Asks_For_The_History_Password(bool hasPassword, PasswordPromptMode expected)
	{
		// Arrange
		IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder =>
			{
				IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

				clipboardLogService
					.Entries
					.Returns([]);

				windowBuilder.RegisterInstance(clipboardLogService);
			});

			ClipboardLogViewModel viewModel = windowMock.Create<ClipboardLogViewModel>();

			ClipboardLogWindow clipboardWindow = windowMock.Create<ClipboardLogWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<ClipboardLogViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<ClipboardLogWindow>(Arg.Any<object[]>())
				.Returns(clipboardWindow);

			IClipboardLogPersistenceCoordinator persistence = Substitute.For<IClipboardLogPersistenceCoordinator>();

			persistence
				.RequiresUnlock
				.Returns(true);

			persistence
				.HasPassword
				.Returns(hasPassword);

			// A cancelled prompt leaves the session in memory, which is enough to reach the assert.
			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(new PinnedSecret(length: 0));

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(persistence);

			builder.RegisterInstance(passwordPrompt);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		await sut.ShowClipboardLogWindowAsync(new Window());

		// Assert
		await passwordPrompt.Received(1).RequestPasswordAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			expected,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowClipboardLogWindowAsync" />: an already-open window is focused instead of opening a duplicate.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowClipboardLogWindowAsync_Focuses_Existing_Window()
	{
		// Arrange
		using AutoMock windowMock = AutoMock.GetLoose(windowBuilder =>
		{
			IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

			clipboardLogService
				.Entries
				.Returns([]);

			windowBuilder.RegisterInstance(clipboardLogService);
		});

		ClipboardLogViewModel viewModel = windowMock.Create<ClipboardLogViewModel>();

		ClipboardLogWindow existing = windowMock.Create<ClipboardLogWindow>(TypedParameter.From(viewModel));

		IViewFactory viewFactory = Substitute.For<IViewFactory>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([existing]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		await sut.ShowClipboardLogWindowAsync(new Window());

		// Assert
		viewFactory
			.DidNotReceive()
			.CreateWindow<ClipboardLogWindow>(Arg.Any<object[]>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowNotepadWindow" />: without an open notepad a new one is opened.
	/// </summary>
	[AvaloniaTest]
	public void ShowNotepadWindow_Opens_A_New_Notepad()
	{
		// Arrange
		NotepadWindow notepad = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<IViewCache>(),
			Substitute.For<IViewLauncher>(),
			TimeProvider.System));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(notepad.ViewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(notepad);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		sut.ShowNotepadWindow(new Window());

		bool isVisible = notepad.IsVisible;

		// Closed before the assertion: a closed notepad takes its dialog host out of the list every headless test shares.
		notepad.Close();

		// Assert
		isVisible
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowNotepadWindow" />: the open notepad comes back from the taskbar instead of a second one.
	/// </summary>
	[AvaloniaTest]
	public void ShowNotepadWindow_Restores_The_Open_Notepad()
	{
		// Arrange
		NotepadWindow notepad = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<IViewCache>(),
			Substitute.For<IViewLauncher>(),
			TimeProvider.System))
		{
			WindowState = WindowState.Minimized
		};

		IViewFactory viewFactory = Substitute.For<IViewFactory>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([notepad]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		notepad.Show();

		// Act
		sut.ShowNotepadWindow(new Window());

		WindowState state = notepad.WindowState;

		// Closed before the assertions: a closed notepad takes its dialog host out of the list every headless test shares.
		notepad.Close();

		// Assert
		state
			.Should()
			.Be(WindowState.Normal);

		viewFactory
			.DidNotReceive()
			.CreateWindow<NotepadWindow>(Arg.Any<object[]>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowStartupErrorAsync" />: with no desktop lifetime to shut down, the process
	/// is ended even when the notice itself could not be shown.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowStartupErrorAsync_Ends_The_Process_Without_A_Desktop_Lifetime()
	{
		// Arrange
		IProcessTerminator processTerminator = Substitute.For<IProcessTerminator>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NoticeViewModel>()
				.Returns(_ => throw new InvalidOperationException());

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(processTerminator);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		await sut.ShowStartupErrorAsync(@"C:\Database\DataOrganizer.sqlite");

		// Assert
		processTerminator
			.Received(1)
			.Terminate(Arg.Any<int>());
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Lets the UI thread run until <paramref name="condition" /> holds, a few seconds at most.
	/// </summary>
	private static async Task WaitUntilAsync(Func<bool> condition)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();

		while (!condition() && stopwatch.Elapsed < TimeSpan.FromSeconds(5.0))
		{
			await Task.Delay(20);

			Dispatcher.UIThread.RunJobs();
		}
	}
	#endregion
}
