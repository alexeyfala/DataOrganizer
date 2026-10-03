using System;
using System.Buffers;
using System.Globalization;

namespace DataOrganizer.Helpers.Text.FoldingStrategies;

/// <summary>
/// Banner of a folded documentation comment of XML: the plain text of its summary, as Visual Studio shows it in the
/// box.
/// </summary>
internal static class DocCommentBanner
{
	#region Data
	/// <summary>
	/// Characters of the indentation of a line.
	/// </summary>
	private const string Blanks = " \t";

	/// <summary>
	/// The most characters of an entity, from its ampersand to its semicolon.
	/// </summary>
	private const int MaxEntityLength = 10;

	/// <summary>
	/// The most characters of a banner, past which it is cut and dots follow it, as in Visual Studio.
	/// </summary>
	private const int MaxLength = 80;

	/// <summary>
	/// Start of the tag that closes the summary.
	/// </summary>
	private const string SummaryEnd = "</summary";

	/// <summary>
	/// Start of the tag that opens the summary, as the tag may hold attributes.
	/// </summary>
	private const string SummaryStart = "<summary";

	/// <summary>
	/// Tag of the summary as the banner shows it.
	/// </summary>
	private const string SummaryTag = "<summary>";
	#endregion

	#region Methods
	/// <summary>
	/// Returns the banner of a documentation comment: its token, the tag and the plain text of its summary, with dots
	/// after a banner cut for its length; <c>null</c> when the comment has no summary.
	/// </summary>
	public static string? Create(string token, ReadOnlySpan<char> comment)
	{
		int start = comment.IndexOf(SummaryStart, StringComparison.Ordinal);

		// A tag whose name only starts like that of the summary is another element.
		if (start < 0 || comment[(start + SummaryStart.Length)..] is not ['>' or '/' or ' ' or '\t' or '\r' or '\n', ..])
		{
			return null;
		}

		ReadOnlySpan<char> summary = comment[start..];

		int end = summary.IndexOf(SummaryEnd, StringComparison.Ordinal);

		// A summary that stays open runs to the end of the comment.
		if (end >= 0)
		{
			summary = summary[..end];
		}

		// Pooled, as a pass makes a banner for each comment.
		char[] xml = ArrayPool<char>.Shared.Rent(summary.Length + 1);

		try
		{
			int length = CopyWithoutLineStarts(summary, token, xml);

			return CreateBanner(token, xml.AsSpan(0, length));
		}
		finally
		{
			ArrayPool<char>.Shared.Return(xml);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Writes a character after the text of a banner, with blanks collapsed into one space, and returns the new length of
	/// the text, which stays as it is when the banner is full.
	/// </summary>
	private static int Append(Span<char> banner, int length, char character)
	{
		// No blank leads the banner or follows another one.
		if (char.IsWhiteSpace(character))
		{
			if (length == 0 || banner[length - 1] == ' ')
			{
				return length;
			}

			character = ' ';
		}

		if (length == banner.Length)
		{
			return length;
		}

		banner[length] = character;

		return length + 1;
	}

	/// <summary>
	/// Writes a text after the text of a banner, with blanks collapsed into one space, and returns the new length of the
	/// text.
	/// </summary>
	private static int Append(Span<char> banner, int length, ReadOnlySpan<char> text)
	{
		foreach (char character in text)
		{
			length = Append(banner, length, character);
		}

		return length;
	}

	/// <summary>
	/// Writes the values of the attributes of a tag after the text of a banner, parted by blanks, and returns the new
	/// length of the text.
	/// </summary>
	private static int AppendValues(Span<char> banner, int length, ReadOnlySpan<char> tag)
	{
		// The quote that opens the value under way, and the number of the values so far.
		char quote = default;

		int count = 0;

		foreach (char character in tag)
		{
			if (quote == default && character is '"' or '\'')
			{
				// The values of several attributes are parted by a blank.
				if (count > 0)
				{
					length = Append(banner, length, ' ');
				}

				quote = character;

				count++;
			}
			else if (character == quote)
			{
				quote = default;
			}
			else if (quote != default)
			{
				length = Append(banner, length, character);
			}
		}

		return length;
	}

	/// <summary>
	/// Copies a text of a comment without the blanks and the token that start its lines, with a line break after each
	/// line, and returns the length of the copy.
	/// </summary>
	private static int CopyWithoutLineStarts(ReadOnlySpan<char> text, string token, Span<char> destination)
	{
		int length = 0;

		foreach (ReadOnlySpan<char> line in text.EnumerateLines())
		{
			ReadOnlySpan<char> rest = line.TrimStart(Blanks);

			if (rest.StartsWith(token, StringComparison.Ordinal))
			{
				rest = rest[token.Length..];
			}

			rest.CopyTo(destination[length..]);

			length += rest.Length;

			destination[length++] = '\n';
		}

		return length;
	}

	/// <summary>
	/// Returns the banner of a summary whose lines are freed of their starts: the token, the tag and the plain text of the
	/// summary, with dots after a banner cut for its length.
	/// </summary>
	private static string CreateBanner(string token, ReadOnlySpan<char> summary)
	{
		// Room for a character past the most with a blank before it, which tells a long text from one of the most length.
		Span<char> banner = stackalloc char[MaxLength + 2];

		// The token and the tag lead the banner, as in Visual Studio.
		int length = Append(banner, 0, token);

		length = Append(banner, length, $" {SummaryTag} ");

		int startEnd = FindTagEnd(summary);

		// The text follows the start tag, unless the tag closes the element at once or stays open.
		ReadOnlySpan<char> content = startEnd > 0 && summary[startEnd - 1] != '/' ? summary[(startEnd + 1)..] : [];

		for (int index = 0; index < content.Length && length < banner.Length; index++)
		{
			char character = content[index];

			if (character == '<' && index + 1 < content.Length && IsMarkupStart(content[index + 1]))
			{
				int end = FindTagEnd(content[index..]);

				// A tag that stays open holds the rest of the summary.
				if (end < 0)
				{
					break;
				}

				ReadOnlySpan<char> tag = content.Slice(index, end + 1);

				// An empty element, such as a reference, stands for the values of its attributes; other tags are left out.
				if (tag is [_, not ('/' or '!' or '?'), .., '/', '>'])
				{
					length = AppendValues(banner, length, tag);
				}

				index += end;
			}
			else if (character == '&' && TryDecodeEntity(content[index..], out char decoded, out int entityLength))
			{
				length = Append(banner, length, decoded);

				index += entityLength - 1;
			}
			else
			{
				length = Append(banner, length, character);
			}
		}

		ReadOnlySpan<char> text = banner[..length].TrimEnd();

		// A long banner is cut with dots after it, as in Visual Studio.
		return text.Length > MaxLength ? $"{text[..MaxLength].TrimEnd()} {Glyphs.ThreeDots}" : text.ToString();
	}

	/// <summary>
	/// Returns the index of the bracket that closes the tag at the start of a text, past the quoted values of its
	/// attributes; -1 when the tag stays open.
	/// </summary>
	private static int FindTagEnd(ReadOnlySpan<char> text)
	{
		// The quote that opens the value under way.
		char quote = default;

		for (int index = 1; index < text.Length; index++)
		{
			char character = text[index];

			if (quote != default)
			{
				if (character == quote)
				{
					quote = default;
				}
			}
			else if (character is '"' or '\'')
			{
				quote = character;
			}
			else if (character == '>')
			{
				return index;
			}
		}

		return -1;
	}

	/// <summary>
	/// Returns <c>true</c> when a character after an angle bracket starts markup, such as the name of a tag, and not text.
	/// </summary>
	private static bool IsMarkupStart(char character)
	{
		return char.IsLetter(character) || character is '_' or ':' or '/' or '!' or '?';
	}

	/// <summary>
	/// Returns the character of a numeric reference, in decimal digits or in hexadecimal ones after an x; zero when the
	/// number is no character.
	/// </summary>
	private static char ParseReference(ReadOnlySpan<char> number)
	{

		bool isParsed = number is ['x', .. var digits]
			? ushort.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort code)
			: ushort.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out code);

		return isParsed ? (char)code : default;
	}

	/// <summary>
	/// Returns <c>true</c> when a text starts with an entity of XML, with the character that it stands for and its length.
	/// </summary>
	private static bool TryDecodeEntity(ReadOnlySpan<char> text, out char character, out int length)
	{
		// An entity runs from its ampersand to its semicolon.
		int semicolon = text[..Math.Min(text.Length, MaxEntityLength)].IndexOf(';');

		length = semicolon + 1;

		ReadOnlySpan<char> name = semicolon > 0 ? text[1..semicolon] : [];

		character = name switch
		{
			"amp" => '&',
			"apos" => '\'',
			"gt" => '>',
			"lt" => '<',
			"quot" => '"',
			['#', .. var number] => ParseReference(number),
			_ => default
		};

		// A surrogate stands for no character on its own.
		return character != default && !char.IsSurrogate(character);
	}
	#endregion
}
