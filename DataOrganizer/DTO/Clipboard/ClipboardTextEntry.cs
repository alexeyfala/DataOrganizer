using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Security;
using Shared.Properties;

namespace DataOrganizer.DTO.Clipboard;

/// <summary>
/// Plain-text clipboard entry, optionally carrying HTML / RTF companion formats.
/// </summary>
public class ClipboardTextEntry : ClipboardLogEntryBase
{
	#region Properties
	/// <summary>
	/// HTML version of <see cref="Text" /> (e.g. from browsers or Word) when the
	/// source app provided one. Pushed back to the clipboard alongside plain text
	/// on restore so paste targets can pick up the formatting.
	/// </summary>
	public required string? Html { get; init; }

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

	/// <inheritdoc />
	public override string? SearchableText => Text;

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
	/// Builds the type badge glyph.
	/// </summary>
	private string BuildTypeGlyph()
	{
		const string htmlGlyph = Glyphs.AngleBracketSlash;

		const string formattedTextGlyph = Glyphs.BButton;

		const string plainTextGlyph = Glyphs.InputLatinLetters;

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
