using AwesomeAssertions;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions.Documents;

namespace DataOrganizer.UnitTests.Extensions.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(LineEndingExtensions)}"" type")]
internal class LineEndingExtensionsTests
{
	#region Methods
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
