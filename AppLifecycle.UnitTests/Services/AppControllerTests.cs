using AppInstance.Interfaces;
using AppInstance.Messages;
using AppLifecycle.Interfaces;
using AppLifecycle.Services;
using AppUpdates.Interfaces;
using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Clipboard.Interfaces;
using CommunityToolkit.Mvvm.Messaging;
using Diagnostics.Interfaces;
using FileExecution.Dto;
using FileExecution.Interfaces;
using FileExecution.Services;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using Settings.Dto;
using Settings.Interfaces;
using Settings.UnitTests.Factories;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;
using WindowContracts.Interfaces;

namespace AppLifecycle.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(AppController)}"" type")]
internal class AppControllerTests
{
	#region Methods
	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: once the main window shows, the application checks for an update.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Checks_For_An_Update()
	{
		// Arrange
		IUpdateNotifier updateNotifier = Substitute.For<IUpdateNotifier>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IStartupDataLoader startupData = Substitute.For<IStartupDataLoader>();

			// An empty tree read at launch, so that the launch goes on; a faked loader gives none and ends it.
			startupData
				.LoadAsync(Arg.Any<CancellationToken>())
				.Returns(LoadedHierarchy.Empty);

			builder.RegisterInstance(updateNotifier);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder.RegisterInstance(startupData);

			// Moq fakes the file system with no folders behind it, and the launch makes one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		await updateNotifier
			.Received(1)
			.NotifyIfUpdateAvailableAsync(Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: data that cannot be used ends the application before the background
	/// services and the main window start.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Ends_When_The_Data_Cannot_Be_Used()
	{
		// Arrange
		IAppShutdown appShutdown = Substitute.For<IAppShutdown>();

		IBackgroundServices backgroundServices = Substitute.For<IBackgroundServices>();

		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IStartupDataLoader startupData = Substitute.For<IStartupDataLoader>();

			startupData
				.LoadAsync(Arg.Any<CancellationToken>())
				.Returns((LoadedHierarchy?)null);

			builder.RegisterInstance(appShutdown);

			builder.RegisterInstance(backgroundServices);

			builder.RegisterInstance(mainWindowLauncher);

			builder.RegisterInstance(startupData);

			// Moq fakes the file system with no folders behind it, and the launch makes one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		appShutdown
			.Received(1)
			.Shutdown();

		backgroundServices
			.DidNotReceive()
			.Start(Arg.Any<CancellationToken>());

		mainWindowLauncher
			.DidNotReceive()
			.ShowMainWindow();
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the directory the sandbox lives in is there and the sandbox
	/// is swept before a window can open a file again.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Erases_The_Sandbox_Before_The_Main_Window()
	{
		// Arrange
		string appDataFolder = TestPath.Combine("AppData", "DataOrganizer");

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IStartupDataLoader startupData = Substitute.For<IStartupDataLoader>();

			// An empty tree read at launch, so that the launch goes on; a faked loader gives none and ends it.
			startupData
				.LoadAsync(Arg.Any<CancellationToken>())
				.Returns(LoadedHierarchy.Empty);

			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.AppDataDirectoryPath
				.Returns(appDataFolder);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(mainWindowLauncher);

			builder.RegisterInstance(startupData);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		Received.InOrder(() =>
		{
			fileSystem.Directory.CreateDirectory(appDataFolder);

			sandbox.EraseAsync(Arg.Any<CancellationToken>());

			mainWindowLauncher.ShowMainWindow();
		});
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: a data folder whose access rights cannot be changed does not stop the
	/// launch.
	/// </summary>
	[Test]
	[UnsupportedOSPlatform("windows")]
	public async Task LaunchAppAsync_Goes_On_When_The_Data_Folder_Cannot_Be_Restricted()
	{
		// Arrange
		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IStartupDataLoader startupData = Substitute.For<IStartupDataLoader>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			IPlatform platform = Substitute.For<IPlatform>();

			// An empty tree read at launch, so that the launch goes on; a faked loader gives none and ends it.
			startupData
				.LoadAsync(Arg.Any<CancellationToken>())
				.Returns(LoadedHierarchy.Empty);

			fileSystem
				.File
				.When(x => x.SetUnixFileMode(Arg.Any<string>(), Arg.Any<UnixFileMode>()))
				.Throw(new UnauthorizedAccessException());

			// Linux or macOS, where the data folder gets restricted.
			platform
				.IsWindows
				.Returns(false);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(mainWindowLauncher);

			builder.RegisterInstance(platform);

			builder.RegisterInstance(startupData);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		mainWindowLauncher
			.Received(1)
			.ShowMainWindow();
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the clipboard history stays off when the settings do not track it, while
	/// its saving is started.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Leaves_The_Clipboard_History_Off_When_It_Is_Not_Tracked()
	{
		// Arrange
		IClipboardLogRunner clipboardLog = Substitute.For<IClipboardLogRunner>();

		IClipboardLogPersistenceCoordinator clipboardLogPersistence = Substitute.For<IClipboardLogPersistenceCoordinator>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IStartupDataLoader startupData = Substitute.For<IStartupDataLoader>();

			// An empty tree read at launch, so that the launch goes on; a faked loader gives none and ends it.
			startupData
				.LoadAsync(Arg.Any<CancellationToken>())
				.Returns(LoadedHierarchy.Empty);

			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(clipboardLog);

			builder.RegisterInstance(clipboardLogPersistence);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder.RegisterInstance(startupData);

			// Moq fakes the file system with no folders behind it, and the launch makes one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		clipboardLogPersistence
			.Received(1)
			.Start();

		await clipboardLog
			.DidNotReceiveWithAnyArgs()
			.StartAsync();
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the copy history is read for the tree of the work session before the
	/// main window opens.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Loads_The_Copy_History_Of_The_Tree_Before_The_Main_Window()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy = [.. ItemDtoFactory.CreateFolderDtos(3)];

		ExplorerItemDtoBase[]? loaded = null;

		ICopyHistoryStore copyHistory = Substitute.For<ICopyHistoryStore>();

		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IStartupDataLoader startupData = Substitute.For<IStartupDataLoader>();

			copyHistory
				.When(x => x.Load(Arg.Any<IEnumerable<ExplorerItemDtoBase>>()))
				.Do(call => loaded = [.. call.Arg<IEnumerable<ExplorerItemDtoBase>>()]);

			startupData
				.LoadAsync(Arg.Any<CancellationToken>())
				.Returns(ItemDtoFactory.CreateLoadedHierarchy(hierarchy));

			builder.RegisterInstance(copyHistory);

			builder.RegisterInstance(mainWindowLauncher);

			builder.RegisterInstance(startupData);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			// Moq fakes the file system with no folders behind it, and the launch makes one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		loaded
			.Should()
			.Equal(hierarchy);

		Received.InOrder(() =>
		{
			copyHistory.Load(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());

			mainWindowLauncher.ShowMainWindow();
		});
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: on Linux and macOS only the owner may enter the data folder.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Off_Windows_Restricts_The_Data_Folder_To_Its_Owner()
	{
		// Arrange
		string appDataFolder = TestPath.Combine("AppData", "DataOrganizer");

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			IPlatform platform = Substitute.For<IPlatform>();

			appEnvironment
				.AppDataDirectoryPath
				.Returns(appDataFolder);

			// Linux or macOS.
			platform
				.IsWindows
				.Returns(false);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(platform);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		// The call is found by its name: the member is not for Windows, while the test runs on every system.
		fileSystem.File.ReceivedCalls()
			.Should()
			.ContainSingle(static x => x.GetMethodInfo().Name == nameof(IFile.SetUnixFileMode))
			.Which
			.GetArguments()
			.Should()
			.Equal(
				appDataFolder,
				UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: Windows leaves the data folder to the access rights it already has.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_On_Windows_Leaves_The_Data_Folder_Rights_Alone()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPlatform platform = Substitute.For<IPlatform>();

			platform
				.IsWindows
				.Returns(true);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(platform);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		fileSystem.File.ReceivedCalls()
			.Should()
			.NotContain(static x => x.GetMethodInfo().Name == nameof(IFile.SetUnixFileMode));
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the folders stored expanded and the object stored selected are in the
	/// work session before the main window shows.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Puts_The_Stored_Tree_State_Into_The_Session_Before_The_Main_Window()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		bool[]? shown = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

			IStartupDataLoader startupData = Substitute.For<IStartupDataLoader>();

			WorkSession session = new();

			// The state the tree finds in the session when the window opens.
			mainWindowLauncher
				.When(x => x.ShowMainWindow())
				.Do(_ => shown =
				[
					session.IsExpanded(folder.Id),
					session.IsSelected(file.Id),
					session.IsExpanded(file.Id),
					session.IsSelected(folder.Id)
				]);

			startupData
				.LoadAsync(Arg.Any<CancellationToken>())
				.Returns(ItemDtoFactory.CreateLoadedHierarchy(
					[folder, file],
					expandedIds: [folder.Id],
					selectedIds: [file.Id]));

			builder.RegisterInstance(mainWindowLauncher);

			builder.RegisterInstance(startupData);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			// Moq fakes the file system with no folders behind it, and the launch makes one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		shown
			.Should()
			.Equal(true, true, false, false);
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the tree read at launch is in the work session before the main window
	/// shows.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Puts_The_Tree_Into_The_Session_Before_The_Main_Window()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy = [.. ItemDtoFactory.CreateFolderDtos(3)];

		ExplorerItemDtoBase[]? shown = null;

		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IStartupDataLoader startupData = Substitute.For<IStartupDataLoader>();

			WorkSession session = new();

			// The tree the window finds in the session when it opens.
			mainWindowLauncher
				.When(x => x.ShowMainWindow())
				.Do(_ => shown = [.. session.Hierarchy]);

			startupData
				.LoadAsync(Arg.Any<CancellationToken>())
				.Returns(ItemDtoFactory.CreateLoadedHierarchy(hierarchy));

			builder.RegisterInstance(mainWindowLauncher);

			builder.RegisterInstance(startupData);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			// Moq fakes the file system with no folders behind it, and the launch makes one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		shown
			.Should()
			.Equal(hierarchy);

		mainWindowLauncher
			.Received(1)
			.ShowMainWindow();
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the clipboard history and its saving start when the settings track it.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Starts_The_Clipboard_History_When_It_Is_Tracked()
	{
		// Arrange
		IClipboardLogRunner clipboardLog = Substitute.For<IClipboardLogRunner>();

		IClipboardLogPersistenceCoordinator clipboardLogPersistence = Substitute.For<IClipboardLogPersistenceCoordinator>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IStartupDataLoader startupData = Substitute.For<IStartupDataLoader>();

			// An empty tree read at launch, so that the launch goes on; a faked loader gives none and ends it.
			startupData
				.LoadAsync(Arg.Any<CancellationToken>())
				.Returns(LoadedHierarchy.Empty);

			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			AppSettings settings = SettingsFactory.CreateSettings();

			settings.TrackClipboardHistory = true;

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(clipboardLog);

			builder.RegisterInstance(clipboardLogPersistence);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder.RegisterInstance(startupData);

			// Moq fakes the file system with no folders behind it, and the launch makes one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		clipboardLogPersistence
			.Received(1)
			.Start();

		await clipboardLog
			.Received(1)
			.StartAsync(Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the launch starts watching the unhandled exceptions and forwarding the
	/// log of Avalonia.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Starts_The_Exception_Monitoring_And_The_Log_Forwarding()
	{
		// Arrange
		IAvaloniaLogForwarder avaloniaLogForwarder = Substitute.For<IAvaloniaLogForwarder>();

		IGlobalExceptionHandler globalExceptionHandler = Substitute.For<IGlobalExceptionHandler>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(avaloniaLogForwarder);

			builder.RegisterInstance(globalExceptionHandler);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		globalExceptionHandler
			.Received(1)
			.StartMonitoring();

		avaloniaLogForwarder
			.Received(1)
			.StartForwarding();
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the startup facts go to the log without the console too.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Writes_The_Startup_Info()
	{
		// Arrange
		IStartupInfoLogger startupInfo = Substitute.For<IStartupInfoLogger>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(startupInfo);

			// Moq fakes the file system with no folders behind it, and the launch makes one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		startupInfo
			.Received(1)
			.Log();
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the startup facts go to the log once the console shows, and before the
	/// data is read.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Writes_The_Startup_Info_After_The_Console_Shows()
	{
		// Arrange
		IConsoleWindowHost consoleWindowHost = Substitute.For<IConsoleWindowHost>();

		IStartupDataLoader startupData = Substitute.For<IStartupDataLoader>();

		IStartupInfoLogger startupInfo = Substitute.For<IStartupInfoLogger>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICommandLineOptions options = Substitute.For<ICommandLineOptions>();

			options
				.ShowConsole
				.Returns(true);

			builder.RegisterInstance(consoleWindowHost);

			builder.RegisterInstance(options);

			builder.RegisterInstance(startupData);

			builder.RegisterInstance(startupInfo);

			// Moq fakes the file system with no folders behind it, and the launch makes one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		Received.InOrder(() =>
		{
			consoleWindowHost.ConfigureAndShowAsync();

			startupInfo.Log();

			startupData.LoadAsync(Arg.Any<CancellationToken>());
		});
	}

	/// <summary>
	/// <see cref="AppController.Receive(ExitAppMessage)" />: the exit closes the files the operating system runs.
	/// </summary>
	[Test]
	public async Task Receive_ExitApp_Closes_The_Executing_Files()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		IMessenger messenger = new StrongReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			engine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(engine);

			builder.RegisterInstance(messenger);

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();
		});

		await mock
			.Create<IExecutingFiles>()
			.ExecuteAsync(new()
			{
				Contents = [],
				File = file,
				IsReadOnly = false
			});

		_ = mock.Create<AppController>();

		// Act
		messenger.Send(new ExitAppMessage());

		// Assert
		await engine
			.Received(1)
			.CloseAsync(file.Id, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="AppController.Receive(ExitAppMessage)" />: the exit disposes the services.
	/// </summary>
	[Test]
	public async Task Receive_ExitApp_Disposes_The_Services()
	{
		// Arrange
		IMessenger messenger = new StrongReferenceMessenger();

		IServiceProvider serviceProvider = Substitute.For<IServiceProvider, IAsyncDisposable>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(messenger);

			builder.RegisterInstance(serviceProvider);
		});

		_ = mock.Create<AppController>();

		// Act
		messenger.Send(new ExitAppMessage());

		// Assert
		await ((IAsyncDisposable)serviceProvider)
			.Received(1)
			.DisposeAsync();
	}

	/// <summary>
	/// <see cref="AppController.Receive(ExitAppMessage)" />: without a restart asked for, the exit starts nothing.
	/// </summary>
	[Test]
	public void Receive_ExitApp_Starts_Nothing_Without_A_Restart()
	{
		// Arrange
		IMessenger messenger = new StrongReferenceMessenger();

		IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(messenger);

			builder.RegisterInstance(processLauncher);
		});

		_ = mock.Create<AppController>();

		// Act
		messenger.Send(new ExitAppMessage());

		// Assert
		processLauncher
			.DidNotReceive()
			.StartProcess(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());
	}

	/// <summary>
	/// <see cref="AppController.Receive(ExitAppMessage)" />: once a restart is asked for, the exit starts the application
	/// again after the session is released, and only then ends this one.
	/// </summary>
	[Test]
	public void Receive_ExitApp_Starts_The_Application_Again_After_The_Session_When_Asked()
	{
		// Arrange
		IAppShutdown appShutdown = Substitute.For<IAppShutdown>();

		IMessenger messenger = new StrongReferenceMessenger();

		IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

		IServiceProvider serviceProvider = Substitute.For<IServiceProvider, IAsyncDisposable>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppProcess appProcess = Substitute.For<IAppProcess>();

			appProcess
				.ProcessPath
				.Returns(TestPath.Combine("Apps", "DataOrganizer"));

			builder.RegisterInstance(appProcess);

			builder.RegisterInstance(appShutdown);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(processLauncher);

			builder.RegisterInstance(serviceProvider);
		});

		_ = mock.Create<AppController>();

		messenger.Send(new RestartAppMessage());

		// Act
		messenger.Send(new ExitAppMessage());

		// Assert
		processLauncher
			.Received(1)
			.StartProcess(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());

		Received.InOrder(() =>
		{
			// AsTask uses the ValueTask of the call spec (CA2012)
			((IAsyncDisposable)serviceProvider).DisposeAsync().AsTask();

			processLauncher.StartProcess(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());

			appShutdown.Shutdown();
		});
	}
	/// <summary>
	/// <see cref="AppController.Receive(ExitAppMessage)" />: the exit stops the background services, closes the secondary
	/// windows and the executing files and erases the sandbox before the application ends.
	/// </summary>
	[Test]
	public void Receive_ExitApp_Stops_The_Session_Before_The_Application_Ends()
	{
		// Arrange
		IAppShutdown appShutdown = Substitute.For<IAppShutdown>();

		IBackgroundServices backgroundServices = Substitute.For<IBackgroundServices>();

		IExecutingFiles executingFiles = Substitute.For<IExecutingFiles>();

		IMessenger messenger = new StrongReferenceMessenger();

		IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

		ISecondaryWindowCloser secondaryWindowCloser = Substitute.For<ISecondaryWindowCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(appShutdown);

			builder.RegisterInstance(backgroundServices);

			builder.RegisterInstance(executingFiles);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(secondaryWindowCloser);
		});

		_ = mock.Create<AppController>();

		// Act
		messenger.Send(new ExitAppMessage());

		// Assert
		appShutdown
			.Received(1)
			.Shutdown();

		Received.InOrder(() =>
		{
			backgroundServices.StopAsync();

			secondaryWindowCloser.CloseSecondaryWindowsAsync();

			executingFiles.CloseAllAsync();

			sandbox.EraseAsync(Arg.Any<CancellationToken>());

			appShutdown.Shutdown();
		});
	}

	/// <summary>
	/// <see cref="AppController.Receive(ExitAppMessage)" />: a restart under the shared <c>dotnet</c> host starts the host
	/// again with the entry assembly of the application.
	/// </summary>
	[Test]
	public void Receive_ExitApp_Under_The_Dotnet_Host_Starts_The_Entry_Assembly_Again()
	{
		// Arrange
		string dotnetHostPath = TestPath.Combine("dotnet", "dotnet");

		string entryAssemblyPath = TestPath.Combine("Apps", "DataOrganizer.dll");

		IMessenger messenger = new StrongReferenceMessenger();

		IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppProcess appProcess = Substitute.For<IAppProcess>();

			appProcess
				.BaseDirectory
				.Returns(TestPath.Combine("Apps"));

			appProcess
				.EntryAssemblyName
				.Returns("DataOrganizer");

			appProcess
				.ProcessPath
				.Returns(dotnetHostPath);

			builder.RegisterInstance(appProcess);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(processLauncher);
		});

		_ = mock.Create<AppController>();

		messenger.Send(new RestartAppMessage());

		// Act
		messenger.Send(new ExitAppMessage());

		// Assert
		processLauncher
			.Received(1)
			.StartProcess(dotnetHostPath, Arg.Is<IReadOnlyList<string>>(x => x.Count == 1 && x[0] == entryAssemblyPath));
	}
	#endregion
}
