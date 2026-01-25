using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Views;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FileSystemPathSelector)}"" type")]
internal class FileSystemPathSelectorTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="FileSystemPathSelector.ClearCommand" />.
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
	/// Test of <see cref="FileSystemPathSelector.ClearCommand" />.
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
