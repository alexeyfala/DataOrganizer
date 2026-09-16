using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using AvaloniaEdit;
using AwesomeAssertions;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Storage;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels.Windows;
using NSubstitute;
using Serilog;

namespace DataOrganizer.UnitTests.ViewModels.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(ConsoleViewModel)}"" type")]
internal class ConsoleViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="ConsoleViewModel.EditorLoadedCommand" />: the records written before an editor arrived
	/// are held back and reach it once it does.
	/// </summary>
	[AvaloniaTest]
	public void EditorLoadedCommand_Writes_The_Records_Buffered_Without_An_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<InlineDispatcherAccessor>()
			.As<IDispatcherAccessor>());

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		sut.WriteCallback("first ");

		sut.WriteCallback("second ");

		TextEditor editor = new();

		// Act
		sut.EditorLoadedCommand.Execute(editor);

		// Assert
		editor.Text
			.Should()
			.Be("first second ");
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
	/// <see cref="ConsoleViewModel.IsPaused" />: a record written while paused waits for the pause to end.
	/// </summary>
	[AvaloniaTest]
	public void Records_Written_While_Paused_Reach_The_Editor_Once_It_Is_Resumed()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<InlineDispatcherAccessor>()
			.As<IDispatcherAccessor>());

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		TextEditor editor = new();

		sut.EditorLoadedCommand.Execute(editor);

		sut.IsPaused = true;

		// Act
		sut.WriteCallback("while paused");

		// Assert
		editor.Text
			.Should()
			.BeEmpty();

		sut.IsPaused = false;

		editor.Text
			.Should()
			.Be("while paused");
	}
	#endregion
}
