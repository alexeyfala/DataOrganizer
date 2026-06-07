using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;

namespace DataOrganizer.UnitTests.TestTypes;

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

	/// <summary>
	/// <see cref="ClipboardFileSystemEntry.Name" />: a file path yields its file name.
	/// </summary>
	[Test]
	public void Name_Of_File_Is_File_Name()
	{
		// Arrange
		ClipboardFileSystemEntry sut = new("C:\\dir\\report.txt", IsFolder: false);

		// Act, Assert
		sut.Name
			.Should()
			.Be("report.txt");
	}

	/// <summary>
	/// <see cref="ClipboardFileSystemEntry.Name" />: a directory path yields its directory name.
	/// </summary>
	[Test]
	public void Name_Of_Folder_Is_Directory_Name()
	{
		// Arrange
		ClipboardFileSystemEntry sut = new("C:\\dir\\sub", IsFolder: true);

		// Act, Assert
		sut.Name
			.Should()
			.Be("sub");
	}
	#endregion
}
