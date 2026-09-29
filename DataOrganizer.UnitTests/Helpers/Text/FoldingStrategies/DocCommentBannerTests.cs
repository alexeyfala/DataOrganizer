using AwesomeAssertions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Text.FoldingStrategies;

namespace DataOrganizer.UnitTests.Helpers.Text.FoldingStrategies;

[TestFixture(Description = $@"Tests of ""{nameof(DocCommentBanner)}"" type")]
internal class DocCommentBannerTests
{
	#region Data
	/// <summary>
	/// Token of a documentation comment of C#.
	/// </summary>
	private const string Token = "///";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DocCommentBanner.Create" />: the tokens that start the lines are left out, and the blanks of the summary
	/// collapse into one space, with one after the tag.
	/// </summary>
	[TestCase("/// <summary>\n/// Text.\n/// </summary>", "/// <summary> Text.")]
	[TestCase("/// <summary>Text.</summary>", "/// <summary> Text.")]
	[TestCase("/// <summary>  a \t\r\n    ///   b\n    /// </summary>", "/// <summary> a b")]
	public void Create_Collapses_The_Blanks_Of_The_Summary(string comment, string expected)
	{
		// Act
		string? banner = DocCommentBanner.Create(Token, comment);

		// Assert
		banner
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocCommentBanner.Create" />: a banner longer than the most characters is cut there, without a blank at
	/// the cut, and dots follow it, also when the text goes on after a blank.
	/// </summary>
	[TestCase(70, "", 66)]
	[TestCase(66, " b", 66)]
	[TestCase(65, " b", 65)]
	public void Create_Cuts_A_Long_Banner_After_The_Most_Characters(int count, string tail, int keptCount)
	{
		// Act
		string? banner = DocCommentBanner.Create(Token, $"/// <summary>{new string('a', count)}{tail}</summary>");

		// Assert
		banner
			.Should()
			.Be($"/// <summary> {new string('a', keptCount)} {Glyphs.ThreeDots}");
	}

	/// <summary>
	/// <see cref="DocCommentBanner.Create" />: the entities of XML show as the characters they stand for, and a blank among
	/// them collapses as well.
	/// </summary>
	[TestCase("/// <summary>&lt;T&gt; &amp; &quot;q&quot; &apos;a&apos;</summary>", "/// <summary> <T> & \"q\" 'a'")]
	[TestCase("/// <summary>&#65;&#x42;&#x0a;C</summary>", "/// <summary> AB C")]
	public void Create_Decodes_The_Entities(string comment, string expected)
	{
		// Act
		string? banner = DocCommentBanner.Create(Token, comment);

		// Assert
		banner
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocCommentBanner.Create" />: the summary is found among the other tags of the comment, also with
	/// attributes of its own.
	/// </summary>
	[TestCase("/// <remarks>R</remarks>\n/// <summary>S</summary>")]
	[TestCase("/// <summary xml:lang=\"en\">S</summary>\n/// <returns>R</returns>")]
	public void Create_Finds_The_Summary_Among_Other_Tags(string comment)
	{
		// Act
		string? banner = DocCommentBanner.Create(Token, comment);

		// Assert
		banner
			.Should()
			.Be("/// <summary> S");
	}

	/// <summary>
	/// <see cref="DocCommentBanner.Create" />: a banner of the most characters is kept whole, even with blanks after it.
	/// </summary>
	[TestCase("")]
	[TestCase("  \n/// ")]
	public void Create_Keeps_A_Banner_Of_The_Most_Characters_Whole(string tail)
	{
		// Arrange
		string text = new('a', 66);

		// Act
		string? banner = DocCommentBanner.Create(Token, $"/// <summary>{text}{tail}</summary>");

		// Assert
		banner
			.Should()
			.Be($"/// <summary> {text}");
	}

	/// <summary>
	/// <see cref="DocCommentBanner.Create" />: a bracket before a blank, a name without its semicolon or with no meaning,
	/// and a number of no character stay text as they are.
	/// </summary>
	[TestCase("/// <summary>a < b</summary>", "/// <summary> a < b")]
	[TestCase("/// <summary>a &nbsp; &amp b</summary>", "/// <summary> a &nbsp; &amp b")]
	[TestCase("/// <summary>&#xD800;</summary>", "/// <summary> &#xD800;")]
	public void Create_Keeps_Text_That_Only_Looks_Like_Markup(string comment, string expected)
	{
		// Act
		string? banner = DocCommentBanner.Create(Token, comment);

		// Assert
		banner
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocCommentBanner.Create" />: the tags of the elements in the summary and its notes are left out, and the
	/// text of the elements stays.
	/// </summary>
	[TestCase("/// <summary>For <c>null</c> p.</summary>", "/// <summary> For null p.")]
	[TestCase("/// <summary>A <see href=\"u\">link</see>.</summary>", "/// <summary> A link.")]
	[TestCase("/// <summary><para>A</para>\n/// <!-- note -->B</summary>", "/// <summary> A B")]
	public void Create_Leaves_The_Tags_Out(string comment, string expected)
	{
		// Act
		string? banner = DocCommentBanner.Create(Token, comment);

		// Assert
		banner
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocCommentBanner.Create" />: a summary without its end tag runs to the end of the comment.
	/// </summary>
	[Test]
	public void Create_Reads_A_Summary_That_Stays_Open_To_The_End()
	{
		// Act
		string? banner = DocCommentBanner.Create(Token, "/// <summary>\n/// S\n/// T");

		// Assert
		banner
			.Should()
			.Be("/// <summary> S T");
	}

	/// <summary>
	/// <see cref="DocCommentBanner.Create" />: a comment without a summary, or with a tag whose name only starts like it,
	/// has no banner.
	/// </summary>
	[TestCase("/// <param name=\"x\">X</param>")]
	[TestCase("/// Adds two numbers.")]
	[TestCase("/// <summaryx>S</summaryx>")]
	[TestCase("/// <summary")]
	public void Create_Returns_Null_Without_A_Summary(string comment)
	{
		// Act
		string? banner = DocCommentBanner.Create(Token, comment);

		// Assert
		banner
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocCommentBanner.Create" />: a summary without text, or whose start tag stays open, shows the tag alone.
	/// </summary>
	[TestCase("/// <summary></summary>")]
	[TestCase("/// <summary/>")]
	[TestCase("/// <summary>\n/// </summary>")]
	[TestCase("/// <summary\n/// Text")]
	public void Create_Shows_A_Summary_Without_Text_As_Its_Tag(string comment)
	{
		// Act
		string? banner = DocCommentBanner.Create(Token, comment);

		// Assert
		banner
			.Should()
			.Be("/// <summary>");
	}

	/// <summary>
	/// <see cref="DocCommentBanner.Create" />: an empty element, such as a reference, shows as the values of its
	/// attributes, even over two lines.
	/// </summary>
	[TestCase("/// <summary>Calls <see cref=\"Run\"/>.</summary>", "/// <summary> Calls Run.")]
	[TestCase("/// <summary>Takes <paramref name=\"p\" />.</summary>", "/// <summary> Takes p.")]
	[TestCase("/// <summary>Is <see langword='null'/>.</summary>", "/// <summary> Is null.")]
	[TestCase("/// <summary>A <see\n/// cref=\"X\"/>.</summary>", "/// <summary> A X.")]
	[TestCase("/// <summary>A <x a=\"1\" b=\"2\"/>.</summary>", "/// <summary> A 1 2.")]
	[TestCase("/// <summary>A<br/>B</summary>", "/// <summary> AB")]
	public void Create_Shows_The_Values_Of_An_Empty_Element(string comment, string expected)
	{
		// Act
		string? banner = DocCommentBanner.Create(Token, comment);

		// Assert
		banner
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocCommentBanner.Create" />: a tag that stays open holds the rest of the summary.
	/// </summary>
	[Test]
	public void Create_Stops_At_A_Tag_That_Stays_Open()
	{
		// Act
		string? banner = DocCommentBanner.Create(Token, "/// <summary>A <see cref=\"X\"\n/// B</summary>");

		// Assert
		banner
			.Should()
			.Be("/// <summary> A");
	}

	/// <summary>
	/// <see cref="DocCommentBanner.Create" />: the token of the language leads the banner and is left out at the start of
	/// each line, even when it is made of quotes, as in Visual Basic.
	/// </summary>
	[Test]
	public void Create_Takes_The_Token_Of_The_Language()
	{
		// Act
		string? banner = DocCommentBanner.Create("'''", "''' <summary>\n''' Calls <see\n''' cref=\"Run\"/>.\n''' </summary>");

		// Assert
		banner
			.Should()
			.Be("''' <summary> Calls Run.");
	}
	#endregion
}
