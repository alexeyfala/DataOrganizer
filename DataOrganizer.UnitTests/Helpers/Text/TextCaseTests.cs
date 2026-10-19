using AwesomeAssertions;
using DataOrganizer.Helpers.Text;
using System.Globalization;

namespace DataOrganizer.UnitTests.Helpers.Text;

[TestFixture(Description = $@"Tests of ""{nameof(TextCase)}"" type")]
internal class TextCaseTests
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
