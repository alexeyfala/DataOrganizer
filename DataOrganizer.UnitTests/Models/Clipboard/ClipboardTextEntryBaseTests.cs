using AwesomeAssertions;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.UnitTests.Factories;

namespace DataOrganizer.UnitTests.Models.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardTextEntryBase)}"" type")]
internal class ClipboardTextEntryBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardTextEntryBase.IsHtml" />, <see cref="ClipboardTextEntryBase.IsRtf" />: plain text carries no companion format.
	/// </summary>
	[Test]
	public void IsHtml_And_IsRtf_Are_False_For_Plain_Text()
	{
		// Arrange
		ClipboardTextEntry sut = ClipboardEntryFactory.CreateTextEntry("plain");

		// Act, Assert
		sut.IsHtml
			.Should()
			.BeFalse();

		sut.IsRtf
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardTextEntryBase.IsHtml" />, <see cref="ClipboardTextEntryBase.IsRtf" />: both companion formats are reported.
	/// </summary>
	[Test]
	public void IsHtml_And_IsRtf_Reflect_Companion_Formats()
	{
		// Arrange
		ClipboardTextEntry sut = ClipboardEntryFactory.CreateTextEntry("x", html: "<b>x</b>", rtf: @"{\rtf1 x}");

		// Act, Assert
		sut.IsHtml
			.Should()
			.BeTrue();

		sut.IsRtf
			.Should()
			.BeTrue();
	}
	#endregion
}
