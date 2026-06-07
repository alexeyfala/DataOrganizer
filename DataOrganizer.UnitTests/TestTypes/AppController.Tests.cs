using Autofac;
using Autofac.Extras.Moq;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using NSubstitute;
using Repository.Interfaces;
using Shared.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(AppController)}"" type")]
internal class AppControllerTests
{
	#region Methods
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
			IAppSettingsManager settingsManager = Substitute.For<IAppSettingsManager>();

			settingsManager
				.Settings
				.Returns(IAppSettingsManager.CreateDefaultSettings());

			options
				.PrintHelp
				.Returns(true);

			builder.RegisterInstance(options);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewLauncher);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(settingsManager);
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
	#endregion
}
