using AwesomeAssertions;
using DataOrganizer.SystemIntegration.Clipboard.Models;
using DataOrganizer.SystemIntegration.UnitTests.Clipboard.TestHelpers.Factories;

namespace DataOrganizer.SystemIntegration.UnitTests.Clipboard.Models;

/// <summary>
/// Tests of <see cref="ClipboardUrlEntry" /> type.
/// </summary>
[TestFixture]
internal sealed class ClipboardUrlEntryTests
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
