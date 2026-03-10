using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Shared.Interfaces;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ExecutionEngine)}"" type")]
internal class ExecutionServiceTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ExecutionEngine.CloseAsync" />.
	/// </summary>
	[Test]
	public async Task CloseAsync_Deletes_File_And_Containing_It_Directory()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto(id: Guid.NewGuid());

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICommandLineOptions options = Substitute.For<ICommandLineOptions>();

			options
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

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processUtils);

			builder.RegisterInstance(options);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = dto,
			IsReadOnly = default,
			SessionEncryptedDek = null,
			ViewModel = null
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
	/// Test of <see cref="ExecutionEngine.ExecuteAsync" />.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Executes_File([Values] bool isReadOnly)
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		IFileChangeTracker changeTracker = Substitute.For<IFileChangeTracker>();

		FileModelDto dto = TestUtils.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICommandLineOptions options = Substitute.For<ICommandLineOptions>();

			options
				.SandboxDirectoryPath
				.Returns(TestUtils.CreateRandomDirectoryName());

			builder.RegisterInstance(options);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processUtils);

			builder.RegisterInstance(changeTracker);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = dto,
			IsReadOnly = isReadOnly,
			SessionEncryptedDek = null,
			ViewModel = null
		};

		// Act
		bool result = await sut.ExecuteAsync(parameters);

		// Assert
		result
			.Should()
			.BeTrue();

		sut.IsExecuted(dto.Id)
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
			.TrackChangesAsync(Arg.Any<TrackChangesParameters>());
	}
	#endregion
}
