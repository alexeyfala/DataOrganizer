using Autofac;
using Autofac.Extras.Moq;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Services;
using NSubstitute;
using Repository.Interfaces;
using Shared.Interfaces;
using Shared.Properties;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(AppController)}"" type")]
internal class AppControllerTests
{
	#region Methods
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

			viewLauncher.ConfigureMainWindow(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());
		});
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
				.Returns(true);

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
			.LoadFromEmbeddedDbAsync();

		viewLauncher
			.Received()
			.ConfigureMainWindow(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());
	}

	/// <summary>
	/// <see cref="AppController.LaunchAppAsync" />: an unavailable database is reported instead of passing unnoticed.
	/// </summary>
	[Test]
	public async Task LaunchAppAsync_Reports_An_Unavailable_Database()
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
				.Returns(false);

			builder.RegisterInstance(dbAccess);

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

		viewLauncher
			.Received()
			.ConfigureMainWindow(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());
	}
	#endregion
}
