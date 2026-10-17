using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Xaml.Interactions.Core;
using Avalonia.Xaml.Interactions.Custom;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Enums.Dialogs;
using DataOrganizer.Enums.Views;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Notepad;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Storage;
using DataOrganizer.Interfaces.Text;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Editor;
using DataOrganizer.Messages.Runtime;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Execution;
using DataOrganizer.Services.Runtime;
using DataOrganizer.Services.Settings;
using DataOrganizer.Services.Views;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using Material.Styles.Controls;
using NSubstitute;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
		using AutoMock windowMock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		Window mainWindow = new()
		{
			DataContext = windowMock.Create<FavoritesViewModel>(),
			WindowState = WindowState.Minimized
		};

		NotepadWindow notepad = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
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
		using AutoMock windowMock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		Window mainWindow = new()
		{
			DataContext = windowMock.Create<FavoritesViewModel>(),
			Height = 300.0,
			Width = 400.0
		};

		NotepadWindow notepad = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
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

		using AutoMock windowMock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

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
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
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
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
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
	/// <see cref="ViewLauncher.CloseSecondaryWindowsAsync" />: a console whose log is not saved yet is closed, so that it
	/// saves the log.
	/// </summary>
	[AvaloniaTest]
	public async Task CloseSecondaryWindowsAsync_Closes_A_Console_With_An_Unsaved_Log()
	{
		// Arrange
		bool isClosed = false;

		ConsoleWindow console = new(new ConsoleViewModel(
			Substitute.For<IAppEnvironment>(),
			Substitute.For<IDirectoryAccessor>(),
			new InlineDispatcherAccessor()));

		console.Closed += (_, _) => isClosed = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns(_ => isClosed ? [] : [console]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		console.Show();

		// Act
		await sut.CloseSecondaryWindowsAsync();

		// Assert
		isClosed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CloseSecondaryWindowsAsync" />: the notepad is closed, so that it saves its settings.
	/// </summary>
	[AvaloniaTest]
	public async Task CloseSecondaryWindowsAsync_Closes_The_Notepad()
	{
		// Arrange
		bool isClosed = false;

		NotepadWindow notepad = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
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

		notepad.Show();

		// Act
		await sut.CloseSecondaryWindowsAsync();

		// Assert
		isClosed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: the exit item of the menu, which closes the window, asks for the exit
	/// of the application once.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Asks_For_The_Exit_Once_When_The_Exit_Menu_Item_Is_Clicked()
	{
		// Arrange
		int sentCount = 0;

		IMessenger messenger = new StrongReferenceMessenger();

		messenger.Register<ExitAppMessage>(this, (_, _) => sentCount++);

		// The editor lives as long as the test, as the window saves it when it closes. It shares the messenger, so a
		// request sent by the view model is counted too.
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(messenger);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = sut.CreateEditorWindow();

		window.Show();

		// The menu is in the side drawer, which the user opens first.
		window.ViewModel.IsLeftDrawerOpened = true;

		Dispatcher.UIThread.RunJobs();

		MenuItem exitItem = FindExitMenuItem(window);

		// Act
		exitItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

		// Assert
		sentCount
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: a window that the shutting down application closes asks for the exit
	/// of the application.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Asks_For_The_Exit_When_The_Application_Shuts_Down()
	{
		// Arrange
		IMessenger messenger = new StrongReferenceMessenger();

		ExitAppMessage? sent = null;

		messenger.Register<ExitAppMessage>(this, (_, message) => sent = message);

		// The editor lives as long as the test, as the window saves it when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

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

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = sut.CreateEditorWindow();

		// The desktop lifetime closes its windows by code, with the reason of the shutdown.
		MethodInfo closeCore = typeof(Window).GetMethod("CloseCore", BindingFlags.Instance | BindingFlags.NonPublic)!;

		// Act
		closeCore.Invoke(window, [WindowCloseReason.ApplicationShutdown, true, false]);

		// Assert
		sent
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: the close button of the window, which closes it by code, asks for the
	/// exit of the application.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Asks_For_The_Exit_When_The_Close_Button_Is_Clicked()
	{
		// Arrange
		bool isClosed = false;

		IMessenger messenger = new StrongReferenceMessenger();

		ExitAppMessage? sent = null;

		messenger.Register<ExitAppMessage>(this, (_, message) => sent = message);

		// The editor lives as long as the test, as the window saves it when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

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

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = sut.CreateEditorWindow();

		window.Closed += (_, _) => isClosed = true;

		window.Show();

		Dispatcher.UIThread.RunJobs();

		Button closeButton = FindCloseButton(window);

		// Act
		closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

		// Assert
		isClosed
			.Should()
			.BeTrue();

		sent
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: a window the user closes asks for the exit of the application.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Asks_For_The_Exit_When_The_User_Closes_The_Window()
	{
		// Arrange
		IMessenger messenger = new StrongReferenceMessenger();

		ExitAppMessage? sent = null;

		messenger.Register<ExitAppMessage>(this, (_, message) => sent = message);

		// The editor lives as long as the test, as the window saves it when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

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

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = sut.CreateEditorWindow();

		// The window is closed the way the system closes it for the user, on Alt+F4 or from the taskbar.
		Func<WindowCloseReason, bool> closeByUser = (Func<WindowCloseReason, bool>)typeof(IWindowImpl)
			.GetProperty("Closing")!
			.GetValue(window.PlatformImpl)!;

		// Act
		closeByUser(WindowCloseReason.WindowClosing);

		// Assert
		sent
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: a window saved off every screen opens in the center of the screen.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Centers_A_Window_Saved_Off_The_Screens()
	{
		// Arrange
		EditorWindowSettings settings = new()
		{
			IsReadOnly = false,
			IsTopmost = false,
			NavigationColumnWidth = 200.0,
			Size = new(700, 500),
			WindowState = WindowShowState.Normal,
			X = 50000,
			Y = 50000
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		EditorWindow window = sut.CreateEditorWindow();

		// Assert
		window.WindowStartupLocation
			.Should()
			.Be(WindowStartupLocation.CenterScreen);
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
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		EditorWindow window = sut.CreateEditorWindow();

		// Assert
		window.Width
			.Should()
			.Be(WindowPlacementHelper.DefaultWindowSize.Width);

		window.Height
			.Should()
			.Be(WindowPlacementHelper.DefaultWindowSize.Height);

		window.WindowStartupLocation
			.Should()
			.Be(WindowStartupLocation.CenterScreen);

		window.ViewModel.NavigationColumnWidth
			.Should()
			.Be(window.Width / 3.0);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: a window saved without a size takes the default one, and its
	/// navigation column a third of it.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Gives_The_Default_Size_Without_A_Saved_One()
	{
		// Arrange
		EditorWindowSettings settings = new()
		{
			IsReadOnly = false,
			IsTopmost = false,
			NavigationColumnWidth = 0.0,
			Size = new(0, 0),
			WindowState = WindowShowState.Normal,
			X = 100,
			Y = 100
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		EditorWindow window = sut.CreateEditorWindow();

		// Assert
		window.Width
			.Should()
			.Be(WindowPlacementHelper.DefaultWindowSize.Width);

		window.Height
			.Should()
			.Be(WindowPlacementHelper.DefaultWindowSize.Height);

		window.ViewModel.NavigationColumnWidth
			.Should()
			.Be(WindowPlacementHelper.DefaultWindowSize.Width / 3.0);
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
			WindowState = WindowShowState.Normal,
			X = positiveValue,
			Y = positiveValue
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		EditorWindow window = sut.CreateEditorWindow();

		// Assert
		window.ViewModel.IsInitialized
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: a switch to the favorites closes the editor without asking for the
	/// exit of the application.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Keeps_The_Application_Running_After_A_Switch_To_The_Favorites()
	{
		// Arrange
		FavoritesWindow favoritesWindow = new();

		IMessenger messenger = new StrongReferenceMessenger();

		ExitAppMessage? sent = null;

		messenger.Register<ExitAppMessage>(this, (_, message) => sent = message);

		// The editor lives as long as the test, as the window saves it when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

			mainWindowLauncher
				.CreateFavoritesWindow()
				.Returns(favoritesWindow);

			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = sut.CreateEditorWindow();

		// Act
		window
			.ViewModel
			.ShowFavorites(window);

		// Closed before the assertions: its dialog host would stay in the list every headless test shares.
		favoritesWindow.Close();

		// Assert
		sent
			.Should()
			.BeNull();
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

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			windowMock
				.Create<ISessionHierarchy>()
				.Hierarchy
				.Add(selected);

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
		sut.CreateEditorWindow();

		// Assert
		received
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: a window closed minimized opens in the normal state.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Opens_A_Window_Saved_Minimized_In_The_Normal_State()
	{
		// Arrange
		EditorWindowSettings settings = new()
		{
			IsReadOnly = false,
			IsTopmost = false,
			NavigationColumnWidth = 200.0,
			Size = new(700, 500),
			WindowState = WindowShowState.Minimized,
			X = 100,
			Y = 100
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		EditorWindow window = sut.CreateEditorWindow();

		// Assert
		window.WindowState
			.Should()
			.Be(WindowState.Normal);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: the window takes the position, the size, the state and the topmost
	/// switch saved for it.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Places_The_Window_From_Saved_Settings()
	{
		// Arrange
		int positiveValue = RandomValues.CreateInt(100, 300);

		EditorWindowSettings settings = new()
		{
			IsReadOnly = false,
			IsTopmost = true,
			NavigationColumnWidth = positiveValue - 20,
			Size = new(positiveValue, positiveValue),
			WindowState = WindowShowState.Normal,
			X = positiveValue,
			Y = positiveValue
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		EditorWindow window = sut.CreateEditorWindow();

		// Assert
		window.Position
			.Should()
			.Be(new PixelPoint(settings.X, settings.Y));

		window.Width
			.Should()
			.Be(settings.Size.Width);

		window.Height
			.Should()
			.Be(settings.Size.Height);

		window.WindowState
			.Should()
			.Be(WindowState.Normal);

		window.Topmost
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: a window saved at zero or negative coordinates, as at the edge of
	/// the screen or on a screen left of the main one, comes back there.
	/// </summary>
	[AvaloniaTest]
	[TestCase(0, 0)]
	[TestCase(-50, 100)]
	public void CreateEditorWindow_Restores_A_Window_Saved_At_Zero_Or_Negative_Coordinates(int x, int y)
	{
		// Arrange
		EditorWindowSettings settings = new()
		{
			IsReadOnly = false,
			IsTopmost = false,
			NavigationColumnWidth = 200.0,
			Size = new(700, 500),
			WindowState = WindowShowState.Normal,
			X = x,
			Y = y
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			// The window stands elsewhere, so only the restore brings it to the stored position.
			editorWindow.Position = new PixelPoint(200, 150);

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
		EditorWindow window = sut.CreateEditorWindow();

		// Assert
		window.Position
			.Should()
			.Be(new PixelPoint(x, y));
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: closing the window serializes the editor settings and the current
	/// window kind to JSON files.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Saves_Settings_When_The_Window_Closes()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		// The editor lives as long as the test, as the window saves it when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

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

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = sut.CreateEditorWindow();

		window.Topmost = true;

		// Act
		window.Close();

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
	/// <see cref="ViewLauncher.CreateEditorWindow" />: a switch to the favorites saves the copy history with the item chosen
	/// in its list.
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
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<CopyHistoryViewSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => savedItemId = call.Arg<CopyHistoryViewSettings>().SelectedItemId);

			mainWindowLauncher
				.CreateFavoritesWindow()
				.Returns(favoritesWindow);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			windowMock
				.Create<ISessionHierarchy>()
				.Hierarchy
				.AddRange(files);

			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance<ICopyHistoryStore>(windowMock.Create<CopyHistory>());

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = sut.CreateEditorWindow();

		CopyHistory copyHistory = windowMock.Create<CopyHistory>();

		copyHistory.Add(files[1], showAtOnce: false);

		copyHistory.Add(files[0], showAtOnce: false);

		CopyHistoryViewModel copyHistoryList = windowMock.Create<CopyHistoryViewModel>();

		// The stored selection comes back through the dispatcher before the choice.
		Dispatcher.UIThread.RunJobs();

		copyHistoryList.SelectedItem = files[1];

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
	/// <see cref="ViewLauncher.CreateEditorWindow" />: closing the window saves the copy history with the item chosen in its
	/// list.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Saves_The_Chosen_Copy_History_Item_When_The_Window_Closes()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		Guid savedItemId = default;

		// The editor lives as long as the test, as the window saves it when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<CopyHistoryViewSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => savedItemId = call.Arg<CopyHistoryViewSettings>().SelectedItemId);

			builder.RegisterInstance(fileSystem);

			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			windowMock
				.Create<ISessionHierarchy>()
				.Hierarchy
				.AddRange(files);

			EditorViewModel viewModel = windowMock.Create<EditorViewModel>();

			EditorWindow editorWindow = windowMock.Create<EditorWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<EditorWindow>(Arg.Any<object[]>())
				.Returns(editorWindow);

			builder.RegisterInstance<ICopyHistoryStore>(windowMock.Create<CopyHistory>());

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = sut.CreateEditorWindow();

		CopyHistory copyHistory = windowMock.Create<CopyHistory>();

		copyHistory.Add(files[1], showAtOnce: false);

		copyHistory.Add(files[0], showAtOnce: false);

		CopyHistoryViewModel copyHistoryList = windowMock.Create<CopyHistoryViewModel>();

		// The stored selection comes back through the dispatcher before the choice.
		Dispatcher.UIThread.RunJobs();

		copyHistoryList.SelectedItem = files[1];

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

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			windowMock
				.Create<ISessionHierarchy>()
				.Hierarchy
				.AddRange(hierarchy);

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
		window = sut.CreateEditorWindow(shown.Id);

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
	/// <see cref="ViewLauncher.CreateEditorWindow" />: closing the window saves the width of its navigation column.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Saves_The_Navigation_Column_Width_When_The_Window_Closes()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		// The editor lives as long as the test, as the window saves it when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

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

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = sut.CreateEditorWindow();

		window.ViewModel.NavigationColumnWidth = 222.0;

		// Act
		window.Close();

		// Assert
		fileSystem.Received(1).SerializeToJsonFile(
			Arg.Is<EditorWindowSettings>(x => x.NavigationColumnWidth == 222.0),
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: closing the window saves the read-only mode for the next editor.

	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Saves_The_Read_Only_Mode_When_The_Window_Closes()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		// The editor lives as long as the test, as the window saves it when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

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

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = sut.CreateEditorWindow();

		window.ViewModel.IsReadOnly = true;

		// Act
		window.Close();

		// Assert
		fileSystem.Received(1).SerializeToJsonFile(
			Arg.Is<EditorWindowSettings>(x => x.IsReadOnly),
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: a maximized window is saved as maximized when it closes.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Saves_The_State_Of_A_Maximized_Window_When_It_Closes()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		// The editor lives as long as the test, as the window saves it when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

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

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		EditorWindow window = sut.CreateEditorWindow();

		window.WindowState = WindowState.Maximized;

		// Act
		window.Close();

		// Assert
		fileSystem.Received(1).SerializeToJsonFile(
			Arg.Is<EditorWindowSettings>(x => x.WindowState == WindowShowState.Maximized),
			Arg.Any<string>(),
			Arg.Any<bool>());
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
		using AutoMock windowMock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			windowMock
				.Create<ISessionHierarchy>()
				.Hierarchy
				.Add(selected);

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
		EditorWindow window = sut.CreateEditorWindow(Guid.NewGuid());

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
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			windowMock
				.Create<ISessionHierarchy>()
				.Hierarchy
				.Add(selected);

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
		EditorWindow window = sut.CreateEditorWindow();

		// Assert
		window.ViewModel.SelectedObject
			.Should()
			.BeSameAs(selected);

		await propertyWriter.DidNotReceive().UpdateIsSelectedAsync(
			Arg.Any<ExplorerItemDtoBase>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: the editor lists the files the operating system runs.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Shows_The_Executing_Files()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder =>
			{
				IExecutionEngine engine = Substitute.For<IExecutionEngine>();

				engine
					.ExecuteAsync(Arg.Any<ExecuteFileParameters>(), Arg.Any<CancellationToken>())
					.Returns(true);

				windowBuilder.RegisterInstance(engine);

				windowBuilder
					.RegisterType<ExecutingFiles>()
					.As<IExecutingFiles>()
					.SingleInstance();

				windowBuilder
					.RegisterType<WorkSession>()
					.AsImplementedInterfaces()
					.SingleInstance();
			});

			// The file runs before the editor opens, as it does when the favorites switch back to the editor.
			_ = windowMock
				.Create<IExecutingFiles>()
				.ExecuteAsync(new ExecuteFileParameters
				{
					Contents = [],
					File = file,
					IsReadOnly = false
				});

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
		EditorWindow window = sut.CreateEditorWindow();

		// Assert
		window.ViewModel.ExecutingFiles
			.Should()
			.Equal(file);
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateEditorWindow" />: the editor opens in the read-only mode of the saved settings.
	/// </summary>
	[AvaloniaTest]
	public void CreateEditorWindow_Takes_The_Read_Only_Mode_From_Saved_Settings()
	{
		// Arrange
		EditorWindowSettings settings = new()
		{
			IsReadOnly = true,
			IsTopmost = false,
			NavigationColumnWidth = 200.0,
			Size = new(800, 600),
			WindowState = WindowShowState.Normal,
			X = 100,
			Y = 100
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		EditorWindow window = sut.CreateEditorWindow();

		// Assert
		window.ViewModel.IsReadOnly
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: the close button of the window, which closes it by code, asks
	/// for the exit of the application.
	/// </summary>
	[AvaloniaTest]
	public void CreateFavoritesWindow_Asks_For_The_Exit_When_The_Close_Button_Is_Clicked()
	{
		// Arrange
		bool isClosed = false;

		IMessenger messenger = new StrongReferenceMessenger();

		ExitAppMessage? sent = null;

		messenger.Register<ExitAppMessage>(this, (_, message) => sent = message);

		// The favorites live as long as the test, as the window saves them when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		FavoritesWindow window = sut.CreateFavoritesWindow();

		window.Closed += (_, _) => isClosed = true;

		window.Show();

		Dispatcher.UIThread.RunJobs();

		Button closeButton = FindCloseButton(window);

		// Act
		closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

		// Assert
		isClosed
			.Should()
			.BeTrue();

		sent
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: a window the user closes asks for the exit of the application.
	/// </summary>
	[AvaloniaTest]
	public void CreateFavoritesWindow_Asks_For_The_Exit_When_The_User_Closes_The_Window()
	{
		// Arrange
		IMessenger messenger = new StrongReferenceMessenger();

		ExitAppMessage? sent = null;

		messenger.Register<ExitAppMessage>(this, (_, message) => sent = message);

		// The favorites live as long as the test, as the window saves them when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		FavoritesWindow window = sut.CreateFavoritesWindow();

		// The window is closed the way the system closes it for the user, on Alt+F4 or from the taskbar.
		Func<WindowCloseReason, bool> closeByUser = (Func<WindowCloseReason, bool>)typeof(IWindowImpl)
			.GetProperty("Closing")!
			.GetValue(window.PlatformImpl)!;

		// Act
		closeByUser(WindowCloseReason.WindowClosing);

		// Assert
		sent
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: a window saved off every screen opens in the center of the
	/// screen.
	/// </summary>
	[AvaloniaTest]
	public void CreateFavoritesWindow_Centers_A_Window_Saved_Off_The_Screens()
	{
		// Arrange
		FavoritesWindowSettings settings = new()
		{
			PopupHeight = 250.0,
			PopupWidth = 500.0,
			X = 50000,
			Y = 50000
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		FavoritesWindow window = sut.CreateFavoritesWindow();

		// Assert
		window.WindowStartupLocation
			.Should()
			.Be(WindowStartupLocation.CenterScreen);
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
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		FavoritesWindow window = sut.CreateFavoritesWindow();

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
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		FavoritesWindow window = sut.CreateFavoritesWindow();

		// Assert
		window.ViewModel.IsInitialized
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: a switch to the editor closes the favorites without asking for the
	/// exit of the application.
	/// </summary>
	[AvaloniaTest]
	public async Task CreateFavoritesWindow_Keeps_The_Application_Running_After_A_Switch_To_The_Editor()
	{
		// Arrange
		EditorWindow editorWindow = new();

		IMessenger messenger = new StrongReferenceMessenger();

		ExitAppMessage? sent = null;

		messenger.Register<ExitAppMessage>(this, (_, message) => sent = message);

		// The favorites live as long as the test, as the window saves them when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

			mainWindowLauncher
				.CreateEditorWindow()
				.Returns(editorWindow);

			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		FavoritesWindow window = sut.CreateFavoritesWindow();

		// Act
		await window
			.ViewModel
			.ShowInEditorAsync(default, window);

		// Closed before the assertions: its dialog host would stay in the list every headless test shares.
		editorWindow.Close();

		// Assert
		sent
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: the window takes the position saved for it.
	/// </summary>
	[AvaloniaTest]
	public void CreateFavoritesWindow_Places_The_Window_From_Saved_Settings()
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
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		FavoritesWindow window = sut.CreateFavoritesWindow();

		// Assert
		window.Position
			.Should()
			.Be(new PixelPoint(settings.X, settings.Y));
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: a window saved at zero or negative coordinates, as at the edge
	/// of the screen or on a screen left of the main one, comes back there.
	/// </summary>
	[AvaloniaTest]
	[TestCase(0, 0)]
	[TestCase(-50, 100)]
	public void CreateFavoritesWindow_Restores_A_Window_Saved_At_Zero_Or_Negative_Coordinates(int x, int y)
	{
		// Arrange
		FavoritesWindowSettings settings = new()
		{
			PopupHeight = 250.0,
			PopupWidth = 500.0,
			X = x,
			Y = y
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			// The window stands elsewhere, so only the restore brings it to the stored position.
			favoritesWindow.Position = new PixelPoint(200, 150);

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
		FavoritesWindow window = sut.CreateFavoritesWindow();

		// Assert
		window.Position
			.Should()
			.Be(new PixelPoint(x, y));
	}

	/// <summary>
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: closing the window clears the favorites collections and
	/// serializes the settings with the current window kind to JSON files.
	/// </summary>
	[AvaloniaTest]
	public void CreateFavoritesWindow_Saves_Settings_When_The_Window_Closes()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		// The favorites live as long as the test, as the window saves them when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

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

		FavoritesWindow window = sut.CreateFavoritesWindow();

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
		window.Close();

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
			IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

			mainWindowLauncher
				.CreateEditorWindow()
				.Returns(editorWindow);

			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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

		FavoritesWindow window = sut.CreateFavoritesWindow();

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

		SelectedFavoritesViewModel favorites = window.ViewModel.SelectedFavorites!;

		// The stored selection comes back through the dispatcher before the choice.
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
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

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

		FavoritesWindow window = sut.CreateFavoritesWindow();

		window
			.ViewModel
			.FavoritesSettings
			.Categories
			.AddRange(categories);

		window.ViewModel.IsPopupFixed = isPopupFixed;

		window
			.ViewModel
			.ShowFavorites();

		SelectedFavoritesViewModel favorites = window.ViewModel.SelectedFavorites!;

		// The stored selection comes back through the dispatcher before the choice.
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
	/// <see cref="ViewLauncher.CreateFavoritesWindow" />: a switch to the editor saves the copy history with the item chosen
	/// in its list.
	/// </summary>
	[AvaloniaTest]
	public async Task CreateFavoritesWindow_Saves_The_Chosen_Copy_History_Item_When_Switching_To_The_Editor()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		EditorWindow editorWindow = new();

		Guid savedItemId = default;

		// The favorites live as long as the test, as the window saves them when it closes.
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<CopyHistoryViewSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => savedItemId = call.Arg<CopyHistoryViewSettings>().SelectedItemId);

			mainWindowLauncher
				.CreateEditorWindow()
				.Returns(editorWindow);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			windowMock
				.Create<ISessionHierarchy>()
				.Hierarchy
				.AddRange(files);

			FavoritesViewModel viewModel = windowMock.Create<FavoritesViewModel>();

			FavoritesWindow favoritesWindow = windowMock.Create<FavoritesWindow>(TypedParameter.From(viewModel));

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<FavoritesViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<FavoritesWindow>(Arg.Any<object[]>())
				.Returns(favoritesWindow);

			builder.RegisterInstance<ICopyHistoryStore>(windowMock.Create<CopyHistory>());

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		FavoritesWindow window = sut.CreateFavoritesWindow();

		CopyHistory copyHistory = windowMock.Create<CopyHistory>();

		copyHistory.Add(files[1], showAtOnce: false);

		copyHistory.Add(files[0], showAtOnce: false);

		CopyHistoryViewModel copyHistoryList = windowMock.Create<CopyHistoryViewModel>();

		// The stored selection comes back through the dispatcher before the choice.
		Dispatcher.UIThread.RunJobs();

		copyHistoryList.SelectedItem = files[1];

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
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		Window window = sut.CreateMainWindow();

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
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		Window window = sut.CreateMainWindow();

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
			using AutoMock windowMock = AutoMock.GetLoose(windowBuilder => windowBuilder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance());

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
		Window window = sut.CreateMainWindow();

		// Assert
		window
			.Should()
			.BeOfType<FavoritesWindow>();
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowClipboardLogWindowAsync" />: saved size and position settings are applied to the window.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowClipboardLogWindowAsync_Applies_Saved_Settings()
	{
		// Arrange
		int positiveValue = RandomValues.CreateInt(330, 500);

		ClipboardLogWindowSettings settings = new()
		{
			ActiveFilter = ClipboardLogEntryFilter.Image,
			KeepOpen = true,
			Size = new(positiveValue, positiveValue),
			X = 10,
			Y = 10
		};

		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

			clipboardLogService
				.Entries
				.Returns([]);

			builder.RegisterInstance(clipboardLogService);
		});

		ClipboardLogWindow window = windowMock.Create<ClipboardLogWindow>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<ClipboardLogWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<ClipboardLogViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<ClipboardLogWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		await sut.ShowClipboardLogWindowAsync(new Window());

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
	/// <see cref="ViewLauncher.ShowClipboardLogWindowAsync" />: a window saved without a size keeps the size its markup
	/// gives it.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowClipboardLogWindowAsync_Keeps_Its_Own_Size_Without_A_Saved_One()
	{
		// Arrange
		ClipboardLogWindow markup = new();

		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

			clipboardLogService
				.Entries
				.Returns([]);

			builder.RegisterInstance(clipboardLogService);
		});

		ClipboardLogWindow window = windowMock.Create<ClipboardLogWindow>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ClipboardLogWindowSettings settings = new()
			{
				Size = new(0, 0),
				X = 10,
				Y = 10
			};

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<ClipboardLogWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<ClipboardLogViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<ClipboardLogWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		await sut.ShowClipboardLogWindowAsync(new Window());

		// Assert
		window.Width
			.Should()
			.Be(markup.Width);

		window.Height
			.Should()
			.Be(markup.Height);
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowClipboardLogWindowAsync" />: without saved settings the window opens in the lower right
	/// corner of the screen of the owner.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowClipboardLogWindowAsync_Opens_In_The_Corner_For_The_First_Launch()
	{
		// Arrange
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

			clipboardLogService
				.Entries
				.Returns([]);

			builder.RegisterInstance(clipboardLogService);
		});

		ClipboardLogWindow window = windowMock.Create<ClipboardLogWindow>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<ClipboardLogViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<ClipboardLogWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		Window owner = new();

		// Act
		await sut.ShowClipboardLogWindowAsync(owner);

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
	/// <see cref="ViewLauncher.ShowClipboardLogWindowAsync" />: a window saved off every screen opens in the lower right
	/// corner of the screen of the owner.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowClipboardLogWindowAsync_Places_A_Window_Saved_Off_The_Screens_In_The_Corner()
	{
		// Arrange
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

			clipboardLogService
				.Entries
				.Returns([]);

			builder.RegisterInstance(clipboardLogService);
		});

		ClipboardLogWindow window = windowMock.Create<ClipboardLogWindow>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ClipboardLogWindowSettings settings = new()
			{
				Size = new(400, 300),
				X = 50000,
				Y = 50000
			};

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<ClipboardLogWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<ClipboardLogViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<ClipboardLogWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		Window owner = new();

		// Act
		await sut.ShowClipboardLogWindowAsync(owner);

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
	/// <see cref="ViewLauncher.ShowClipboardLogWindowAsync" />: closing the window serializes its settings to a JSON file.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowClipboardLogWindowAsync_Saves_Settings_When_The_Window_Closes()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

			clipboardLogService
				.Entries
				.Returns([]);

			builder.RegisterInstance(clipboardLogService);
		});

		ClipboardLogWindow window = windowMock.Create<ClipboardLogWindow>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<ClipboardLogViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<ClipboardLogWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		await sut.ShowClipboardLogWindowAsync(new Window());

		// Act
		window.Close();

		// Assert
		fileSystem.Received(1).SerializeToJsonFile(
			Arg.Any<ClipboardLogWindowSettings>(),
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowClipboardLogWindowAsync" />: closing the window saves its keep-open flag.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowClipboardLogWindowAsync_Saves_The_Keep_Open_Flag_When_The_Window_Closes()
	{
		// Arrange
		ClipboardLogWindowSettings? captured = null;

		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

			clipboardLogService
				.Entries
				.Returns([]);

			builder.RegisterInstance(clipboardLogService);
		});

		ClipboardLogWindow window = windowMock.Create<ClipboardLogWindow>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<ClipboardLogWindowSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => captured = call.Arg<ClipboardLogWindowSettings>());

			viewFactory
				.CreateViewModel<ClipboardLogViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<ClipboardLogWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		await sut.ShowClipboardLogWindowAsync(new Window());

		window.ViewModel.KeepOpen = true;

		// Act
		window.Close();

		// Assert
		captured
			.Should()
			.NotBeNull();

		captured.KeepOpen
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowClipboardLogWindowAsync" />: closing the window saves its type filter.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowClipboardLogWindowAsync_Saves_The_Type_Filter_When_The_Window_Closes()
	{
		// Arrange
		ClipboardLogWindowSettings? captured = null;

		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogService clipboardLogService = Substitute.For<IClipboardLogService>();

			clipboardLogService
				.Entries
				.Returns([]);

			builder.RegisterInstance(clipboardLogService);
		});

		ClipboardLogWindow window = windowMock.Create<ClipboardLogWindow>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<ClipboardLogWindowSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => captured = call.Arg<ClipboardLogWindowSettings>());

			viewFactory
				.CreateViewModel<ClipboardLogViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<ClipboardLogWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		await sut.ShowClipboardLogWindowAsync(new Window());

		window.ViewModel.ActiveFilter = ClipboardLogEntryFilter.Image;

		// Act
		window.Close();

		// Assert
		captured
			.Should()
			.NotBeNull();

		captured.ActiveFilter
			.Should()
			.Be(ClipboardLogEntryFilter.Image);
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowFavorites" />: the favorites window that is the main window opens its favorites in the
	/// popup.
	/// </summary>
	[AvaloniaTest]
	public void ShowFavorites_Opens_The_Favorites_Of_The_Favorites_Window()
	{
		// Arrange
		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesWindow window = windowMock.Create<FavoritesWindow>(TypedParameter.From(windowMock.Create<FavoritesViewModel>()));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([window]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		window.Show();

		// Act
		sut.ShowFavorites();

		FavoritesPopupContentKind popupContent = window.ViewModel.PopupContent;

		// Closed before the assertion: its dialog host would stay in the list every headless test shares.
		window.Close();

		// Assert
		popupContent
			.Should()
			.Be(FavoritesPopupContentKind.Favorites);
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowInEditor" />: the favorites window that is the main window gives way to an editor that
	/// shows the object.
	/// </summary>
	[AvaloniaTest]
	public void ShowInEditor_Switches_The_Favorites_Window_To_An_Editor_With_The_Object()
	{
		// Arrange
		Guid id = Guid.NewGuid();

		bool isClosed = false;

		EditorWindow editor = new();

		using AutoMock windowMock = AutoMock.GetLoose(builder =>
		{
			IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

			mainWindowLauncher
				.CreateEditorWindow(id)
				.Returns(editor);

			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesWindow window = windowMock.Create<FavoritesWindow>(TypedParameter.From(windowMock.Create<FavoritesViewModel>()));

		window.Closed += (_, _) => isClosed = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([window]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		window.Show();

		// Act
		sut.ShowInEditor(id);

		bool isEditorShown = editor.IsVisible;

		// Closed before the assertions: its dialog host would stay in the list every headless test shares.
		editor.Close();

		// Assert
		isClosed
			.Should()
			.BeTrue();

		isEditorShown
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowNotepadWindow" />: the saved size, position and topmost flag are applied to the window.
	/// </summary>
	[AvaloniaTest]
	public void ShowNotepadWindow_Applies_Saved_Settings()
	{
		// Arrange
		NotepadWindow window = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
			TimeProvider.System));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			NotepadWindowSettings settings = new()
			{
				IsTopmost = true,
				Size = new(500, 400),
				WindowState = WindowShowState.Normal,
				X = 30,
				Y = 40
			};

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<NotepadWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		sut.ShowNotepadWindow(new Window());

		PixelPoint position = window.Position;

		// Closed before the assertions: a closed notepad takes its dialog host out of the list every headless test shares.
		window.Close();

		// Assert
		window.Width
			.Should()
			.Be(500.0);

		window.Height
			.Should()
			.Be(400.0);

		position
			.Should()
			.Be(new PixelPoint(30, 40));

		window.Topmost
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowNotepadWindow" />: a window saved off every screen opens in the center of the screen of
	/// the owner.
	/// </summary>
	[AvaloniaTest]
	public void ShowNotepadWindow_Centers_A_Window_Saved_Off_The_Screens()
	{
		// Arrange
		NotepadWindow window = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
			TimeProvider.System));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			NotepadWindowSettings settings = new()
			{
				IsTopmost = false,
				Size = new(500, 400),
				WindowState = WindowShowState.Normal,
				X = -20000,
				Y = -20000
			};

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<NotepadWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		Window owner = new();

		// Act
		sut.ShowNotepadWindow(owner);

		PixelRect bounds = new(window.Position, new PixelSize(500, 400));

		// Closed before the assertion: a closed notepad takes its dialog host out of the list every headless test shares.
		window.Close();

		// Assert
		bounds.Center
			.Should()
			.Be(owner.Screens!.Primary!.WorkingArea.Center);
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowNotepadWindow" />: the view model of the window is disposed when the window closes, so
	/// the editors of its tabs go with it.
	/// </summary>
	[AvaloniaTest]
	public void ShowNotepadWindow_Disposes_The_View_Model_When_The_Window_Closes()
	{
		// Arrange
		NotepadWindow window = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
			TimeProvider.System));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		sut.ShowNotepadWindow(new Window());

		// Act
		window.Close();

		// Assert
		window.ViewModel.IsDisposed
			.Should()
			.BeTrue();
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
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
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
	/// <see cref="ViewLauncher.ShowNotepadWindow" />: without saved settings the window takes the default size and opens
	/// in the center of the screen of the owner.
	/// </summary>
	[AvaloniaTest]
	public void ShowNotepadWindow_Opens_Centered_With_The_Default_Size_For_The_First_Launch()
	{
		// Arrange
		NotepadWindow window = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
			TimeProvider.System));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		Window owner = new();

		// Act
		sut.ShowNotepadWindow(owner);

		PixelRect bounds = new(
			window.Position,
			new PixelSize(WindowPlacementHelper.DefaultWindowSize.Width, WindowPlacementHelper.DefaultWindowSize.Height));

		// Closed before the assertions: a closed notepad takes its dialog host out of the list every headless test shares.
		window.Close();

		// Assert
		window.Width
			.Should()
			.Be(WindowPlacementHelper.DefaultWindowSize.Width);

		window.Height
			.Should()
			.Be(WindowPlacementHelper.DefaultWindowSize.Height);

		bounds.Center
			.Should()
			.Be(owner.Screens!.Primary!.WorkingArea.Center);
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowNotepadWindow" />: the window opens the saved tabs and selects the saved one.
	/// </summary>
	[AvaloniaTest]
	public void ShowNotepadWindow_Opens_The_Saved_Tabs()
	{
		// Arrange
		NotepadWindow? window = null;

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
				Substitute.For<IDocumentEncodingSwitcher>(),
				Substitute.For<IMainWindowLauncher>(),
				Substitute.For<INotepadSessionState>(),
				store,
				Substitute.For<INotificationService>(),
				Substitute.For<ISyntaxRegistry>(),
				Substitute.For<ITaskExceptionHandler>(),
				Substitute.For<IViewCache>(),
				TimeProvider.System);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(viewModel);

			// Created on the call, as by the real factory, so the window binds to the tabs restored before it.
			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(_ => window = new NotepadWindow(viewModel));

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		sut.ShowNotepadWindow(new Window());

		object? selected = window!.Tabs.SelectedItem;

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
	/// <see cref="ViewLauncher.ShowNotepadWindow" />: the tabs of the window get their texts from the disk.
	/// </summary>
	[AvaloniaTest]
	public void ShowNotepadWindow_Reads_The_Texts_Of_The_Tabs()
	{
		// Arrange
		NotepadWindow? window = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INotepadStore store = Substitute.For<INotepadStore>();

			store
				.Read(1)
				.Returns(Encoding.UTF8.GetBytes("Text"));

			NotepadViewModel viewModel = new(
				Substitute.For<IDialogService>(),
				Substitute.For<IDispatcherAccessor>(),
				Substitute.For<IDocumentEncodingSwitcher>(),
				Substitute.For<IMainWindowLauncher>(),
				Substitute.For<INotepadSessionState>(),
				store,
				Substitute.For<INotificationService>(),
				Substitute.For<ISyntaxRegistry>(),
				Substitute.For<ITaskExceptionHandler>(),
				Substitute.For<IViewCache>(),
				TimeProvider.System);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(_ => window = new NotepadWindow(viewModel));

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		sut.ShowNotepadWindow(new Window());

		string text = window!.ViewModel.Tabs[0].Editor.Document.Text;

		// Closed before the assertion: a closed notepad takes its dialog host out of the list every headless test shares.
		window.Close();

		// Assert
		text
			.Should()
			.Be("Text");
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
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
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
	/// <see cref="ViewLauncher.ShowNotepadWindow" />: the window opens in the saved state, a minimized one as a normal one.
	/// </summary>
	[AvaloniaTest]
	[TestCase(WindowShowState.Maximized, WindowState.Maximized)]
	[TestCase(WindowShowState.Minimized, WindowState.Normal)]
	public void ShowNotepadWindow_Restores_The_Saved_State(WindowShowState saved, WindowState expected)
	{
		// Arrange
		NotepadWindow window = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
			TimeProvider.System));

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

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<NotepadWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		sut.ShowNotepadWindow(new Window());

		WindowState state = window.WindowState;

		// Closed before the assertion: a closed notepad takes its dialog host out of the list every headless test shares.
		window.Close();

		// Assert
		state
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowNotepadWindow" />: closing the window saves its position, size, state and topmost flag.
	/// </summary>
	[AvaloniaTest]
	public void ShowNotepadWindow_Saves_The_Placement_When_The_Window_Closes()
	{
		// Arrange
		NotepadWindowSettings? captured = null;

		NotepadWindow window = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
			TimeProvider.System));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<NotepadWindowSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => captured = call.Arg<NotepadWindowSettings>());

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		sut.ShowNotepadWindow(new Window());

		window.Height = 400.0;

		window.Position = new PixelPoint(30, 40);

		window.Topmost = true;

		window.Width = 500.0;

		// Act
		window.Close();

		// Assert
		captured
			.Should()
			.BeEquivalentTo(new NotepadWindowSettings
			{
				IsTopmost = true,
				Size = new(500, 400),
				WindowState = WindowShowState.Normal,
				X = 30,
				Y = 40
			});
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowNotepadWindow" />: the window saves its settings when it closes.
	/// </summary>
	[AvaloniaTest]
	public void ShowNotepadWindow_Saves_The_Settings_When_The_Window_Closes()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		NotepadWindow window = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
			TimeProvider.System));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(fileSystem);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		sut.ShowNotepadWindow(new Window());

		// Act
		window.Close();

		// Assert
		fileSystem.Received(1).SerializeToJsonFile(
			Arg.Any<NotepadWindowSettings>(),
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowNotepadWindow" />: a maximized window is saved as maximized when it closes.
	/// </summary>
	[AvaloniaTest]
	public void ShowNotepadWindow_Saves_The_State_Of_A_Maximized_Window_When_It_Closes()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		NotepadWindow window = new(new NotepadViewModel(
			Substitute.For<IDialogService>(),
			Substitute.For<IDispatcherAccessor>(),
			Substitute.For<IDocumentEncodingSwitcher>(),
			Substitute.For<IMainWindowLauncher>(),
			Substitute.For<INotepadSessionState>(),
			Substitute.For<INotepadStore>(),
			Substitute.For<INotificationService>(),
			Substitute.For<ISyntaxRegistry>(),
			Substitute.For<ITaskExceptionHandler>(),
			Substitute.For<IViewCache>(),
			TimeProvider.System));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NotepadViewModel>()
				.Returns(window.ViewModel);

			viewFactory
				.CreateWindow<NotepadWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		sut.ShowNotepadWindow(new Window());

		window.WindowState = WindowState.Maximized;

		// Act
		window.Close();

		// Assert
		fileSystem.Received(1).SerializeToJsonFile(
			Arg.Is<NotepadWindowSettings>(x => x.WindowState == WindowShowState.Maximized),
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ViewLauncher.ShowStartupErrorAsync" />: a topmost notice names the database file, and the report ends
	/// when the notice is closed.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowStartupErrorAsync_Shows_A_Topmost_Notice_Until_It_Is_Closed()
	{
		// Arrange
		const string databaseFilePath = @"C:\Database\DataOrganizer.sqlite";

		NoticeViewModel viewModel = new(Substitute.For<IDirectoryAccessor>());

		NoticeWindow window = new(viewModel);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<NoticeViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<NoticeWindow>(Arg.Any<object[]>())
				.Returns(window);

			builder.RegisterInstance(viewFactory);
		});

		ViewLauncher sut = mock.Create<ViewLauncher>();

		// Act
		Task report = sut.ShowStartupErrorAsync(databaseFilePath);

		bool isWaiting = !report.IsCompleted;

		window.Close();

		await report;

		// Assert
		isWaiting
			.Should()
			.BeTrue();

		viewModel.FilePath
			.Should()
			.Be(databaseFilePath);

		viewModel.IsTopmost
			.Should()
			.BeTrue();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the button of a window that closes the window when clicked.
	/// </summary>
	private static Button FindCloseButton(Window window)
	{
		return window
			.GetLogicalDescendants()
			.OfType<Button>()
			.Single(x => Interaction
				.GetBehaviors(x)
				.OfType<EventTriggerBehavior>()
				.Any(y => y.Actions is { } actions && actions.OfType<CloseWindowAction>().Any()));
	}

	/// <summary>
	/// Returns the item of the side menu of an editor window that closes the window when clicked.
	/// </summary>
	private static MenuItem FindExitMenuItem(EditorWindow window)
	{
		NavigationDrawer drawer = window
			.GetLogicalDescendants()
			.OfType<NavigationDrawer>()
			.Single();

		return ((Panel)drawer.LeftDrawerContent!).Children
			.OfType<Menu>()
			.Single()
			.Items
			.OfType<MenuItem>()
			.Single(x => Interaction
				.GetBehaviors(x)
				.OfType<EventTriggerBehavior>()
				.Any(y => y.Actions is { } actions && actions.OfType<CloseWindowAction>().Any()));
	}

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
