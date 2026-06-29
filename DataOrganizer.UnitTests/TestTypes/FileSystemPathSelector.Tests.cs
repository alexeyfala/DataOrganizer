using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Views;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FileSystemPathSelector)}"" type")]
internal class FileSystemPathSelectorTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileSystemPathSelector.ClearCommand" />: the command can execute when a non-empty path is set.
	/// </summary>
	[AvaloniaTest]
	public void ClearCommand_Can_Execute_When_Path_Is_Set()
	{
		// Arrange
		FileSystemPathSelector sut = new()
		{
			// Act
			Path = TestUtils.CreateRandomFileName(10)
		};

		// Assert
		sut.ClearCommand.CanExecute(null)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FileSystemPathSelector.ClearCommand" />: the command cannot execute after it has cleared the path.
	/// </summary>
	[AvaloniaTest]
	public void ClearCommand_Cannot_Execute_After_Path_Has_Been_Cleared()
	{
		// Arrange
		FileSystemPathSelector sut = new()
		{
			Path = TestUtils.CreateRandomFileName(10)
		};

		// Act
		sut
			.ClearCommand
			.Execute(null);

		// Assert
		sut.ClearCommand.CanExecute(null)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileSystemPathSelector.ClearCommand" />: the command cannot execute when the path is an empty string.
	/// </summary>
	[AvaloniaTest]
	public void ClearCommand_Cannot_Execute_When_Path_Is_Empty_String()
	{
		// Arrange
		FileSystemPathSelector sut = new()
		{
			// Act
			Path = string.Empty
		};

		// Assert
		sut.ClearCommand.CanExecute(null)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FileSystemPathSelector.ClearCommand" />: the command cannot execute when the path is null.
	/// </summary>
	[AvaloniaTest]
	public void ClearCommand_Cannot_Execute_When_Path_Is_Null()
	{
		// Arrange
		FileSystemPathSelector sut = new()
		{
			// Act
			Path = null
		};

		// Assert
		sut.ClearCommand.CanExecute(null)
			.Should()
			.BeFalse();
	}
	#endregion
}
