using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using NSubstitute;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ConsoleViewModel)}"" type")]
internal class ConsoleViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ConsoleViewModel.ClearCommand" />.
	/// </summary>
	[Test]
	public void ClearCommand_Does_Nothing_When_Editor_Is_Null()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Act
		Action act = () => sut.ClearCommand.Execute(null);

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// Test of <see cref="ConsoleViewModel" /> constructor.
	/// </summary>
	[Test]
	public void Constructor_Initializes_Default_Property_Values()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		// Act
		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Assert
		sut.FontSize
			.Should()
			.Be(14.0);

		sut.IsPaused
			.Should()
			.BeFalse();

		sut.IsWordWrap
			.Should()
			.BeFalse();

		sut.IsSaved
			.Should()
			.BeFalse();

		sut.WriteCallback
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// Test of <see cref="ConsoleViewModel.EditorLoadedCommand" />.
	/// </summary>
	[Test]
	public void EditorLoadedCommand_Does_Nothing_When_Editor_Is_Null()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Act
		Action act = () => sut.EditorLoadedCommand.Execute(null);

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// Test of <see cref="ConsoleViewModel.OpenAppDirectoryCommand" />.
	/// </summary>
	[Test]
	public void OpenAppDirectoryCommand_Does_Nothing_When_Process_Utils_Not_Injected()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Act
		Action act = () => sut.OpenAppDirectoryCommand.Execute(null);

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// Test of <see cref="ConsoleViewModel.OpenAppDirectoryCommand" />.
	/// </summary>
	[Test]
	public void OpenAppDirectoryCommand_Invokes_Process_Utils_When_Reference_Is_Injected()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		sut.InjectReference(processUtils);

		// Act
		sut.OpenAppDirectoryCommand.Execute(null);

		// Assert
		processUtils
			.Received()
			.OpenAppDirectory();
	}

	/// <summary>
	/// Test of <see cref="ConsoleViewModel.OpenDatabaseDirectoryCommand" />.
	/// </summary>
	[Test]
	public void OpenDatabaseDirectoryCommand_Does_Nothing_When_AppEnvironment_Not_Injected()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		sut.InjectReference(processUtils);

		// Act
		sut.OpenDatabaseDirectoryCommand.Execute(null);

		// Assert
		processUtils
			.Received(0)
			.OpenDirectory(Arg.Any<string>());
	}

	/// <summary>
	/// Test of <see cref="ConsoleViewModel.OpenDatabaseDirectoryCommand" />.
	/// </summary>
	[Test]
	public void OpenDatabaseDirectoryCommand_Opens_Database_Directory_When_References_Are_Injected()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		const string databasePath = @"C:\fake\Data\Database";

		IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

		appEnvironment
			.DatabaseDirectoryPath
			.Returns(databasePath);

		sut.InjectReference(processUtils);

		sut.InjectReference(appEnvironment);

		// Act
		sut.OpenDatabaseDirectoryCommand.Execute(null);

		// Assert
		processUtils
			.Received()
			.OpenDirectory(databasePath);
	}

	/// <summary>
	/// Test of <see cref="ConsoleViewModel.IsPaused" /> change handler.
	/// </summary>
	[Test]
	public void Setting_IsPaused_To_False_Does_Not_Throw_When_Editor_Is_Null()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		sut.IsPaused = true;

		// Act
		Action act = () => sut.IsPaused = false;

		// Assert
		act
			.Should()
			.NotThrow();

		sut.IsPaused
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="ConsoleViewModel.WriteCallback" />.
	/// </summary>
	[Test]
	public void WriteCallback_Buffers_Value_When_Editor_Is_Null_Without_Throwing()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

		// Act
		Action act = () => sut.WriteCallback("log line");

		// Assert
		act
			.Should()
			.NotThrow();
	}
	#endregion
}
