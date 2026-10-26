using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Services.Documents;
using DataOrganizer.ViewModels.Windows;
using Diagnostics.Interfaces;
using Diagnostics.Services;
using EditorDocuments.Interfaces;
using FileSystemOperations.Interfaces;
using NSubstitute;
using System;
using System.Collections.Generic;
using TestSupport.Common;
using Threading.Enums;
using Threading.Interfaces;
using Threading.UnitTests.Fakes;

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
		ConsoleLog consoleLog = new();

		AvaloniaLogText logText = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(consoleLog)
				.As<IConsoleLog>();

			builder
				.RegisterInstance(logText)
				.As<ILogText>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		consoleLog.Write("record");

		// Act
		sut.ClearCommand.Execute(null);

		// Assert
		logText.Document.TextLength
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="ConsoleViewModel.Document" />: records of whole lines keep every line in the log.
	/// </summary>
	[AvaloniaTest]
	public void Document_Keeps_Every_Line_Of_The_Records()
	{
		// Arrange
		ConsoleLog consoleLog = new();

		AvaloniaLogText logText = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(consoleLog)
				.As<IConsoleLog>();

			builder
				.RegisterInstance(logText)
				.As<ILogText>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Act
		consoleLog.Write("first\n");

		consoleLog.Write("second\n");

		// Assert
		logText.Document.Text
			.Should()
			.Be("first\nsecond\n");
	}

	/// <summary>
	/// <see cref="ConsoleViewModel.Document" />: the records of the console log reach the log in the order they are
	/// written.
	/// </summary>
	[AvaloniaTest]
	public void Document_Takes_The_Records_In_The_Order_Written()
	{
		// Arrange
		ConsoleLog consoleLog = new();

		AvaloniaLogText logText = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(consoleLog)
				.As<IConsoleLog>();

			builder
				.RegisterInstance(logText)
				.As<ILogText>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Act
		consoleLog.Write("first ");

		consoleLog.Write("second ");

		// Assert
		logText.Document.Text
			.Should()
			.Be("first second ");
	}

	/// <summary>
	/// <see cref="ConsoleViewModel.Document" />: the records written before the console opened reach the log too.
	/// </summary>
	[AvaloniaTest]
	public void Document_Takes_The_Records_Written_Before_The_Console()
	{
		// Arrange
		ConsoleLog consoleLog = new();

		AvaloniaLogText logText = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(consoleLog)
				.As<IConsoleLog>();

			builder
				.RegisterInstance(logText)
				.As<ILogText>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		consoleLog.Write("before");

		// Act
		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Assert
		logText.Document.Text
			.Should()
			.Be("before");
	}

	/// <summary>
	/// <see cref="ConsoleViewModel.IsPaused" />: a record written while paused waits for the pause to end.
	/// </summary>
	[AvaloniaTest]
	public void IsPaused_Holds_A_Record_Until_It_Is_Resumed()
	{
		// Arrange
		ConsoleLog consoleLog = new();

		AvaloniaLogText logText = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(consoleLog)
				.As<IConsoleLog>();

			builder
				.RegisterInstance(logText)
				.As<ILogText>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		sut.IsPaused = true;

		// Act
		consoleLog.Write("while paused");

		// Assert
		logText.Document.Text
			.Should()
			.BeEmpty();

		sut.IsPaused = false;

		logText.Document.Text
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

		ConsoleLog consoleLog = new();

		AvaloniaLogText logText = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDispatcherAccessor dispatcher = Substitute.For<IDispatcherAccessor>();

			dispatcher
				.When(x => x.Post(Arg.Any<Action>(), Arg.Any<UiPriority>()))
				.Do(x => posted.Add(x.Arg<Action>()));

			builder
				.RegisterInstance(consoleLog)
				.As<IConsoleLog>();

			builder
				.RegisterInstance(logText)
				.As<ILogText>();

			builder.RegisterInstance(dispatcher);
		});

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		consoleLog.Write("before ");

		sut.IsPaused = true;

		consoleLog.Write("while paused");

		// Act
		posted.ForEach(static x => x());

		// Assert
		logText.Document.Text
			.Should()
			.BeEmpty();

		sut.IsPaused = false;

		logText.Document.Text
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
		string directoryPath = TestPath.Combine("Data", "DataOrganizer");

		IDirectoryAccessor directoryAccessor = Substitute.For<IDirectoryAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.AppDataDirectoryPath
				.Returns(directoryPath);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterType<ConsoleLog>()
				.As<IConsoleLog>();

			builder.RegisterInstance(directoryAccessor);
		});

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Act
		sut.OpenAppDataDirectoryCommand.Execute(null);

		// Assert
		directoryAccessor
			.Received(1)
			.OpenDirectory(directoryPath);
	}

	/// <summary>
	/// <see cref="ConsoleViewModel.OpenAppDirectoryCommand" />: opens the folder the application runs from.
	/// </summary>
	[Test]
	public void OpenAppDirectoryCommand_Opens_The_Application_Directory()
	{
		// Arrange
		IDirectoryAccessor directoryAccessor = Substitute.For<IDirectoryAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ConsoleLog>()
				.As<IConsoleLog>();

			builder.RegisterInstance(directoryAccessor);
		});

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Act
		sut.OpenAppDirectoryCommand.Execute(null);

		// Assert
		directoryAccessor
			.Received(1)
			.OpenAppDirectory();
	}
	#endregion
}
