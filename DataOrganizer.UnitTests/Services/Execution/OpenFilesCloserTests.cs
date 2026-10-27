using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Services.Execution;
using DialogContracts.Interfaces;
using EditorMessages.Messages;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Threading.Interfaces;
using Threading.UnitTests.Fakes;

namespace DataOrganizer.UnitTests.Services.Execution;

[TestFixture(Description = $@"Tests of ""{nameof(OpenFilesCloser)}"" type")]
internal class OpenFilesCloserTests
{
	#region Methods
	/// <summary>
	/// <see cref="OpenFilesCloser.Close" />: no question is asked.
	/// </summary>
	[Test]
	public void Close_Asks_Nothing()
	{
		// Arrange
		IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(confirmationPrompt));

		OpenFilesCloser sut = mock.Create<OpenFilesCloser>();

		// Act
		sut.Close([.. ItemDtoFactory.CreateFileDtos(1, isExecuting: true)]);

		// Assert
		confirmationPrompt
			.ReceivedCalls()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="OpenFilesCloser.Close" />: every edited file is asked to close in the editor, and every executing file is
	/// closed in the operating system.
	/// </summary>
	[Test]
	public void Close_Closes_Editing_And_Executing_Files()
	{
		// Arrange
		FileDto[] editingFiles = [.. ItemDtoFactory.CreateFileDtos(2, isEditing: true)];

		FileDto[] executingFiles = [.. ItemDtoFactory.CreateFileDtos(2, isExecuting: true)];

		IMessenger messenger = new StrongReferenceMessenger();

		List<FileDto> closedInEditor = Capture(messenger);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(messenger);

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();
		});

		OpenFilesCloser sut = mock.Create<OpenFilesCloser>();

		// Act
		sut.Close([.. editingFiles, .. executingFiles]);

		// Assert
		closedInEditor
			.Should()
			.Equal(editingFiles);

		executingFiles
			.Should()
			.OnlyContain(x => !x.IsExecuting);
	}

	/// <summary>
	/// <see cref="OpenFilesCloser.TryCloseAsync" />: no open files need no question.
	/// </summary>
	[Test]
	public async Task TryCloseAsync_Asks_Nothing_Without_Open_Files()
	{
		// Arrange
		IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(confirmationPrompt));

		OpenFilesCloser sut = mock.Create<OpenFilesCloser>();

		// Act
		bool result = await sut.TryCloseAsync([]);

		// Assert
		result
			.Should()
			.BeTrue();

		await confirmationPrompt
			.DidNotReceiveWithAnyArgs()
			.RequestYesCancelAsync(default!);
	}

	/// <summary>
	/// <see cref="OpenFilesCloser.TryCloseAsync" />: the open files are closed once the user agrees, and stay open when
	/// the user refuses.
	/// </summary>
	[Test]
	public async Task TryCloseAsync_Closes_The_Files_Only_When_The_User_Agrees([Values] bool isAgreed)
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2, isEditing: true)];

		IMessenger messenger = new StrongReferenceMessenger();

		List<FileDto> closedInEditor = Capture(messenger);

		IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(isAgreed);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(messenger);
		});

		OpenFilesCloser sut = mock.Create<OpenFilesCloser>();

		// Act
		bool result = await sut.TryCloseAsync(files);

		// Assert
		result
			.Should()
			.Be(isAgreed);

		closedInEditor
			.Should()
			.Equal(isAgreed ? files : []);

		await confirmationPrompt
			.Received(1)
			.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Collects the files the editor is asked to close.
	/// </summary>
	private static List<FileDto> Capture(IMessenger messenger)
	{
		List<FileDto> closed = [];

		messenger.Register<List<FileDto>, CloseEditingFileMessage>(
			closed,
			static (recipient, message) => recipient.Add(message.File));

		return closed;
	}
	#endregion
}
