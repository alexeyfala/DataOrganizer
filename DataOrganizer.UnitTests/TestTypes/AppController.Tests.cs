using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.Windows;
using Mapster;
using MapsterMapper;
using NSubstitute;
using Repository.Interfaces;
using Shared.Common;
using Shared.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(AppController)}"" type")]
internal class AppControllerTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="AppController.LaunchAppAsync(ConsoleWindow?, CancellationToken)" />.
	/// </summary>
	[AvaloniaTest]
	public async Task LaunchAppAsync_Loads_Entities_From_Database_And_Configures_Main_Window()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		ICommandLineOptions options = Substitute.For<ICommandLineOptions>();

		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Config
				.Returns(Substitute.For<TypeAdapterConfig>());

			viewLauncher
				.ConfigureMainWindow(Arg.Any<IEnumerable<ExplorerModelBaseDto>>())
				.Returns(Substitute.For<Window>());

			options
				.PrintHelp
				.Returns(true);

			builder.RegisterInstance(options);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(mapper);

			builder.RegisterInstance(viewLauncher);

			builder.RegisterInstance(dbAccess);
		});

		AppController sut = mock.Create<AppController>();

		// Act
		await sut
			.LaunchAppAsync(null)
			.ConfigureAwait(false);

		// Assert
		fileSystem
			.Received()
			.CreateDirectory(AppUtils.AppDataDirectoryPath);

		options
			.Received()
			.GetHelp();

		await dbAccess
			.Received()
			.ConnectAsync(Arg.Any<bool>());

		await dbAccess
			.Received()
			.GetAllFoldersAsync();

		await dbAccess
			.Received()
			.GetAllFilesAsync(excludedProperties: Arg.Any<string[]>());

		viewLauncher
			.Received()
			.ConfigureMainWindow(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());
	}
	#endregion
}
