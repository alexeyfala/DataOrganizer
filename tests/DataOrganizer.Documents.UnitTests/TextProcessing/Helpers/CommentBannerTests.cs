using AwesomeAssertions;
using DataOrganizer.Documents.TextProcessing.Helpers;
using DataOrganizer.Shared.Constants;

namespace DataOrganizer.Documents.UnitTests.TextProcessing.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(CommentBanner)}"" type")]
internal class CommentBannerTests
{
	#region Methods
	/// <summary>
	/// <see cref="CommentBanner.Create" />: the blanks at the end of the line stay out of the banner.
	/// </summary>
	[Test]
	public void Create_Drops_The_Blanks_At_The_End_Of_The_Line()
	{
		// Act
		string banner = CommentBanner.Create("// a \t ");

		// Assert
		banner
			.Should()
			.Be($"// a {Glyphs.ThreeDots}");
	}

	/// <summary>
	/// <see cref="CommentBanner.Create" />: a token with no text after it on its line shows alone before the dots.
	/// </summary>
	[Test]
	public void Create_Puts_The_Dots_After_A_Lone_Token()
	{
		// Act
		string banner = CommentBanner.Create("<!--");

		// Assert
		banner
			.Should()
			.Be($"<!-- {Glyphs.ThreeDots}");
	}
	#endregion
}
