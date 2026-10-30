using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DialogContracts.Interfaces;
using Entities.Enums;
using FileContents.Interfaces;
using FileExecution.Dto;
using FileExecution.Interfaces;
using FileExecution.Services;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace FileExecution.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(FileExecutionScenario)}"" type")]
internal class FileExecutionScenarioTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileExecutionScenario.ConfirmAsync" />: a document is let through without a question.
	/// </summary>
	[Test]
	public async Task ConfirmAsync_Accepts_A_Document_Without_A_Question()
	{
		// Arrange
		IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(confirmationPrompt));

		FileExecutionScenario sut = mock.Create<FileExecutionScenario>();

		FileDto file = ItemDtoFactory.CreateFileDto();

		file.Name = "notes.txt";

		// Act
		bool result = await sut.ConfirmAsync(file);

		// Assert
		result
			.Should()
			.BeTrue();

		await confirmationPrompt
			.DidNotReceive()
			.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileExecutionScenario.ConfirmAsync" />: a file that may run a program is let through only when the user
	/// says yes.
	/// </summary>
	[Test]
	public async Task ConfirmAsync_Asks_Before_A_File_That_May_Run_A_Program([Values] bool isConfirmed)
	{
		// Arrange
		IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(isConfirmed);

			builder.RegisterInstance(confirmationPrompt);
		});

		FileExecutionScenario sut = mock.Create<FileExecutionScenario>();

		FileDto file = ItemDtoFactory.CreateFileDto();

		file.Name = "setup.exe";

		// Act
		bool result = await sut.ConfirmAsync(file);

		// Assert
		result
			.Should()
			.Be(isConfirmed);

		await confirmationPrompt
			.Received(1)
			.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileExecutionScenario.ConfirmAsync" />: a file that is already running is refused without a question.
	/// </summary>
	[Test]
	public async Task ConfirmAsync_Refuses_A_File_That_Is_Already_Executing()
	{
		// Arrange
		IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutingFiles executingFiles = Substitute.For<IExecutingFiles>();

			executingFiles
				.IsExecuting(file.Id)
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(executingFiles);
		});

		FileExecutionScenario sut = mock.Create<FileExecutionScenario>();

		file.Name = "setup.exe";

		// Act
		bool result = await sut.ConfirmAsync(file);

		// Assert
		result
			.Should()
			.BeFalse();

		await confirmationPrompt
			.DidNotReceive()
			.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileExecutionScenario.ExecuteAsync" />: the contents read for the file are run, with the keeper whose key
	/// opened them, so that the changes are encrypted by it.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Executes_The_Contents_With_The_Keeper_Of_The_File([Values] bool isReadOnly)
	{
		// Arrange
		byte[] contents = RandomValues.CreateBytes(16);

		FolderDto keeper = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		file.Parent = keeper;

		IExecutingFiles executingFiles = Substitute.For<IExecutingFiles>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsOpener fileContentsOpener = Substitute.For<IFileContentsOpener>();

			fileContentsOpener
				.OpenAsync(file.Id, keeper.Id, file.Name, file.Kind)
				.Returns(contents);

			builder.RegisterInstance(executingFiles);

			builder.RegisterInstance(fileContentsOpener);
		});

		FileExecutionScenario sut = mock.Create<FileExecutionScenario>();

		// Act
		await sut.ExecuteAsync(file, isReadOnly);

		// Assert
		await executingFiles.Received(1).ExecuteAsync(
			Arg.Is<ExecuteFileParameters>(x => x.Contents == contents
				&& x.File == file
				&& x.IsReadOnly == isReadOnly
				&& x.KeeperId == keeper.Id),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileExecutionScenario.ExecuteAsync" />: a file whose contents cannot be read is not run.
	/// </summary>
	[Test]
	public async Task ExecuteAsync_Executes_Nothing_Without_Contents()
	{
		// Arrange
		IExecutingFiles executingFiles = Substitute.For<IExecutingFiles>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsOpener fileContentsOpener = Substitute.For<IFileContentsOpener>();

			// The contents cannot be read or opened.
			fileContentsOpener
				.OpenAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<EntityKind>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(executingFiles);

			builder.RegisterInstance(fileContentsOpener);
		});

		FileExecutionScenario sut = mock.Create<FileExecutionScenario>();

		// Act
		await sut.ExecuteAsync(ItemDtoFactory.CreateFileDto(), isReadOnly: false);

		// Assert
		await executingFiles
			.DidNotReceive()
			.ExecuteAsync(Arg.Any<ExecuteFileParameters>(), Arg.Any<CancellationToken>());
	}
	#endregion
}
