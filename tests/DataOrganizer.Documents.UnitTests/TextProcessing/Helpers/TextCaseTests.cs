using AwesomeAssertions;
using DataOrganizer.Documents.TextProcessing.Helpers;
using System.Globalization;

namespace DataOrganizer.Documents.UnitTests.TextProcessing.Helpers;

/// <summary>
/// Tests of <see cref="TextCase" /> type.
/// </summary>
[TestFixture]
internal sealed class TextCaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="TextCase.ToTitleCase" />: every word starts with a capital letter, a word in capitals included.
	/// </summary>
	[TestCase("hELLO wORLD", "Hello World")]
	[TestCase("NASA rocket", "Nasa Rocket")]
	public void ToTitleCase_Capitalizes_Every_Word(string text, string expected)
	{
		// Act
		string result = TextCase.ToTitleCase(text, CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion
}
