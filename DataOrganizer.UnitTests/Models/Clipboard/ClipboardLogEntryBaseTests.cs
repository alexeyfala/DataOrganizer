using AwesomeAssertions;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.UnitTests.Factories;
using System.Linq;

namespace DataOrganizer.UnitTests.Models.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardLogEntryBase)}"" type")]
internal class ClipboardLogEntryBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardLogEntryBase.TypeToolTip" />: every kind of entry names itself in its own way.
	/// </summary>
	[Test]
	public void TypeToolTip_Differs_Between_Entry_Kinds()
	{
		// Arrange
		ClipboardLogEntryBase[] entries =
		[
			ClipboardEntryFactory.CreateTextEntry("a"),
			ClipboardEntryFactory.CreateUrlEntry("https://example.com"),
			ClipboardEntryFactory.CreateImageEntry([]),
			ClipboardEntryFactory.CreateFilesEntry()
		];

		// Act
		string[] tooltips = [.. entries.Select(static x => x.TypeToolTip)];

		// Assert
		tooltips
			.Should()
			.OnlyHaveUniqueItems();
	}
	#endregion
}
