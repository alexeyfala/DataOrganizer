using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Storage;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels.Windows;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UnitTests.ViewModels.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(ConsoleViewModel)}"" type")]
internal class ConsoleViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="ConsoleViewModel.ClearCommand" />: removes the whole log.
	/// </summary>
	[AvaloniaTest]
	public void ClearCommand_Empties_The_Document()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<InlineDispatcherAccessor>()
			.As<IDispatcherAccessor>());

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		sut.WriteCallback("record");

		// Act
		sut.ClearCommand.Execute(null);

		// Assert
		sut.Document.TextLength
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="ConsoleViewModel.IsPaused" />: a record written while paused waits for the pause to end.
	/// </summary>
	[AvaloniaTest]
	public void IsPaused_Holds_A_Record_Until_It_Is_Resumed()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<InlineDispatcherAccessor>()
			.As<IDispatcherAccessor>());

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		sut.IsPaused = true;

		// Act
		sut.WriteCallback("while paused");

		// Assert
		sut.Document.Text
			.Should()
			.BeEmpty();

		sut.IsPaused = false;

		sut.Document.Text
			.Should()
			.Be("while paused");
	}

	/// <summary>
	/// <see cref="ConsoleViewModel.IsPaused" />: a record still on its way to the log when the pause begins
	/// waits with the records written after it, in the order they were written.
	/// </summary>
	[AvaloniaTest]
	public void IsPaused_Holds_A_Record_Written_Just_Before_It()
	{
		// Arrange
		List<Action> posted = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDispatcherAccessor dispatcher = Substitute.For<IDispatcherAccessor>();

			dispatcher
				.When(x => x.Post(Arg.Any<Action>(), Arg.Any<DispatcherPriority>()))
				.Do(x => posted.Add(x.Arg<Action>()));

			builder.RegisterInstance(dispatcher);
		});

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		sut.WriteCallback("before ");

		sut.IsPaused = true;

		sut.WriteCallback("while paused");

		// Act
		posted.ForEach(static x => x());

		// Assert
		sut.Document.Text
			.Should()
			.BeEmpty();

		sut.IsPaused = false;

		sut.Document.Text
			.Should()
			.Be("before while paused");
	}

	/// <summary>
	/// <see cref="ConsoleViewModel.OpenAppDataDirectoryCommand" />: opens the folder the application writes to.
	/// </summary>
	[Test]
	public void OpenAppDataDirectoryCommand_Opens_The_Application_Data_Directory()
	{
		// Arrange
		const string directoryPath = @"C:\Data\DataOrganizer";

		IDirectoryAccessor directoryAccessor = Substitute.For<IDirectoryAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.AppDataDirectoryPath
				.Returns(directoryPath);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(directoryAccessor);
		});

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Act
		sut.OpenAppDataDirectoryCommand.Execute(null);

		// Assert
		directoryAccessor
			.Received(1)
			.OpenDirectory(directoryPath, Arg.Any<ILogger?>());
	}

	/// <summary>
	/// <see cref="ConsoleViewModel.OpenAppDirectoryCommand" />: opens the folder the application runs from.
	/// </summary>
	[Test]
	public void OpenAppDirectoryCommand_Opens_The_Application_Directory()
	{
		// Arrange
		IDirectoryAccessor directoryAccessor = Substitute.For<IDirectoryAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(directoryAccessor));

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Act
		sut.OpenAppDirectoryCommand.Execute(null);

		// Assert
		directoryAccessor
			.Received(1)
			.OpenAppDirectory(Arg.Any<ILogger?>());
	}

	/// <summary>
	/// <see cref="ConsoleViewModel.RemoveStartLines" />: a longer document keeps only its last lines.
	/// </summary>
	[AvaloniaTest]
	public void RemoveStartLines_Keeps_The_Last_Lines()
	{
		// Arrange
		TextDocument document = new(CreateText(lineCount: 5));

		// Act
		ConsoleViewModel.RemoveStartLines(document, maxLines: 3);

		// Assert
		document.Text
			.Should()
			.Be("3\n4\n5");
	}

	/// <summary>
	/// <see cref="ConsoleViewModel.RemoveStartLines" />: a document within the limit stays as it is.
	/// </summary>
	[AvaloniaTest]
	public void RemoveStartLines_Leaves_A_Document_Within_The_Limit([Values(2, 3)] int lineCount)
	{
		// Arrange
		string text = CreateText(lineCount);

		TextDocument document = new(text);

		// Act
		ConsoleViewModel.RemoveStartLines(document, maxLines: 3);

		// Assert
		document.Text
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="ConsoleViewModel.WriteCallback" />: the records reach the log in the order they are written.
	/// </summary>
	[AvaloniaTest]
	public void WriteCallback_Appends_The_Records_In_Order()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<InlineDispatcherAccessor>()
			.As<IDispatcherAccessor>());

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Act
		sut.WriteCallback("first ");

		sut.WriteCallback("second ");

		// Assert
		sut.Document.Text
			.Should()
			.Be("first second ");
	}

	/// <summary>
	/// <see cref="ConsoleViewModel.WriteCallback" />: the log keeps no undo history of its records.
	/// </summary>
	[AvaloniaTest]
	public void WriteCallback_Leaves_Nothing_To_Undo()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<InlineDispatcherAccessor>()
			.As<IDispatcherAccessor>());

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Act
		sut.WriteCallback("record");

		// Assert
		sut.Document.UndoStack.CanUndo
			.Should()
			.BeFalse();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a text of lines numbered from one.
	/// </summary>
	private static string CreateText(int lineCount) => string.Join('\n', Enumerable.Range(1, lineCount));
	#endregion
}
