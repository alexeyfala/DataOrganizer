using AwesomeAssertions;
using DataOrganizer.Services.Text;
using System.Linq;
using TextProcessing.Helpers;

namespace DataOrganizer.UnitTests.Helpers.Text;

[TestFixture(Description = $@"Tests of ""{nameof(SyntaxLanguageProfiles)}"" type")]
internal class SyntaxLanguageProfilesTests
{
	#region Methods
	/// <summary>
	/// <see cref="SyntaxLanguageProfiles.Languages" />: every language with a profile has a grammar.
	/// </summary>
	/// <remarks>
	/// A language that a new version of the grammars renames only turns its profile off, so nothing but this test
	/// catches it.
	/// </remarks>
	[Test]
	public void Languages_Have_A_Grammar()
	{
		// Act
		string[] missing = [.. SyntaxLanguageProfiles
			.Languages
			.Where(static x => SyntaxRegistry.Instance.FindScope(x) is null)];

		// Assert
		missing
			.Should()
			.BeEmpty();
	}
	#endregion
}
