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
using Serilog;
using Shared.Interfaces;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
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

		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

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

			processManager
				.ProcessExists(Arg.Any<int>())
				.Returns(true);

			builder.RegisterInstance(processManager);

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
		await sut.CloseAsync(dto.Id, new CancellationToken(canceled: true));

		// Assert
		sut.IsExecuting(dto.Id)
			.Should()
			.BeTrue();

		processManager
			.DidNotReceive()
			.KillProcess(Arg.Any<int>());
	}

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

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);

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
	/// <see cref="ExecutionEngine.CloseAsync" />: a process that has ended is not killed.
	/// </summary>
	[Test]
	public async Task CloseAsync_Kills_No_Process_That_Has_Ended()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

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

			// The process has ended by the time the file is closed.
			processManager
				.ProcessExists(Arg.Any<int>())
				.Returns(false);

			builder.RegisterInstance(processManager);

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
		processManager
			.DidNotReceive()
			.KillProcess(Arg.Any<int>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.CloseAsync" />: a file that is gone from the sandbox is not deleted again.
	/// </summary>
	[Test]
	public async Task CloseAsync_Leaves_A_File_That_Is_Gone_Alone()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			// The file is gone from the sandbox by the time it is closed.
			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(false);

			builder.RegisterInstance(fileSystem);

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
		fileSystem
			.DidNotReceive()
			.SetFileReadOnly(Arg.Any<string>(), false);

		fileSystem
			.DidNotReceive()
			.EraseAndDeleteFile(Arg.Any<string>());
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
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

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

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);
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
	/// <see cref="ExecutionEngine.DisposeAsync" />: a file that is gone from the sandbox is not deleted again.
	/// </summary>
	[Test]
	public async Task DisposeAsync_Leaves_A_File_That_Is_Gone_Alone()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			// The file is gone from the sandbox by the time the engine is disposed.
			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(false);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(sandbox);
		});

		ExecutionEngine sut = mock.Create<ExecutionEngine>();

		ExecuteFileParameters parameters = new()
		{
			Contents = [],
			File = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid()),
			IsReadOnly = true
		};

		await sut.ExecuteAsync(parameters);

		// Act
		await sut.DisposeAsync();

		// Assert
		fileSystem
			.DidNotReceive()
			.SetFileReadOnly(Arg.Any<string>(), false);

		fileSystem
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

		IProcessManager processManager = Substitute.For<IProcessManager>();

		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

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

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);
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
	/// <see cref="ExecutionEngine.ExecuteAsync" />: without an associated program, or with the "Open with" dialog of
	/// Windows as one, the file opens in the program the user picks.
	/// </summary>
	[TestCase(null)]
	[TestCase(@"C:\Windows\System32\OpenWith.exe")]
	public async Task ExecuteAsync_Opens_The_File_In_The_Picked_Program_Without_A_Usable_Association(string? associatedAppPath)
	{
		// Arrange
		IProcessManager processManager = Substitute.For<IProcessManager>();

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
				.FindApplicationByExtension(Arg.Any<string>())
				.Returns(associatedAppPath);

			processManager
				.StartProcess(PickedAppPath, Arg.Any<string>(), out Arg.Any<int>())
				.Returns(x =>
				{
					x[2] = RandomValues.CreateIntFrom10To100();

					return true;
				});

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			builder.RegisterInstance(appPicker);

			builder.RegisterInstance(fileAssociation);

			builder.RegisterInstance(processManager);

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
			.BeTrue();

		processManager
			.Received(1)
			.StartProcess(PickedAppPath, Arg.Any<string>(), out Arg.Any<int>());
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

			IProcessManager processManager = Substitute.For<IProcessManager>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

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

		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			// The first call holds the engine while it writes the file.
			fileSystem
				.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
				.Returns(writing.Task);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);
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

		processManager
			.Received(1)
			.StartProcess(Arg.Any<string>(), out Arg.Any<int>());
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

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

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

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);
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

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: a call that waited for the engine while it was being disposed starts
	/// nothing.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Returns_False_When_Disposed_While_It_Waited()
	{
		// Arrange
		TaskCompletionSource writing = new();

		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			// The first call holds the engine while it writes the file.
			fileSystem
				.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
				.Returns(writing.Task);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);
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

		processManager
			.Received(1)
			.StartProcess(Arg.Any<string>(), out Arg.Any<int>());
	}

	/// <summary>
	/// <see cref="ExecutionEngine.ExecuteAsync" />: a wait for the engine that the caller cancels starts nothing.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Returns_False_When_The_Wait_Is_Cancelled()
	{
		// Arrange
		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(processManager);
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

		processManager
			.DidNotReceive()
			.StartProcess(Arg.Any<string>(), out Arg.Any<int>());
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

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IProcessManager processManager = Substitute.For<IProcessManager>();

		FileDto dto = ItemDtoFactory.CreateFileDto(id: Guid.NewGuid());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileChangeTracker changeTracker = Substitute.For<IFileChangeTracker>();

			sandbox
				.GetFileDirectoryPath(dto.Id)
				.Returns(directoryPath);

			// The call holds the engine while it writes the file, and the dispose waits for it.
			fileSystem
				.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
				.Returns(writing.Task);

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

			changeTracker
				.TrackChangesAsync(Arg.Any<TrackChangesParameters>(), Arg.Do<CancellationToken>(x => trackerToken = x))
				.Returns(Task.CompletedTask);

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);

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

		processManager
			.Received(1)
			.KillProcess(Arg.Any<int>());

		// Once when the file is written, once when it is rolled back.
		fileSystem
			.Received(2)
			.SetFileReadOnly(Arg.Any<string>(), false);

		fileSystem
			.Received(1)
			.EraseAndDeleteFile(Arg.Any<string>());

		fileSystem
			.Received(1)
			.DeleteDirectory(directoryPath, Arg.Any<bool>());
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

		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionSandbox sandbox = Substitute.For<IExecutionSandbox>();

			IFileChangeTracker changeTracker = Substitute.For<IFileChangeTracker>();

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

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

			processManager
				.ProcessExists(Arg.Any<int>())
				.Returns(true);

			changeTracker
				.TrackChangesAsync(Arg.Any<TrackChangesParameters>(), Arg.Do<CancellationToken>(x => trackerToken = x))
				.Returns(Task.CompletedTask);

			exceptionHandler
				.When(x => x.Watch(Arg.Any<Task>()))
				.Throw(new InvalidOperationException());

			builder.RegisterInstance(sandbox);

			builder.RegisterInstance(processManager);

			builder.RegisterInstance(changeTracker);

			builder.RegisterInstance(exceptionHandler);
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

		processManager
			.Received(1)
			.KillProcess(Arg.Any<int>());
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

		IProcessManager processManager = Substitute.For<IProcessManager>();

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
				.FindApplicationByExtension(Arg.Any<string>())
				.Returns((string?)null);

			sandbox
				.GetFileDirectoryPath(Arg.Any<Guid>())
				.Returns(RandomValues.CreateDirectoryName());

			builder.RegisterInstance(appPicker);

			builder.RegisterInstance(fileAssociation);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(processManager);

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
			.DidNotReceive()
			.CreateDirectory(Arg.Any<string>());

		processManager
			.DidNotReceiveWithAnyArgs()
			.StartProcess(default!, default!, out _);

		processManager
			.DidNotReceiveWithAnyArgs()
			.StartProcess(default!, out _);
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
			.Received(1)
			.WriteAllBytesAsync(Path.Combine(directoryPath, "report_.txt"), Arg.Any<byte[]>());
	}
	#endregion
}
