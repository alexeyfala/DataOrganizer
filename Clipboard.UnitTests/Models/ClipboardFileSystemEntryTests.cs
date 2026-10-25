using AwesomeAssertions;
using Clipboard.Models;
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
	#endregion
}
