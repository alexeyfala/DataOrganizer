using AwesomeAssertions;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.UnitTests.Factories;

namespace DataOrganizer.UnitTests.Models.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardUrlEntry)}"" type")]
internal class ClipboardUrlEntryTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardUrlEntry.IsUrl" />: a link entry exposes an openable URL.
	/// </summary>
	[Test]
	public void IsUrl_Is_True_For_A_Link()
	{
		// Arrange
		ClipboardUrlEntry sut = ClipboardEntryFactory.CreateUrlEntry("https://example.com");

		// Act, Assert
		sut.IsUrl
			.Should()
			.BeTrue();
	}
	#endregion
}
