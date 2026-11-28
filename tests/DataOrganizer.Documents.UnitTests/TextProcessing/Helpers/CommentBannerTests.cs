using AwesomeAssertions;
using DataOrganizer.Documents.TextProcessing.Helpers;
using DataOrganizer.Shared.Constants;

namespace DataOrganizer.Documents.UnitTests.TextProcessing.Helpers;

/// <summary>
/// Tests of <see cref="CommentBanner" /> type.
/// </summary>
[TestFixture]
internal sealed class CommentBannerTests
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
	#endregion
}
