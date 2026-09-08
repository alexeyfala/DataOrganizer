using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities;
using DataOrganizer.DTO.Execution;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Services.Execution;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Serilog;
using Shared.Interfaces;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ExecutionEngine)}"" type")]
internal class ExecutionEngineTests
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
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
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
				.FindApplicationByExtension(Arg.Any<string>())
				.Returns(Path.Combine(Path.GetTempPath(), "test.exe"));

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processUtils);

			builder.RegisterInstance(sandbox);

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
	/// <see cref="ExecutionEngine.CloseAsync" />: does nothing when the id is not currently executing.
	/// </summary>
	[Test]
	public async Task CloseAsync_Does_Nothing_When_Id_Unknown()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

		using AutoMock mock = CreateConfiguredMock(fileSystem, processUtils, fileAssociation);

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		// Act
		await sut.CloseAsync(Guid.NewGuid());

		// Assert
		processUtils
			.DidNotReceive()
			.KillProcess(Arg.Any<int>());

		fileSystem
			.DidNotReceive()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: waits for a locked file to be released before deleting it.
	/// </summary>
	[Test]
	public async Task CloseAsync_Waits_For_Locked_File_Then_Deletes()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

		fileSystem
			.IsFileExists(Arg.Any<string>())
			.Returns(true);

		fileSystem
			.IsFileLocked(Arg.Any<string>())
			.Returns(true);

		fileSystem
			.WaitFileUnlockedAsync(Arg.Any<string>(), Arg.Any<ILogger>(), Arg.Any<CancellationToken>())
			.Returns(true);

		processUtils
			.IsProcessExists(Arg.Any<int>())
			.Returns(true);

		FileModelDto dto = TestUtils.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = CreateConfiguredMock(fileSystem, processUtils, fileAssociation);

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = dto,
			IsReadOnly = true
		};

		await sut.ExecuteAsync(parameters);

		// Act
		await sut.CloseAsync(dto.Id);

		// Assert
		await fileSystem
			.Received()
			.WaitFileUnlockedAsync(Arg.Any<string>(), Arg.Any<ILogger>(), Arg.Any<CancellationToken>());

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.DisposeAsync" />: cleans up every executing file it still tracks.
	/// </summary>
	[Test]
	public async Task DisposeAsync_Cleans_Up_Executing_Files()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

		fileSystem
			.IsFileExists(Arg.Any<string>())
			.Returns(true);

		processUtils
			.IsProcessExists(Arg.Any<int>())
			.Returns(true);

		FileModelDto dto = TestUtils.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = CreateConfiguredMock(fileSystem, processUtils, fileAssociation);

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = dto,
			IsReadOnly = true
		};

		await sut.ExecuteAsync(parameters);

		// Act
		await sut.DisposeAsync();

		// Assert
		processUtils
			.Received()
			.KillProcess(Arg.Any<int>());

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());

		sut.IsExecuting(dto.Id)
			.Should()
			.BeFalse();
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
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(TestUtils.CreateRandomDirectoryName());

			processUtils
				.StartProcess(Arg.Any<string>(), out Arg.Any<int>())
				.Returns(x =>
				{
					x[1] = TestUtils.CreateRandomIntFrom10To100();

					return true;
				});

			fileAssociation
				.FindApplicationByExtension(Arg.Any<string>())
				.Returns(Path.Combine(Path.GetTempPath(), "test.exe"));

			builder.RegisterInstance(sandbox);

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

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: overwrites the plain text it was given, read-only files included,
	/// and hands the tracker a hash instead of the contents.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Overwrites_The_Contents_It_Was_Given([Values] bool isReadOnly)
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

		IFileChangeTracker changeTracker = Substitute.For<IFileChangeTracker>();

		TrackChangesParameters? tracked = null;

		changeTracker
			.TrackChangesAsync(Arg.Do<TrackChangesParameters>(x => tracked = x), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		using AutoMock mock = CreateConfiguredMock(
			fileSystem,
			processUtils,
			fileAssociation,
			changeTracker);

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		byte[] contents = TestUtils.CreateRandomBytes(16);

		byte[] expectedHash = CryptographicOperations.HashData(TrackChangesParameters.HashAlgorithm, contents);

		ExecuteFileParameters parameters = new()
		{
			Contents = contents,
			File = TestUtils.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = isReadOnly
		};

		// Act
		await sut.ExecuteAsync(parameters);

		// Assert
		contents
			.Should()
			.AllSatisfy(x => x
				.Should()
				.Be(0));

		if (isReadOnly)
		{
			tracked
				.Should()
				.BeNull();

			return;
		}

		tracked
			.Should()
			.NotBeNull();

		tracked
			.PreviousHash
			.Should()
			.Equal(expectedHash);
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: returns false without starting a process once the engine is disposed.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Returns_False_After_Dispose()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

		using AutoMock mock = CreateConfiguredMock(fileSystem, processUtils, fileAssociation);

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		await sut.DisposeAsync();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = TestUtils.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = true
		};

		// Act
		bool result = await sut.ExecuteAsync(parameters);

		// Assert
		result
			.Should()
			.BeFalse();

		processUtils
			.DidNotReceive()
			.StartProcess(Arg.Any<string>(), out Arg.Any<int>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: refuses a second execution of a file that is already running.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Returns_False_When_Already_Executing()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

		FileModelDto dto = TestUtils.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = CreateConfiguredMock(fileSystem, processUtils, fileAssociation);

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = dto,
			IsReadOnly = true
		};

		await sut.ExecuteAsync(parameters);

		// Act
		bool result = await sut.ExecuteAsync(parameters);

		// Assert
		result
			.Should()
			.BeFalse();

		processUtils
			.Received(1)
			.StartProcess(Arg.Any<string>(), out Arg.Any<int>());
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds a loose <see cref="AutoMock" /> with the sandbox environment, process launch and
	/// file-association stubs wired for a successful execution.
	/// </summary>
	private static AutoMock CreateConfiguredMock(
		IFileSystem fileSystem,
		IProcessUtils processUtils,
		IFileAssociationService fileAssociation,
		IFileChangeTracker? changeTracker = null)
	{
		return AutoMock.GetLoose(builder =>
		{
			if (changeTracker is not null)
			{
				builder.RegisterInstance(changeTracker);
			}

			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(TestUtils.CreateRandomDirectoryName());

			processUtils
				.StartProcess(Arg.Any<string>(), out Arg.Any<int>())
				.Returns(x =>
				{
					x[1] = TestUtils.CreateRandomIntFrom10To100();

					return true;
				});

			fileAssociation
				.FindApplicationByExtension(Arg.Any<string>())
				.Returns(Path.Combine(Path.GetTempPath(), "test.exe"));

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processUtils);

			builder.RegisterInstance(fileAssociation);
		});
	}
	#endregion
}
