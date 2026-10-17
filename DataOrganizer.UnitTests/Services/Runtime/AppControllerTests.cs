using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Updates;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Runtime;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using Repository.Enums;
using Repository.Interfaces.Database;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Runtime;

[TestFixture(Description = $@"Tests of ""{nameof(AppController)}"" type")]
internal class AppControllerTests
{
	#region Methods
	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: once the main window shows, the update check asks through the update
	/// prompt.
	/// </summary>
	[AvaloniaTest]
	public async Task LaunchAppAsync_Checks_For_An_Update_With_The_Update_Prompt()
	{
		// Arrange
		IUpdatePrompt updatePrompt = Substitute.For<IUpdatePrompt>();

		IUpdateNotifier updateNotifier = Substitute.For<IUpdateNotifier>();

		Window mainWindow = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IDbConnector dbConnector = Substitute.For<IDbConnector>();

			IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			mainWindowLauncher
				.CreateMainWindow()
				.Returns(mainWindow);

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(updateNotifier);

			builder.RegisterInstance(updatePrompt);

			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		mainWindow.Close();

		// Assert
		await updateNotifier
			.Received(1)
			.NotifyIfUpdateAvailableAsync(updatePrompt, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: a schema that does not match reports itself and ends the
	/// launch, instead of showing intact data as an empty hierarchy.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Ends_On_A_Schema_That_Does_Not_Match(
		[Values(DbConnectionStatus.SchemaTooOld, DbConnectionStatus.SchemaTooNew)] DbConnectionStatus status)
	{
		// Arrange
		const string databaseFilePath = @"C:\Database\DataOrganizer.sqlite";

		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		INoticeLauncher noticeLauncher = Substitute.For<INoticeLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			IDbConnector dbConnector = Substitute.For<IDbConnector>();

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(status);

			dbConnector
				.GetDbFilePath()
				.Returns(databaseFilePath);

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(mainWindowLauncher);

			builder.RegisterInstance(noticeLauncher);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		await noticeLauncher
			.Received(1)
			.ShowStartupErrorAsync(databaseFilePath);

		await entityLoader
			.DidNotReceive()
			.LoadHierarchyAsync(Arg.Any<CancellationToken>());

		mainWindowLauncher
			.DidNotReceive()
			.CreateMainWindow();
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: with no desktop lifetime to shut down, the process is ended once the
	/// startup error is reported.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Ends_The_Process_After_The_Startup_Error_Without_A_Desktop_Lifetime()
	{
		// Arrange
		IProcessTerminator processTerminator = Substitute.For<IProcessTerminator>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IDbConnector dbConnector = Substitute.For<IDbConnector>();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.SchemaTooNew);

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(processTerminator);

			builder.RegisterInstance(settingsStore);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		processTerminator
			.Received(1)
			.Terminate(Arg.Any<int>());
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the directory the sandbox lives in is there and the sandbox
	/// is swept before a window can open a file again.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Erases_The_Sandbox_Before_The_Main_Window()
	{
		// Arrange
		const string appDataFolder = @"C:\AppData\DataOrganizer";

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			appEnvironment
				.AppDataDirectoryPath
				.Returns(appDataFolder);

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(mainWindowLauncher);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		Received.InOrder(() =>
		{
			fileSystem.CreateDirectory(appDataFolder);

			sandbox.EraseAsync(Arg.Any<CancellationToken>());

			mainWindowLauncher.CreateMainWindow();
		});
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the launch goes on with an empty hierarchy when the data cannot be read.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Goes_On_When_Data_Cannot_Be_Read()
	{
		// Arrange
		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			IDbConnector dbConnector = Substitute.For<IDbConnector>();

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns((ExplorerItemDtoBase[]?)null);

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(mainWindowLauncher);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		mainWindowLauncher
			.Received(1)
			.CreateMainWindow();
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the clipboard history stays off when the settings do not track it, while
	/// its saving is started.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Leaves_The_Clipboard_History_Off_When_It_Is_Not_Tracked()
	{
		// Arrange
		IClipboardLogService clipboardLog = Substitute.For<IClipboardLogService>();

		IClipboardLogPersistenceCoordinator clipboardLogPersistence = Substitute.For<IClipboardLogPersistenceCoordinator>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IDbConnector dbConnector = Substitute.For<IDbConnector>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			builder.RegisterInstance(clipboardLog);

			builder.RegisterInstance(clipboardLogPersistence);

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
	/// <see cref="AppController.LaunchAppAsync" />: connects to the database, loads entities into the work session and
	/// configures the main window.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Loads_Entities_From_Database_And_Configures_Main_Window()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy = [.. ItemDtoFactory.CreateFolderDtos(3)];

		ExplorerItemDtoBase[]? shown = null;

		IDbConnector dbConnector = Substitute.For<IDbConnector>();

		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			WorkSession session = new();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			// The tree the window finds in the session when it opens.
			mainWindowLauncher
				.When(x => x.CreateMainWindow())
				.Do(_ => shown = [.. session.Hierarchy]);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns(hierarchy);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(mainWindowLauncher);

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(settingsStore);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		await dbConnector
			.Received(1)
			.ConnectAsync();

		await entityLoader
			.Received()
			.LoadHierarchyAsync();

		// What was read from the database is in the session before the window opens.
		shown
			.Should()
			.Equal(hierarchy);

		mainWindowLauncher
			.Received(1)
			.CreateMainWindow();
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

		ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IDbConnector dbConnector = Substitute.For<IDbConnector>();

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			copyHistory
				.When(x => x.Load(Arg.Any<IEnumerable<ExplorerItemDtoBase>>()))
				.Do(call => loaded = [.. call.Arg<IEnumerable<ExplorerItemDtoBase>>()]);

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns(hierarchy);

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			builder.RegisterInstance(copyHistory);

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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

			mainWindowLauncher.CreateMainWindow();
		});
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the main window is shown.
	/// </summary>
	[AvaloniaTest]
	public async Task LaunchAppAsync_Shows_The_Main_Window()
	{
		// Arrange
		Window mainWindow = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			mainWindowLauncher
				.CreateMainWindow()
				.Returns(mainWindow);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		bool isVisible = mainWindow.IsVisible;

		mainWindow.Close();

		// Assert
		isVisible
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: an unavailable database leaves the data unread, and the launch goes on.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Skips_Loading_When_The_Database_Is_Unavailable()
	{
		// Arrange
		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			IDbConnector dbConnector = Substitute.For<IDbConnector>();

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.FileUnreadable);

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(mainWindowLauncher);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		await entityLoader
			.DidNotReceive()
			.LoadHierarchyAsync(Arg.Any<CancellationToken>());

		mainWindowLauncher
			.Received(1)
			.CreateMainWindow();
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the clipboard history and its saving start when the settings track it.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Starts_The_Clipboard_History_When_It_Is_Tracked()
	{
		// Arrange
		IClipboardLogService clipboardLog = Substitute.For<IClipboardLogService>();

		IClipboardLogPersistenceCoordinator clipboardLogPersistence = Substitute.For<IClipboardLogPersistenceCoordinator>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IDbConnector dbConnector = Substitute.For<IDbConnector>();

			AppSettings settings = SettingsFactory.CreateSettings();

			settings.TrackClipboardHistory = true;

			settingsStore
				.Settings
				.Returns(settings);

			dbConnector
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			builder.RegisterInstance(clipboardLog);

			builder.RegisterInstance(clipboardLogPersistence);

			builder.RegisterInstance(dbConnector);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
	/// <see cref="AppController.Receive(ExitAppMessage)" />: the exit closes the files the operating system runs.
	/// </summary>
	[Test]
	public void Receive_ExitApp_Closes_The_Executing_Files()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(isExecuting: true);

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		IMessenger messenger = new StrongReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(engine);

			builder.RegisterInstance(messenger);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		mock
			.Create<ISessionHierarchy>()
			.Hierarchy
			.Add(file);

		_ = mock.Create<AppController>();

		// Act
		messenger.Send(new ExitAppMessage());

		// Assert
		engine
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

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
	/// <see cref="AppController.Receive(ExitAppMessage)" />: with no desktop lifetime to shut down, the exit ends the
	/// process.
	/// </summary>
	[Test]
	public void Receive_ExitApp_Ends_The_Process_Without_A_Desktop_Lifetime()
	{
		// Arrange
		IMessenger messenger = new StrongReferenceMessenger();

		IProcessTerminator processTerminator = Substitute.For<IProcessTerminator>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(messenger);

			builder.RegisterInstance(processTerminator);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		_ = mock.Create<AppController>();

		// Act
		messenger.Send(new ExitAppMessage());

		// Assert
		processTerminator
			.Received(1)
			.Terminate(Arg.Any<int>());
	}

	/// <summary>
	/// <see cref="AppController.Receive(ExitAppMessage)" />: the exit stops the background services, closes the secondary
	/// windows and erases the sandbox before the desktop application shuts down.
	/// </summary>
	[Test]
	public void Receive_ExitApp_Stops_The_Session_Before_The_Desktop_Shuts_Down()
	{
		// Arrange
		IBackgroundServices backgroundServices = Substitute.For<IBackgroundServices>();

		IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

		IMessenger messenger = new StrongReferenceMessenger();

		IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

		ISecondaryWindowLauncher secondaryWindowLauncher = Substitute.For<ISecondaryWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			Application app = Substitute.For<Application>();

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();

			builder.RegisterInstance(backgroundServices);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(secondaryWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		_ = mock.Create<AppController>();

		// Act
		messenger.Send(new ExitAppMessage());

		// Assert
		Received.InOrder(() =>
		{
			backgroundServices.StopAsync();

			secondaryWindowLauncher.CloseSecondaryWindowsAsync();

			sandbox.EraseAsync(Arg.Any<CancellationToken>());

			lifetime.Shutdown(Arg.Any<int>());
		});
	}
	#endregion
}
