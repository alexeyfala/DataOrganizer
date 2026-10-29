using AwesomeAssertions;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;

namespace Hierarchy.UnitTests.Dto;

[TestFixture(Description = $@"Tests of ""{nameof(FileDto)}"" type")]
internal class FileDtoTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileDto.IsOpened" />: a file is open while it is edited or executed.
	/// </summary>
	[TestCase(false, false, false)]
	[TestCase(false, true, true)]
	[TestCase(true, false, true)]
	public void IsOpened_Tells_Whether_The_File_Is_Edited_Or_Executed(bool isEditing, bool isExecuting, bool expected)
	{
		// Arrange
		FileDto sut = ItemDtoFactory.CreateFileDto(isEditing: isEditing, isExecuting: isExecuting);

		// Act
		bool result = sut.IsOpened();

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion
}
