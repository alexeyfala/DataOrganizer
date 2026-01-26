using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Views;
using Shared.Common;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FileSystemPathSelector)}"" type")]
internal class FileSystemPathSelectorTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="FileSystemPathSelector.ClearCommand" />.
	/// </summary>
	[AvaloniaTest]
	public void ClearCommand_Can_Execute_When_Path_Is_Set()
	{
		// Arrange
		FileSystemPathSelector sut = new()
		{
			// Act
			Path = AppUtils.CreateRandomFileName(10)
		};

		// Assert
		sut.ClearCommand.CanExecute(null)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="FileSystemPathSelector.ClearCommand" />.
	/// </summary>
	[AvaloniaTest]
	public void ClearCommand_Cannot_Execute_After_Path_Has_Been_Cleared()
	{
		// Arrange
		FileSystemPathSelector sut = new()
		{
			Path = AppUtils.CreateRandomFileName(10)
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
