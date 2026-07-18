using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;

namespace DataOrganizer.UnitTests.TestTypes.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardFileSystemEntry)}"" type")]
internal class ClipboardFileSystemEntryTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardFileSystemEntry.Name" />: a trailing separator is ignored.
	/// </summary>
	[Test]
	public void Name_Ignores_Trailing_Separator()
	{
		// Arrange
		ClipboardFileSystemEntry sut = new("C:\\dir\\sub\\", IsFolder: true);

		// Act, Assert
		sut.Name
			.Should()
			.Be("sub");
	}
	#endregion
}
