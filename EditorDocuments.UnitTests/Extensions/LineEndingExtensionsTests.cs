using AwesomeAssertions;
using EditorDocuments.Enums;
using EditorDocuments.Extensions;

namespace EditorDocuments.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(LineEndingExtensions)}"" type")]
internal class LineEndingExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="LineEndingExtensions.ToCaption" />: line endings of one style are named after their system and characters.
	/// </summary>
	[TestCase(LineEnding.Cr, "Macintosh (CR)")]
	[TestCase(LineEnding.CrLf, "Windows (CR LF)")]
	[TestCase(LineEnding.Lf, "Unix (LF)")]
	public void ToCaption_Names_The_Line_Break(LineEnding lineEnding, string expected)
	{
		// Act
		string? result = lineEnding.ToCaption();

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="LineEndingExtensions.ToNewLine" />: each line break style gives its own characters.
	/// </summary>
	[TestCase(LineEnding.Cr, "\r")]
	[TestCase(LineEnding.CrLf, "\r\n")]
	[TestCase(LineEnding.Lf, "\n")]
	public void ToNewLine_Gives_The_Characters_Of_The_Style(LineEnding lineEnding, string expected)
	{
		// Act
		string result = lineEnding.ToNewLine();

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion
}
