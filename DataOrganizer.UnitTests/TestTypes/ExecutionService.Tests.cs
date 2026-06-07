using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities;
using DataOrganizer.DTO.Execution;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Shared.Interfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ExecutionEngine)}"" type")]
internal class ExecutionServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: kills the process, clears the read-only flag, erases the file and deletes its directory.
	/// </summary>
	[Test]
	public async Task CloseAsync_Deletes_File_And_Containing_It_Directory()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto(id: Guid.NewGuid());

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.SandboxDirectoryPath
				.Returns(TestUtils.CreateRandomDirectoryName());

			fileSystem
				.IsFileExists(Arg.Any<string>())
				.Returns(true);

			processUtils
				.StartProcess(Arg.Any<string>(), out Arg.Any<int>())
				.Returns(x =>
				{
					x[1] = TestUtils.CreateRandomIntFrom10To100();

					return true;
				});

			processUtils
				.IsProcessExists(Arg.Any<int>())
				.Returns(true);

			fileAssociation
				.GetApplicationByExtension(Arg.Any<string>())
				.Returns(Path.Combine(Path.GetTempPath(), "test.exe"));

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processUtils);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(fileAssociation);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = dto,
			IsReadOnly = default
		};

		await sut.ExecuteAsync(parameters);

		// Act
		await sut.CloseAsync(dto.Id);

		// Assert
		processUtils
			.Received()
			.KillProcess(Arg.Any<int>());

		fileSystem
			.Received()
			.SetFileReadOnly(Arg.Any<string>(), false);

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());

		fileSystem
			.Received()
			.DeleteDirectory(Arg.Any<string>(), Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: writes the file, starts the process and tracks changes only for editable (non-read-only) files.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Executes_File([Values] bool isReadOnly)
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		IFileChangeTracker changeTracker = Substitute.For<IFileChangeTracker>();

		IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

		FileModelDto dto = TestUtils.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.SandboxDirectoryPath
				.Returns(TestUtils.CreateRandomDirectoryName());

			processUtils
				.StartProcess(Arg.Any<string>(), out Arg.Any<int>())
				.Returns(x =>
				{
					x[1] = TestUtils.CreateRandomIntFrom10To100();

					return true;
				});

			fileAssociation
				.GetApplicationByExtension(Arg.Any<string>())
				.Returns(Path.Combine(Path.GetTempPath(), "test.exe"));

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processUtils);

			builder.RegisterInstance(changeTracker);

			builder.RegisterInstance(fileAssociation);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = dto,
			IsReadOnly = isReadOnly
		};

		// Act
		bool result = await sut.ExecuteAsync(parameters);

		// Assert
		result
			.Should()
			.BeTrue();

		sut.IsExecuting(dto.Id)
			.Should()
			.BeTrue();

		fileSystem
			.Received()
			.CreateDirectory(Arg.Any<string>());

		await fileSystem
			.Received()
			.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>());

		fileSystem
			.Received()
			.SetFileReadOnly(Arg.Any<string>(), isReadOnly);

		processUtils
			.Received()
			.StartProcess(Arg.Any<string>(), out Arg.Any<int>());

		await changeTracker
			.Received(isReadOnly ? 0 : 1)
			.TrackChangesAsync(Arg.Any<TrackChangesParameters>(), Arg.Any<CancellationToken>());
	}
	#endregion
}
