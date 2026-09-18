using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Services.Execution;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Serilog;
using Shared.Interfaces;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Services.Execution;

[TestFixture(Description = $@"Tests of ""{nameof(ExecutionEngine)}"" type")]
internal class ExecutionEngineTests
{
	#region Data
	/// <summary>
	/// Path of the application a file extension is associated with.
	/// </summary>
	private const string AssociatedAppPath = @"C:\Apps\test.exe";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: kills the process, clears the read-only flag, erases the file and deletes its directory.
	/// </summary>
	[Test]
	public async Task CloseAsync_Deletes_File_And_Containing_It_Directory()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(true);

			processManager
				.StartProcess(Arg.Any<string>(), out Arg.Any<int>())
				.Returns(x =>
				{
					x[1] = RandomValues.CreateIntFrom10To100();

					return true;
				});

			processManager
				.ProcessExists(Arg.Any<int>())
				.Returns(true);

			fileAssociation
				.FindApplicationByExtension(Arg.Any<string>())
				.Returns(AssociatedAppPath);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileAssociation);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = dto,
			// A read-only file is the case where clearing the flag is what lets the erase happen at all.
			IsReadOnly = true
		};

		await sut.ExecuteAsync(parameters);

		// Act
		await sut.CloseAsync(dto.Id);

		// Assert
		processManager
			.Received(1)
			.KillProcess(Arg.Any<int>());

		fileSystem
			.Received(1)
			.SetFileReadOnly(Arg.Any<string>(), false);

		fileSystem
			.Received(1)
			.EraseAndDeleteFile(Arg.Any<string>());

		fileSystem
			.Received(1)
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

		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		// Act
		await sut.CloseAsync(Guid.NewGuid());

		// Assert
		processManager
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

		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

			IProcessManager processManager = Substitute.For<IProcessManager>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.IsFileLocked(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.WaitUntilFileUnlockedAsync(Arg.Any<string>(), Arg.Any<ILogger>(), Arg.Any<CancellationToken>())
				.Returns(true);

			fileAssociation
				.FindApplicationByExtension(Arg.Any<string>())
				.Returns(AssociatedAppPath);

			processManager
				.StartProcess(Arg.Any<string>(), out Arg.Any<int>())
				.Returns(x =>
				{
					x[1] = RandomValues.CreateIntFrom10To100();

					return true;
				});

			processManager
				.ProcessExists(Arg.Any<int>())
				.Returns(true);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);

			builder.RegisterInstance(fileAssociation);
		});

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
			.WaitUntilFileUnlockedAsync(Arg.Any<string>(), Arg.Any<ILogger>(), Arg.Any<CancellationToken>());

		fileSystem
			.Received(1)
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

		IProcessManager processManager = Substitute.For<IProcessManager>();

		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(true);

			fileAssociation
				.FindApplicationByExtension(Arg.Any<string>())
				.Returns(AssociatedAppPath);

			processManager
				.StartProcess(Arg.Any<string>(), out Arg.Any<int>())
				.Returns(x =>
				{
					x[1] = RandomValues.CreateIntFrom10To100();

					return true;
				});

			processManager
				.ProcessExists(Arg.Any<int>())
				.Returns(true);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);

			builder.RegisterInstance(fileAssociation);
		});

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
		processManager
			.Received(1)
			.KillProcess(Arg.Any<int>());

		fileSystem
			.Received(1)
			.EraseAndDeleteFile(Arg.Any<string>());

		sut.IsExecuting(dto.Id)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: writes the file into the sandbox, marks it read-only when asked and starts the process.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Executes_File([Values] bool isReadOnly)
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessManager processManager = Substitute.For<IProcessManager>();

		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			processManager
				.StartProcess(Arg.Any<string>(), out Arg.Any<int>())
				.Returns(x =>
				{
					x[1] = RandomValues.CreateIntFrom10To100();

					return true;
				});

			fileAssociation
				.FindApplicationByExtension(Arg.Any<string>())
				.Returns(AssociatedAppPath);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);

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
			.Received(1)
			.CreateDirectory(Arg.Any<string>());

		await fileSystem
			.Received(1)
			.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>());

		fileSystem
			.Received(1)
			.SetFileReadOnly(Arg.Any<string>(), isReadOnly);

		processManager
			.Received(1)
			.StartProcess(Arg.Any<string>(), out Arg.Any<int>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: overwrites the plain text it was given, read-only files included,
	/// and hands the tracker a hash instead of the contents.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Overwrites_The_Contents_It_Was_Given([Values] bool isReadOnly)
	{
		// Arrange
		TrackChangesParameters? tracked = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

			IFileChangeTracker changeTracker = Substitute.For<IFileChangeTracker>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			IProcessManager processManager = Substitute.For<IProcessManager>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			fileAssociation
				.FindApplicationByExtension(Arg.Any<string>())
				.Returns(AssociatedAppPath);

			changeTracker
				.TrackChangesAsync(Arg.Do<TrackChangesParameters>(x => tracked = x), Arg.Any<CancellationToken>())
				.Returns(Task.CompletedTask);

			processManager
				.StartProcess(Arg.Any<string>(), out Arg.Any<int>())
				.Returns(x =>
				{
					x[1] = RandomValues.CreateIntFrom10To100();

					return true;
				});

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);

			builder.RegisterInstance(fileAssociation);

			builder.RegisterInstance(changeTracker);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		byte[] contents = RandomValues.CreateBytes(16);

		byte[] expectedHash = CryptographicOperations.HashData(TrackChangesParameters.HashAlgorithm, contents);

		ExecuteFileParameters parameters = new()
		{
			Contents = contents,
			File = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = isReadOnly
		};

		// Act
		await sut.ExecuteAsync(parameters);

		// Assert
		contents
			.Should()
			.AllSatisfy(x => x.Should().Be(0));

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
		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(processManager));

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		await sut.DisposeAsync();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = true
		};

		// Act
		bool result = await sut.ExecuteAsync(parameters);

		// Assert
		result
			.Should()
			.BeFalse();

		processManager
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
		IProcessManager processManager = Substitute.For<IProcessManager>();

		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			fileAssociation
				.FindApplicationByExtension(Arg.Any<string>())
				.Returns(AssociatedAppPath);

			processManager
				.StartProcess(Arg.Any<string>(), out Arg.Any<int>())
				.Returns(x =>
				{
					x[1] = RandomValues.CreateIntFrom10To100();

					return true;
				});

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);

			builder.RegisterInstance(fileAssociation);
		});

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

		processManager
			.Received(1)
			.StartProcess(Arg.Any<string>(), out Arg.Any<int>());
	}
	#endregion
}
