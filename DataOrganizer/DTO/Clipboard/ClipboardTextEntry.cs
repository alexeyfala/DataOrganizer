using DataOrganizer.Helpers;
using Shared.Properties;
using System;
using System.Text.RegularExpressions;

namespace DataOrganizer.DTO.Clipboard;

/// <summary>
/// Plain-text clipboard entry, optionally carrying HTML / RTF companion formats.
/// </summary>
public partial class ClipboardTextEntry : ClipboardHistoryEntryBase
{
	#region Properties
	/// <summary>
	/// Renderable HTML fragment for formatted entries.
	/// </summary>
	public string? FormattedTextPreview => field ??= BuildFormattedTextPreview();

	/// <summary>
	/// HTML version of <see cref="Text" /> (e.g. from browsers or Word) when the
	/// source app provided one. Pushed back to the clipboard alongside plain text
	/// on restore so paste targets can pick up the formatting.
	/// </summary>
	public required string? Html { get; init; }

	/// <summary>
	/// <c>True</c> when <see cref="IsHtml" /> == <c>True</c> or <see cref="IsRtf" /> == <c>True</c>.
	/// </summary>
	public bool IsFormattedText => IsHtml || IsRtf;

	/// <summary>
	/// <c>True</c> when <see cref="Html" /> is not null.
	/// </summary>
	public bool IsHtml => Html is not null;

	/// <summary>
	/// <c>True</c> when <see cref="Rtf" /> is not null.
	/// </summary>
	public bool IsRtf => Rtf is not null;

	/// <summary>
	/// <c>True</c> when <see cref="Text" /> heuristically looks like a password / secret token.
	/// </summary>
	public bool IsSensitive => SensitiveTextDetector.LooksLikeSecret(Text);

	/// <summary>
	/// RTF version of <see cref="Text" /> when the source app provided one.
	/// </summary>
	public required string? Rtf { get; init; }

	/// <summary>
	/// Plain text content.
	/// </summary>
	public required string Text { get; init; }

	/// <inheritdoc />
	public override string TypeGlyph => BuildTypeGlyph();

	/// <inheritdoc />
	public override string TypeToolTip => field ??= BuildTypeToolTip();
	#endregion

	#region Helpers
	/// <summary>
	/// Strips the Windows CF_HTML descriptor header, returning just the fragment markup.
	/// </summary>
	private static string ExtractHtmlFragment(string html)
	{
		const string startMarker = "<!--StartFragment-->";

		const string endMarker = "<!--EndFragment-->";

		int start = html.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);

		int end = html.IndexOf(endMarker, StringComparison.OrdinalIgnoreCase);

		if (start >= 0 && end > start)
		{
			return NormalizePreBlocks(html[(start + startMarker.Length)..end]);
		}

		// No CF_HTML markers (non-Windows, or already a bare fragment): drop any
		// leading descriptor header by returning from the first tag onward.
		int firstTag = html.IndexOf('<');

		return NormalizePreBlocks(firstTag > 0
			? html[firstTag..]
			: html);
	}

	/// <summary>
	/// Makes preformatted blocks (e.g. Visual Studio code copies) render multi-line by
	/// turning their literal newlines/tabs into explicit breaks the HTML engine honors.
	/// </summary>
	private static string NormalizePreBlocks(string html)
	{
		if (html.IndexOf("<pre", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return html;
		}

		return PreBlockRegex().Replace(html, static match =>
		{
			string inner = match
				.Groups[2]
				.Value
				.Replace("\r\n", "<br>")
				.Replace("\n", "<br>")
				.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");

			return match.Groups[1].Value + inner + match.Groups[3].Value;
		});
	}

	/// <summary>
	/// Matches for a single preformatted block: open tag, inner content, close tag.
	/// </summary>
	[GeneratedRegex(@"(<pre\b[^>]*>)(.*?)(</pre>)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
	private static partial Regex PreBlockRegex();

	/// <summary>
	/// Backing builder for <see cref="FormattedTextPreview" />.
	/// </summary>
	private string? BuildFormattedTextPreview()
	{
		if (Html is { } html)
		{
			return ExtractHtmlFragment(html);
		}

		if (Rtf is { } rtf)
		{
			try
			{
				return RtfPipe.Rtf.ToHtml(rtf);
			}
			catch
			{
				// Conversion failed -> entry stays on the plain-text template.
				return null;
			}
		}

		return null;
	}

	/// <summary>
	/// Builds the type badge glyph.
	/// </summary>
	private string BuildTypeGlyph()
	{
		const string htmlGlyph = "</>";

		const string formattedTextGlyph = "🅱️";

		const string plainTextGlyph = "🔤";

		return (IsHtml, IsRtf) switch
		{
			(true, true) => $"{htmlGlyph} {formattedTextGlyph}",
			(true, false) => htmlGlyph,
			(false, true) => formattedTextGlyph,
			_ => plainTextGlyph
		};
	}

	/// <summary>
	/// Builds the type badge tooltip matching <see cref="BuildTypeGlyph" />.
	/// </summary>
	private string BuildTypeToolTip() => (IsHtml, IsRtf) switch
	{
		(true, true) => $"HTML + {Strings.FormattedText}",
		(true, false) => "HTML",
		(false, true) => Strings.FormattedText,
		_ => Strings.PlainText
	};
	#endregion
}
