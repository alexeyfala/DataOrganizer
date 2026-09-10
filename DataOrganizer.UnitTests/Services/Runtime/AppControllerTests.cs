using Autofac;
using Autofac.Extras.Moq;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Runtime;
using NSubstitute;
using Repository.Enums;
using Repository.Interfaces.Database;
using Shared.Interfaces;
using Shared.Properties;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestSupport;

namespace DataOrganizer.UnitTests.Services.Runtime;

[TestFixture(Description = $@"Tests of ""{nameof(AppController)}"" type")]
internal class AppControllerTests
{
	#region Methods
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

		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(status);

			dbAccess
				.GetDbFilePath()
				.Returns(databaseFilePath);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(viewLauncher);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		await viewLauncher
			.Received(1)
			.ShowStartupErrorAsync(databaseFilePath);

		await entityLoader
			.DidNotReceive()
			.LoadHierarchyAsync(Arg.Any<CancellationToken>());

		viewLauncher
			.DidNotReceive()
			.CreateMainWindow(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: sweeps the sandbox before a window can open a file again.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Erases_The_Sandbox_Before_The_Main_Window()
	{
		// Arrange
		IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(viewLauncher);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		Received.InOrder(() =>
		{
			sandbox.EraseAsync(Arg.Any<CancellationToken>());

			viewLauncher.CreateMainWindow(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
		});
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: hotkeys that could be read are passed over in silence.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Keeps_Silent_About_Readable_Hotkeys()
	{
		// Arrange
		INotificationService notificationService = Substitute.For<INotificationService>();

		FileDto file = TestData.CreateFileDto();

		file
			.Hotkeys
			.Add(new()
			{
				Code = KeyCode.VcA,
				Id = Guid.NewGuid(),
				Index = 0,
				Mask = EventMask.LeftCtrl,
				OwnerId = file.Id
			});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns([file]);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(notificationService);

			builder.RegisterInstance(settingsStore);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		notificationService
			.DidNotReceive()
			.ShowToast(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: connects to the database, loads entities and configures the main window.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Loads_Entities_From_Database_And_Configures_Main_Window()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		ICommandLineOptions options = Substitute.For<ICommandLineOptions>();

		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			options
				.PrintHelp
				.Returns(true);

			builder.RegisterInstance(options);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewLauncher);

			dbAccess
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(settingsStore);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		fileSystem
			.Received()
			.CreateDirectory(Arg.Any<string>());

		options
			.Received()
			.GetHelp();

		await dbAccess
			.Received()
			.ConnectAsync();

		await entityLoader
			.Received()
			.LoadHierarchyAsync();

		viewLauncher
			.Received()
			.CreateMainWindow(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: an unavailable database is reported instead of passing unnoticed.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Reports_An_Unavailable_Database()
	{
		// Arrange
		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		INotificationService notificationService = Substitute.For<INotificationService>();

		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.FileUnreadable);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(notificationService);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(viewLauncher);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		notificationService
			.Received(1)
			.ShowToast(Strings.DatabaseIsUnavailable);

		// The message is enough: reading a database that is not there would only add a second one.
		await entityLoader
			.DidNotReceive()
			.LoadHierarchyAsync(Arg.Any<CancellationToken>());

		viewLauncher
			.Received()
			.CreateMainWindow(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: data that cannot be read is reported, and the launch
	/// goes on with an empty hierarchy.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Reports_Data_It_Cannot_Read()
	{
		// Arrange
		INotificationService notificationService = Substitute.For<INotificationService>();

		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns((ExplorerItemDtoBase[]?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(notificationService);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(viewLauncher);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		notificationService
			.Received(1)
			.ShowToast(Strings.FailedToReadDatabase);

		viewLauncher
			.Received()
			.CreateMainWindow(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: hotkeys that could not be read are reported by the file they belong to.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Reports_Files_With_Unreadable_Hotkeys()
	{
		// Arrange
		INotificationService notificationService = Substitute.For<INotificationService>();

		FileDto file = TestData.CreateFileDto();

		file
			.Hotkeys
			.Add(new()
			{
				Code = KeyCode.VcUndefined,
				Id = Guid.NewGuid(),
				Index = 0,
				Mask = EventMask.LeftCtrl,
				OwnerId = file.Id
			});

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ConnectAsync(Arg.Any<CancellationToken>())
				.Returns(DbConnectionStatus.Connected);

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns([file]);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(notificationService);

			builder.RegisterInstance(settingsStore);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		notificationService
			.Received(1)
			.ShowToast(Arg.Is<string>(x => x.Contains(file.Name)));
	}
	#endregion
}
