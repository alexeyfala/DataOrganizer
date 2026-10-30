using AwesomeAssertions;
using Clipboard.Helpers;
using Clipboard.Models;

namespace Clipboard.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardLogEntryFactory)}"" type")]
internal class ClipboardLogEntryFactoryTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardLogEntryFactory.CreateText" />: only a text that is a whole http or https link, in any case of
	/// letters, becomes a link entry.
	/// </summary>
	[TestCase("https://example.com", true)]
	[TestCase("http://example.com", true)]
	[TestCase("HTTPS://EXAMPLE.COM/A", true)]
	[TestCase("see https://example.com", false)]
	[TestCase("https://example.com page", false)]
	[TestCase("ftp://example.com", false)]
	public void CreateText_Builds_A_Link_Entry_Only_For_A_Whole_Link(string text, bool isLink)
	{
		// Act
		ClipboardTextEntryBase entry = ClipboardLogEntryFactory.CreateText(text, null, null, [1]);

		// Assert
		entry.IsUrl
			.Should()
			.Be(isLink);
	}

	/// <summary>
	/// <see cref="ClipboardLogEntryFactory.CreateText" />: plain text becomes a text entry.
	/// </summary>
	[Test]
	public void CreateText_Builds_Text_Entry_For_Plain_Text()
	{
		// Act
		ClipboardTextEntryBase entry = ClipboardLogEntryFactory.CreateText("just text", "<b>x</b>", null, [1]);

		// Assert
		ClipboardTextEntry text = entry
			.Should()
			.BeOfType<ClipboardTextEntry>()
			.Subject;

		text.Text
			.Should()
			.Be("just text");

		text.Html
			.Should()
			.Be("<b>x</b>");
	}

	/// <summary>
	/// <see cref="ClipboardLogEntryFactory.CreateText" />: whole-string URL text becomes a URL entry (trimmed).
	/// </summary>
	[Test]
	public void CreateText_Builds_Url_Entry_For_Url_Text()
	{
		// Act
		ClipboardTextEntryBase entry = ClipboardLogEntryFactory.CreateText(
			text: "  https://example.com/x  ",
			html: null,
			rtf: null,
			hash: [1]);

		// Assert
		ClipboardUrlEntry url = entry
			.Should()
			.BeOfType<ClipboardUrlEntry>()
			.Subject;

		url.Url
			.Should()
			.Be("https://example.com/x");

		url.Text
			.Should()
			.Be("  https://example.com/x  ");
	}

	/// <summary>
	/// <see cref="ClipboardLogEntryFactory.CreateText" />: a link is never treated as a secret, even with a token-like part.
	/// </summary>
	[Test]
	public void CreateText_Leaves_A_Link_Not_Sensitive()
	{
		// Act
		ClipboardTextEntryBase entry = ClipboardLogEntryFactory.CreateText("https://example.com/Xk7#pQ2!mZ", null, null, [1]);

		// Assert
		entry.IsUrl
			.Should()
			.BeTrue();

		entry.IsSensitive
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardLogEntryFactory.CreateText" />: ordinary prose is not flagged.
	/// </summary>
	[Test]
	public void CreateText_Leaves_Plain_Prose_Not_Sensitive()
	{
		// Act
		ClipboardTextEntryBase entry = ClipboardLogEntryFactory.CreateText("hello world", null, null, [1]);

		// Assert
		entry.IsSensitive
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardLogEntryFactory.CreateText" />: a high-entropy token is flagged.
	/// </summary>
	[Test]
	public void CreateText_Marks_A_Secret_Like_Token_As_Sensitive()
	{
		// Act
		ClipboardTextEntryBase entry = ClipboardLogEntryFactory.CreateText("Xy7$kQ9pLm2!", null, null, [1]);

		// Assert
		entry.IsSensitive
			.Should()
			.BeTrue();
	}
	#endregion
}
