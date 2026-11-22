using AwesomeAssertions;
using DataOrganizer.Shared.Helpers;
using DataOrganizer.Shared.Properties;
using System.Globalization;

namespace DataOrganizer.Shared.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(QuoteBuilder)}"" type")]
internal class QuoteBuilderTests
{
	#region Data
	/// <summary>
	/// Culture of the resources captured before a test to be restored afterwards.
	/// </summary>
	private CultureInfo? _resourceCulture;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="QuoteBuilder.Build" />: Russian puts the text in «», every other language in straight quotes.
	/// </summary>
	[TestCase("ru-RU", "«Name»")]
	[TestCase("en-US", @"""Name""")]
	[TestCase("de-DE", @"""Name""")]
	public void Build_Puts_The_Text_In_The_Quotes_Of_The_Language(string language, string expected)
	{
		// Arrange
		Strings.Culture = CultureInfo.GetCultureInfo(language);

		// Act
		string result = QuoteBuilder.Build("Name");

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// Remembers the culture of the resources, which a test replaces.
	/// </summary>
	[SetUp]
	public void RememberCulture() => _resourceCulture = Strings.Culture;

	/// <summary>
	/// Puts the culture of the resources back, so that a change does not reach the tests that follow.
	/// </summary>
	[TearDown]
	public void RestoreCulture() => Strings.Culture = _resourceCulture;
	#endregion
}
