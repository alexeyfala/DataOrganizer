using AwesomeAssertions;
using Clipboard.Models;
using System;
using System.IO;
using TestSupport.Common;

namespace Clipboard.UnitTests.Models;

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
		string path = TestPath.Combine("dir", "sub") + Path.DirectorySeparatorChar;

		ClipboardFileSystemEntry sut = new(path, IsFolder: true);

		// Act, Assert
		sut.Name
			.Should()
			.Be("sub");
	}

	/// <summary>
	/// <see cref="ClipboardFileSystemEntry.Name" />: the root of a Unix file system, which trims down to nothing, keeps
	/// its path.
	/// </summary>
	[Test]
	public void Name_Keeps_The_Root_Of_A_Unix_File_System()
	{
		// Arrange
		ClipboardFileSystemEntry sut = new("/", IsFolder: true);

		// Act, Assert
		sut.Name
			.Should()
			.Be("/");
	}

	/// <summary>
	/// <see cref="ClipboardFileSystemEntry.Name" />: the root of a Windows drive is named by its drive.
	/// </summary>
	[Test]
	public void Name_Of_A_Drive_Root_Is_The_Drive()
	{
		// Arrange
		ClipboardFileSystemEntry sut = new(@"C:\", IsFolder: true);

		// Act, Assert
		sut.Name
			.Should()
			.Be(OperatingSystem.IsWindows() ? "C:" : @"C:\");
	}
	#endregion
}
