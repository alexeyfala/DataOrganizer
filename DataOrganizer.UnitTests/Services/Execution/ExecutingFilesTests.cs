using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Execution;
using DataOrganizer.Services.Execution;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Execution;

[TestFixture(Description = $@"Tests of ""{nameof(ExecutingFiles)}"" type")]
internal class ExecutingFilesTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExecutingFiles.Close" />: the file leaves the list, loses its mark and is closed in the engine.
	/// </summary>
	[Test]
	public async Task Close_Unmarks_The_File_And_Closes_It_In_The_Engine()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			engine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(engine);
		});

		ExecutingFiles sut = mock.Create<ExecutingFiles>();

		await sut.ExecuteAsync(CreateParameters(file));

		// Act
		sut.Close(file);

		// Assert
		sut.Files
			.Should()
			.BeEmpty();

		file.IsExecuting
			.Should()
			.BeFalse();

		await engine
			.Received(1)
			.CloseAsync(file.Id, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ExecutingFiles.CloseAllAsync" />: every executing file is closed in the engine, and the list and the
	/// marks stay as they are.
	/// </summary>
	[Test]
	public async Task CloseAllAsync_Closes_Every_Executing_File_In_The_Engine()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		FileDto other = ItemDtoFactory.CreateFileDto();

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			engine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(engine);
		});

		ExecutingFiles sut = mock.Create<ExecutingFiles>();

		await sut.ExecuteAsync(CreateParameters(file));

		await sut.ExecuteAsync(CreateParameters(other));

		// Act
		await sut.CloseAllAsync();

		// Assert
		await engine
			.Received(1)
			.CloseAsync(file.Id, Arg.Any<CancellationToken>());

		await engine
			.Received(1)
			.CloseAsync(other.Id, Arg.Any<CancellationToken>());

		sut.Files
			.Should()
			.Equal(file, other);

		file.IsExecuting
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ExecutingFiles.ExecuteAsync" />: a file the engine does not run stays out of the list and unmarked.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Leaves_A_File_That_Does_Not_Run()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionEngine engine = Substitute.For<IExecutionEngine>();

			engine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>(), Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(engine);
		});

		ExecutingFiles sut = mock.Create<ExecutingFiles>();

		// Act
		bool result = await sut.ExecuteAsync(CreateParameters(file));

		// Assert
		result
			.Should()
			.BeFalse();

		sut.Files
			.Should()
			.BeEmpty();

		file.IsExecuting
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ExecutingFiles.ExecuteAsync" />: a file the engine runs is listed and marked.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Lists_And_Marks_A_File_That_Runs()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionEngine engine = Substitute.For<IExecutionEngine>();

			engine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(engine);
		});

		ExecutingFiles sut = mock.Create<ExecutingFiles>();

		// Act
		bool result = await sut.ExecuteAsync(CreateParameters(file));

		// Assert
		result
			.Should()
			.BeTrue();

		sut.Files
			.Should()
			.Equal(file);

		file.IsExecuting
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ExecutingFiles.Receive(CloseExecutingFileMessage)" />: only the executing file the message names is
	/// removed from the list, unmarked and closed in the engine.
	/// </summary>
	[Test]
	public async Task Receive_CloseExecutingFile_Closes_Only_The_Named_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		FileDto other = ItemDtoFactory.CreateFileDto();

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		IMessenger messenger = new StrongReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			engine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(engine);

			builder.RegisterInstance(messenger);
		});

		ExecutingFiles sut = mock.Create<ExecutingFiles>();

		await sut.ExecuteAsync(CreateParameters(other));

		await sut.ExecuteAsync(CreateParameters(file));

		// Act
		messenger.Send(new CloseExecutingFileMessage(file.Id));

		// Assert
		sut.Files
			.Should()
			.Equal(other);

		file.IsExecuting
			.Should()
			.BeFalse();

		other.IsExecuting
			.Should()
			.BeTrue();

		await engine
			.Received(1)
			.CloseAsync(file.Id, Arg.Any<CancellationToken>());

		await engine
			.DidNotReceive()
			.CloseAsync(other.Id, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ExecutingFiles.Receive(CloseExecutingFileMessage)" />: a message that names no executing file closes
	/// nothing.
	/// </summary>
	[Test]
	public void Receive_CloseExecutingFile_Closes_Nothing_For_A_File_That_Does_Not_Run()
	{
		// Arrange
		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		IMessenger messenger = new StrongReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(engine);

			builder.RegisterInstance(messenger);
		});

		_ = mock.Create<ExecutingFiles>();

		// Act
		messenger.Send(new CloseExecutingFileMessage(Guid.NewGuid()));

		// Assert
		engine
			.DidNotReceiveWithAnyArgs()
			.CloseAsync(default);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates the parameters that run a file with no contents.
	/// </summary>
	private static ExecuteFileParameters CreateParameters(FileDto file) => new()
	{
		Contents = [],
		File = file,
		IsReadOnly = false
	};
	#endregion
}
