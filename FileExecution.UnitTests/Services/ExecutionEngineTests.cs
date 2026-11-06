using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Diagnostics.Interfaces;
using FileExecution.Dto;
using FileExecution.Interfaces;
using FileExecution.Services;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using Notifications.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using Shared.Interfaces;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Testably.Abstractions.Testing;
using TestSupport.Common;

namespace FileExecution.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(ExecutionEngine)}"" type")]
internal class ExecutionEngineTests
{
	#region Data
	/// <summary>
	/// Path of the application the user picks to open a file.
	/// </summary>
	private const string PickedAppPath = @"C:\Apps\picked.exe";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: a wait for the engine that the caller cancels closes nothing and ends
	/// quietly.
	/// </summary>
	[Test]
	public async Task CloseAsync_Closes_Nothing_When_The_Wait_Is_Cancelled()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		IExecutingFileProcess process = Substitute.For<IExecutingFileProcess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			processLauncher
				.OpenFile(Arg.Any<string>())
				.Returns(process);

			builder.RegisterInstance(processLauncher);

			builder.RegisterInstance(sandbox);

			// Moq fakes the file system with no folders behind it, and the start writes the file into one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
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
		await sut.CloseAsync(dto.Id, new CancellationToken(canceled: true));

		// Assert
		sut.IsExecuting(dto.Id)
			.Should()
			.BeTrue();

		process
			.DidNotReceive()
			.Kill();

		process
			.DidNotReceive()
			.Dispose();
	}

	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: kills the process, clears the read-only flag, erases the file and deletes its directory.
	/// </summary>
	[Test]
	public async Task CloseAsync_Deletes_File_And_Containing_It_Directory()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		IFileEraser fileEraser = Substitute.For<IFileEraser>();

		MockFileSystem files = new();

		IExecutingFileProcess process = Substitute.For<IExecutingFileProcess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			processLauncher
				.OpenFile(Arg.Any<string>())
				.Returns(process);

			builder.RegisterInstance(fileEraser);

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(processLauncher);

			builder.RegisterInstance(sandbox);
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
		process
			.Received(1)
			.Kill();

		files.Statistics.File.Methods
			.Should()
			.ContainSingle(x => x.Name == nameof(IFile.SetAttributes) && x.Parameters[1].Is<FileAttributes>(a => !a.HasFlag(FileAttributes.ReadOnly)));

		fileEraser
			.Received(1)
			.EraseAndDeleteFile(Arg.Any<string>());

		files.Statistics.Directory.Methods
			.Should()
			.ContainSingle(x => x.Name == nameof(IDirectory.Delete));
	}

	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: a process that cannot be killed is still released, and the file is
	/// erased all the same.
	/// </summary>
	[Test]
	public async Task CloseAsync_Deletes_The_File_When_The_Kill_Fails()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		IFileEraser fileEraser = Substitute.For<IFileEraser>();

		IExecutingFileProcess process = Substitute.For<IExecutingFileProcess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			processLauncher
				.OpenFile(Arg.Any<string>())
				.Returns(process);

			process
				.When(x => x.Kill())
				.Throw(new InvalidOperationException());

			builder.RegisterInstance(fileEraser);

			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();

			builder.RegisterInstance(processLauncher);

			builder.RegisterInstance(sandbox);
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
		process
			.Received(1)
			.Dispose();

		fileEraser
			.Received(1)
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: does nothing when the id is not currently executing.
	/// </summary>
	[Test]
	public async Task CloseAsync_Does_Nothing_When_Id_Unknown()
	{
		// Arrange
		IFileEraser fileEraser = Substitute.For<IFileEraser>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(fileEraser));

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		// Act
		await sut.CloseAsync(Guid.NewGuid());

		// Assert
		fileEraser
			.DidNotReceive()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: a process that has ended is not killed.
	/// </summary>
	[Test]
	public async Task CloseAsync_Kills_No_Process_That_Has_Ended()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		IExecutingFileProcess process = Substitute.For<IExecutingFileProcess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			processLauncher
				.OpenFile(Arg.Any<string>())
				.Returns(process);

			// The process has ended by the time the file is closed.
			process
				.HasExited
				.Returns(true);

			builder.RegisterInstance(processLauncher);

			builder.RegisterInstance(sandbox);
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
		process
			.DidNotReceive()
			.Kill();
	}

	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: the programs that hold the file are killed and released instead of the
	/// started process, which is only released.
	/// </summary>
	[Test]
	public async Task CloseAsync_Kills_The_Programs_That_Hold_The_File()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		IExecutingFileProcess started = Substitute.For<IExecutingFileProcess>();

		IExecutingFileProcess firstHolder = Substitute.For<IExecutingFileProcess>();

		IExecutingFileProcess secondHolder = Substitute.For<IExecutingFileProcess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

			IExecutingFileHolderFinder holderFinder = Substitute.For<IExecutingFileHolderFinder>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			processLauncher
				.OpenFile(Arg.Any<string>())
				.Returns(started);

			holderFinder
				.FindHolders(Arg.Any<string>())
				.Returns([firstHolder, secondHolder]);

			builder.RegisterInstance(processLauncher);

			builder.RegisterInstance(holderFinder);

			builder.RegisterInstance(sandbox);

			// Moq fakes the file system with no folders behind it, and the start writes the file into one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
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
		firstHolder
			.Received(1)
			.Kill();

		firstHolder
			.Received(1)
			.Dispose();

		secondHolder
			.Received(1)
			.Kill();

		secondHolder
			.Received(1)
			.Dispose();

		started
			.DidNotReceive()
			.Kill();

		started
			.Received(1)
			.Dispose();
	}

	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: when the search for the programs that hold the file fails, the started
	/// process is killed as without a search.
	/// </summary>
	[Test]
	public async Task CloseAsync_Kills_The_Started_Process_When_The_Search_Fails()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		IExecutingFileProcess started = Substitute.For<IExecutingFileProcess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

			IExecutingFileHolderFinder holderFinder = Substitute.For<IExecutingFileHolderFinder>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			processLauncher
				.OpenFile(Arg.Any<string>())
				.Returns(started);

			holderFinder
				.FindHolders(Arg.Any<string>())
				.Throws(new InvalidOperationException());

			builder.RegisterInstance(processLauncher);

			builder.RegisterInstance(holderFinder);

			builder.RegisterInstance(sandbox);

			// Moq fakes the file system with no folders behind it, and the start writes the file into one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
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
		started
			.Received(1)
			.Kill();
	}

	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: a file that is gone from the sandbox is not deleted again.
	/// </summary>
	[Test]
	public async Task CloseAsync_Leaves_A_File_That_Is_Gone_Alone()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		IFileEraser fileEraser = Substitute.For<IFileEraser>();

		MockFileSystem files = new();

		string directoryPath = RandomValues.CreateDirectoryName();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(directoryPath);

			builder.RegisterInstance(fileEraser);

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(sandbox);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = dto,
			IsReadOnly = false
		};

		await sut.ExecuteAsync(parameters);

		// The file is gone from the sandbox by the time it is closed.
		files
			.Directory
			.Delete(directoryPath, recursive: true);

		// Act
		await sut.CloseAsync(dto.Id);

		// Assert
		// The one clearing of the sign is the write of a file that is not read-only; the gone copy is not touched again.
		files.Statistics.File.Methods
			.Should()
			.ContainSingle(x => x.Name == nameof(IFile.SetAttributes) && x.Parameters[1].Is<FileAttributes>(a => !a.HasFlag(FileAttributes.ReadOnly)));

		fileEraser
			.DidNotReceive()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: the process started for the file is released, whether it still runs or
	/// has ended.
	/// </summary>
	[Test]
	public async Task CloseAsync_Releases_The_Started_Process([Values] bool hasExited)
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		IExecutingFileProcess process = Substitute.For<IExecutingFileProcess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			processLauncher
				.OpenFile(Arg.Any<string>())
				.Returns(process);

			process
				.HasExited
				.Returns(hasExited);

			builder.RegisterInstance(processLauncher);

			builder.RegisterInstance(sandbox);

			// Moq fakes the file system with no folders behind it, and the start writes the file into one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
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
		process
			.Received(1)
			.Dispose();
	}

	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: the change tracker of the file is told to stop, and the closing waits
	/// until it has.
	/// </summary>
	[Test]
	public async Task CloseAsync_Stops_The_Change_Tracker_And_Waits_For_It()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		TaskCompletionSource tracking = new();

		CancellationToken trackerToken = default;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileChangeTracker changeTracker = Substitute.For<IFileChangeTracker>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			changeTracker
				.TrackChangesAsync(Arg.Any<TrackChangesParameters>(), Arg.Do<CancellationToken>(x => trackerToken = x))
				.Returns(tracking.Task);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(changeTracker);

			// Moq fakes the file system with no folders behind it, and the start writes the file into one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = dto,
			IsReadOnly = false
		};

		await sut.ExecuteAsync(parameters);

		// Act
		Task closing = sut.CloseAsync(dto.Id);

		// Assert
		trackerToken.IsCancellationRequested
			.Should()
			.BeTrue();

		closing.IsCompleted
			.Should()
			.BeFalse();

		tracking.SetResult();

		await closing;
	}

	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: waits for a locked file to be released before deleting it.
	/// </summary>
	[Test]
	public async Task CloseAsync_Waits_For_Locked_File_Then_Deletes()
	{
		// Arrange
		IFileEraser fileEraser = Substitute.For<IFileEraser>();

		IFileLockChecker fileLockChecker = Substitute.For<IFileLockChecker>();

		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			fileLockChecker
				.IsLocked(Arg.Any<string>())
				.Returns(true);

			fileLockChecker
				.WaitUntilUnlockedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileEraser);

			builder.RegisterInstance(fileLockChecker);

			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();
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
		await fileLockChecker
			.Received()
			.WaitUntilUnlockedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

		fileEraser
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
		IFileEraser fileEraser = Substitute.For<IFileEraser>();

		IExecutingFileProcess process = Substitute.For<IExecutingFileProcess>();

		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			processLauncher
				.OpenFile(Arg.Any<string>())
				.Returns(process);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileEraser);

			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();

			builder.RegisterInstance(processLauncher);
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
		process
			.Received(1)
			.Kill();

		process
			.Received(1)
			.Dispose();

		fileEraser
			.Received(1)
			.EraseAndDeleteFile(Arg.Any<string>());

		sut.IsExecuting(dto.Id)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ExecutionEngine.DisposeAsync" />: a file that is gone from the sandbox is not deleted again.
	/// </summary>
	[Test]
	public async Task DisposeAsync_Leaves_A_File_That_Is_Gone_Alone()
	{
		// Arrange
		IFileEraser fileEraser = Substitute.For<IFileEraser>();

		MockFileSystem files = new();

		string directoryPath = RandomValues.CreateDirectoryName();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(directoryPath);

			builder.RegisterInstance(fileEraser);

			builder.RegisterInstance<IFileSystem>(files);

			builder.RegisterInstance(sandbox);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = false
		};

		await sut.ExecuteAsync(parameters);

		// The file is gone from the sandbox by the time the engine is disposed.
		files
			.Directory
			.Delete(directoryPath, recursive: true);

		// Act
		await sut.DisposeAsync();

		// Assert
		// The one clearing of the sign is the write of a file that is not read-only; the gone copy is not touched again.
		files.Statistics.File.Methods
			.Should()
			.ContainSingle(x => x.Name == nameof(IFile.SetAttributes) && x.Parameters[1].Is<FileAttributes>(a => !a.HasFlag(FileAttributes.ReadOnly)));

		fileEraser
			.DidNotReceive()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.DisposeAsync" />: the change trackers of the files are told to stop.
	/// </summary>
	[Test]
	public async Task DisposeAsync_Stops_The_Change_Trackers()
	{
		// Arrange
		CancellationToken trackerToken = default;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileChangeTracker changeTracker = Substitute.For<IFileChangeTracker>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			changeTracker
				.TrackChangesAsync(Arg.Any<TrackChangesParameters>(), Arg.Do<CancellationToken>(x => trackerToken = x))
				.Returns(Task.CompletedTask);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(changeTracker);

			// Moq fakes the file system with no folders behind it, and the start writes the file into one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = false
		};

		await sut.ExecuteAsync(parameters);

		// Act
		await sut.DisposeAsync();

		// Assert
		trackerToken.IsCancellationRequested
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: writes the file into the sandbox, marks it read-only when asked and starts the process.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Executes_File([Values] bool isReadOnly)
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processLauncher);
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
			.Directory
			.Received(1)
			.CreateDirectory(Arg.Any<string>());

		await fileSystem
			.File
			.Received(1)
			.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>());

		fileSystem
			.File
			.Received(1)
			.SetAttributes(Arg.Any<string>(), Arg.Is<FileAttributes>(x => x.HasFlag(FileAttributes.ReadOnly) == isReadOnly));

		processLauncher
			.Received(1)
			.OpenFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: the task of the change tracker goes to the exception handler, so that
	/// its failure is seen.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Hands_The_Change_Tracker_To_The_Exception_Handler()
	{
		// Arrange
		Task tracking = Task.FromResult(new object());

		ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileChangeTracker changeTracker = Substitute.For<IFileChangeTracker>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			changeTracker
				.TrackChangesAsync(Arg.Any<TrackChangesParameters>(), Arg.Any<CancellationToken>())
				.Returns(tracking);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(changeTracker);

			builder.RegisterInstance(exceptionHandler);

			// Moq fakes the file system with no folders behind it, and the start writes the file into one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = false
		};

		// Act
		await sut.ExecuteAsync(parameters);

		// Assert
		exceptionHandler
			.Received(1)
			.Watch(tracking);
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: without an associated program the file opens in the program the user
	/// picks.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Opens_The_File_In_The_Picked_Program_Without_An_Associated_Program()
	{
		// Arrange
		IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppPickerService appPicker = Substitute.For<IAppPickerService>();

			IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			appPicker
				.PickAppAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(new AssociatedAppInfo
				{
					AppName = "Picked",
					AppPath = PickedAppPath
				});

			fileAssociation
				.HasAssociatedApp(Arg.Any<string>())
				.Returns(false);

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			builder.RegisterInstance(appPicker);

			builder.RegisterInstance(fileAssociation);

			builder.RegisterInstance(processLauncher);

			builder.RegisterInstance(sandbox);

			builder
				.RegisterType<WindowsAppResolver>()
				.As<IAppResolver>();

			// Moq fakes the file system with no folders behind it, and the start writes the file into one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = false
		};

		// Act
		bool result = await sut.ExecuteAsync(parameters);

		// Assert
		result
			.Should()
			.BeTrue();

		processLauncher
			.Received(1)
			.OpenFile(PickedAppPath, Arg.Any<string>());
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

			IFileChangeTracker changeTracker = Substitute.For<IFileChangeTracker>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			changeTracker
				.TrackChangesAsync(Arg.Do<TrackChangesParameters>(x => tracked = x), Arg.Any<CancellationToken>())
				.Returns(Task.CompletedTask);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

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
	/// <see cref="ExecutionEngine.ExecuteAsync" />: a file that another call started while this one waited for the engine
	/// is refused, and only one process starts.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Refuses_A_File_Started_While_It_Waited()
	{
		// Arrange
		TaskCompletionSource writing = new();

		IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			// The first call holds the engine while it writes the file.
			fileSystem
				.File
				.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
				.Returns(writing.Task);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processLauncher);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = true
		};

		Task<bool> first = sut.ExecuteAsync(parameters);

		Task<bool> second = sut.ExecuteAsync(parameters);

		// Act
		writing.SetResult();

		// Assert
		(await first)
			.Should()
			.BeTrue();

		(await second)
			.Should()
			.BeFalse();

		processLauncher
			.Received(1)
			.OpenFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: tells the user that a file could not be opened.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Reports_A_File_That_Could_Not_Be_Opened()
	{
		// Arrange
		INotificationService notification = Substitute.For<INotificationService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			fileSystem
				.File
				.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
				.Throws(new IOException());

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(notification);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

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

		notification
			.Received(1)
			.ShowErrorSnackbar(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: returns false without starting a process once the engine is disposed.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Returns_False_After_Dispose()
	{
		// Arrange
		IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(processLauncher));

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

		processLauncher
			.DidNotReceive()
			.OpenFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: refuses a second execution of a file that is already running.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Returns_False_When_Already_Executing()
	{
		// Arrange
		IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processLauncher);
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

		processLauncher
			.Received(1)
			.OpenFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: a call that waited for the engine while it was being disposed starts
	/// nothing.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Returns_False_When_Disposed_While_It_Waited()
	{
		// Arrange
		TaskCompletionSource writing = new();

		IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			// The first call holds the engine while it writes the file.
			fileSystem
				.File
				.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
				.Returns(writing.Task);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processLauncher);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters firstParameters = new()
		{
			Contents = [],
			File = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = true
		};

		ExecuteFileParameters secondParameters = new()
		{
			Contents = [],
			File = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = true
		};

		Task<bool> first = sut.ExecuteAsync(firstParameters);

		Task<bool> second = sut.ExecuteAsync(secondParameters);

		Task disposing = sut
			.DisposeAsync()
			.AsTask();

		// Act
		writing.SetResult();

		// Assert
		(await second)
			.Should()
			.BeFalse();

		await first;

		await disposing;

		processLauncher
			.Received(1)
			.OpenFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: a wait for the engine that the caller cancels starts nothing.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Returns_False_When_The_Wait_Is_Cancelled()
	{
		// Arrange
		IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(processLauncher);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = true
		};

		// Act
		bool result = await sut.ExecuteAsync(parameters, new CancellationToken(canceled: true));

		// Assert
		result
			.Should()
			.BeFalse();

		processLauncher
			.DidNotReceive()
			.OpenFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: a file whose start the dispose of the engine overtook is rolled back:
	/// its tracker is stopped, its process killed and its file and folder deleted.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Rolls_Back_A_File_Started_While_The_Engine_Was_Disposed()
	{
		// Arrange
		TaskCompletionSource writing = new();

		CancellationToken trackerToken = default;

		string directoryPath = RandomValues.CreateDirectoryName();

		IFileEraser fileEraser = Substitute.For<IFileEraser>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IExecutingFileProcess process = Substitute.For<IExecutingFileProcess>();

		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileChangeTracker changeTracker = Substitute.For<IFileChangeTracker>();

			IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

			sandbox
				.GetFileDirectoryPath(dto.Id)
				.Returns(directoryPath);

			// The call holds the engine while it writes the file, and the dispose waits for it.
			fileSystem
				.File
				.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
				.Returns(writing.Task);

			processLauncher
				.OpenFile(Arg.Any<string>())
				.Returns(process);

			changeTracker
				.TrackChangesAsync(Arg.Any<TrackChangesParameters>(), Arg.Do<CancellationToken>(x => trackerToken = x))
				.Returns(Task.CompletedTask);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileEraser);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processLauncher);

			builder.RegisterInstance(changeTracker);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = dto,
			IsReadOnly = false
		};

		Task<bool> executing = sut.ExecuteAsync(parameters);

		Task disposing = sut
			.DisposeAsync()
			.AsTask();

		// Act
		writing.SetResult();

		// Assert
		(await executing)
			.Should()
			.BeFalse();

		await disposing;

		trackerToken.IsCancellationRequested
			.Should()
			.BeTrue();

		process
			.Received(1)
			.Kill();

		process
			.Received(1)
			.Dispose();

		// Once when the file is written, once when it is rolled back.
		fileSystem
			.File
			.Received(2)
			.SetAttributes(Arg.Any<string>(), Arg.Is<FileAttributes>(x => !x.HasFlag(FileAttributes.ReadOnly)));

		fileEraser
			.Received(1)
			.EraseAndDeleteFile(Arg.Any<string>());

		fileSystem
			.Directory
			.Received(1)
			.Delete(directoryPath, true);
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: when the change tracker cannot be handed to the exception handler, the
	/// tracker is stopped, the start is rolled back and the call fails.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Rolls_Back_When_The_Tracker_Cannot_Be_Watched()
	{
		// Arrange
		CancellationToken trackerToken = default;

		IExecutingFileProcess process = Substitute.For<IExecutingFileProcess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileChangeTracker changeTracker = Substitute.For<IFileChangeTracker>();

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			processLauncher
				.OpenFile(Arg.Any<string>())
				.Returns(process);

			changeTracker
				.TrackChangesAsync(Arg.Any<TrackChangesParameters>(), Arg.Do<CancellationToken>(x => trackerToken = x))
				.Returns(Task.CompletedTask);

			exceptionHandler
				.When(x => x.Watch(Arg.Any<Task>()))
				.Throw(new InvalidOperationException());

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(processLauncher);

			builder.RegisterInstance(changeTracker);

			builder.RegisterInstance(exceptionHandler);

			// Moq fakes the file system with no folders behind it, and the start writes the file into one.
			builder.RegisterInstance(Substitute.For<IFileSystem>());
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = false
		};

		// Act
		bool result = await sut.ExecuteAsync(parameters);

		// Assert
		result
			.Should()
			.BeFalse();

		trackerToken.IsCancellationRequested
			.Should()
			.BeTrue();

		process
			.Received(1)
			.Kill();

		process
			.Received(1)
			.Dispose();
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: when the user cancels the choice of a program, the file is not
	/// written and nothing starts.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Starts_Nothing_When_The_Choice_Of_A_Program_Is_Cancelled()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessLauncher processLauncher = Substitute.For<IProcessLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppPickerService appPicker = Substitute.For<IAppPickerService>();

			IFileAssociationService fileAssociation = Substitute.For<IFileAssociationService>();

			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			// The user closes the picker without a program.
			appPicker
				.PickAppAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns((AssociatedAppInfo?)null);

			// No program is associated with the extension.
			fileAssociation
				.HasAssociatedApp(Arg.Any<string>())
				.Returns(false);

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			builder.RegisterInstance(appPicker);

			builder.RegisterInstance(fileAssociation);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processLauncher);

			builder.RegisterInstance(sandbox);

			builder
				.RegisterType<WindowsAppResolver>()
				.As<IAppResolver>();
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = false
		};

		// Act
		bool result = await sut.ExecuteAsync(parameters);

		// Assert
		result
			.Should()
			.BeFalse();

		fileSystem
			.Directory
			.DidNotReceive()
			.CreateDirectory(Arg.Any<string>());

		processLauncher
			.DidNotReceiveWithAnyArgs()
			.OpenFile(default!, default!);

		processLauncher
			.DidNotReceiveWithAnyArgs()
			.OpenFile(default!);
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: writes the file into the sandbox under a name that the file system accepts.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Writes_The_File_Under_A_Name_The_File_System_Accepts()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		string directoryPath = RandomValues.CreateDirectoryName();

		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		dto.Name = "report?.txt";

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			sandbox
				.GetFileDirectoryPath(dto.Id)
				.Returns(directoryPath);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = dto,
			IsReadOnly = true
		};

		// Act
		await sut.ExecuteAsync(parameters);

		// Assert
		await fileSystem
			.File
			.Received(1)
			.WriteAllBytesAsync(Path.Combine(directoryPath, "report_.txt"), Arg.Any<byte[]>());
	}
	#endregion
}
