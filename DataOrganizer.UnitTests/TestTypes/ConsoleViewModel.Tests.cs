using Autofac;
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
	/// Test of <see cref="ConsoleViewModel.ClearCommand" />: does not throw when the editor is null.
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
	/// Test of <see cref="ConsoleViewModel.EditorLoadedCommand" />: does not throw when the editor is null.
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
	/// Test of <see cref="ConsoleViewModel.OpenAppDirectoryCommand" />: does not throw with default substitutes.
	/// </summary>
	[Test]
	public void OpenAppDirectoryCommand_Does_Not_Throw_With_Default_Substitute()
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
	/// Test of <see cref="ConsoleViewModel.OpenAppDirectoryCommand" />: invokes process utils to open the app directory.
	/// </summary>
	[Test]
	public void OpenAppDirectoryCommand_Invokes_Process_Utils_When_Reference_Is_Injected()
	{
		// Arrange
		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		using AutoMock mock = AutoMock.GetLoose();

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>(TypedParameter.From(processUtils));

		// Act
		sut.OpenAppDirectoryCommand.Execute(null);

		// Assert
		processUtils
			.Received()
			.OpenAppDirectory();
	}

	/// <summary>
	/// Test of <see cref="ConsoleViewModel.OpenDatabaseDirectoryCommand" />: opens the configured database directory.
	/// </summary>
	[Test]
	public void OpenDatabaseDirectoryCommand_Opens_Database_Directory_When_References_Are_Injected()
	{
		// Arrange
		const string databasePath = @"C:\fake\Data\Database";

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.DatabaseDirectoryPath
				.Returns(databasePath);

			builder.RegisterInstance(processUtils);

			builder.RegisterInstance(appEnvironment);
		});

		ConsoleViewModel sut = mock.Create<ConsoleViewModel>();

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
	/// Test of <see cref="ConsoleViewModel.WriteCallback" />: buffers the value without throwing when the editor is null.
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
