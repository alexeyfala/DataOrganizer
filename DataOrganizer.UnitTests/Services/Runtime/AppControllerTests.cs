using Autofac;
using Autofac.Extras.Moq;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using Repository.Enums;
using Repository.Interfaces.Database;
using Shared.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

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

			builder.RegisterInstance(viewLauncher);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		Received.InOrder(() =>
		{
			fileSystem.CreateDirectory(appDataFolder);

			sandbox.EraseAsync(Arg.Any<CancellationToken>());

			viewLauncher.CreateMainWindow(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
		});
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: the launch goes on with an empty hierarchy when the data cannot be read.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Goes_On_When_Data_Cannot_Be_Read()
	{
		// Arrange
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

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(viewLauncher);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		viewLauncher
			.Received(1)
			.CreateMainWindow(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: connects to the database, loads entities and configures the main window.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Loads_Entities_From_Database_And_Configures_Main_Window()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy = [.. ItemDtoFactory.CreateFolderDtos(3)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(IAppSettingsStore.CreateDefaultSettings());

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns(hierarchy);

			builder.RegisterInstance(entityLoader);

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
		await dbAccess
			.Received(1)
			.ConnectAsync();

		await entityLoader
			.Received()
			.LoadHierarchyAsync();

		// What was read from the database is what the window is given.
		viewLauncher
			.Received(1)
			.CreateMainWindow(hierarchy);
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: an unavailable database leaves the data unread, and the launch goes on.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Skips_Loading_When_The_Database_Is_Unavailable()
	{
		// Arrange
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
				.Returns(DbConnectionStatus.FileUnreadable);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(viewLauncher);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut.LaunchAppAsync();

		// Assert
		await entityLoader
			.DidNotReceive()
			.LoadHierarchyAsync(Arg.Any<CancellationToken>());

		viewLauncher
			.Received(1)
			.CreateMainWindow(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}
	#endregion
}
