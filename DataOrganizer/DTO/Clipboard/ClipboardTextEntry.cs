using Shared.Properties;

namespace DataOrganizer.DTO.Clipboard;

/// <summary>
/// Plain-text clipboard entry, optionally carrying HTML / RTF companion formats.
/// </summary>
public class ClipboardTextEntry : ClipboardHistoryEntryBase
{
	#region Properties
	/// <summary>
	/// HTML version of <see cref="Text" /> (e.g. from browsers or Word) when the
	/// source app provided one. Pushed back to the clipboard alongside plain text
	/// on restore so paste targets can pick up the formatting.
	/// </summary>
	public string? Html { get; init; }

	/// <summary>
	/// RTF version of <see cref="Text" /> when the source app provided one.
	/// </summary>
	public string? Rtf { get; init; }

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
		const string htmlGlyph = "</>";

		const string formattedTextGlyph = "🅱️";

		const string plainTextGlyph = "🔤";

		bool hasHtml = Html is not null;

		bool hasRtf = Rtf is not null;

		return (hasHtml, hasRtf) switch
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
	private string BuildTypeToolTip()
	{
		bool hasHtml = Html is not null;

		bool hasRtf = Rtf is not null;

		return (hasHtml, hasRtf) switch
		{
			(true, true) => $"HTML + {Strings.FormattedText}",
			(true, false) => "HTML",
			(false, true) => Strings.FormattedText,
			_ => Strings.PlainText
		};
	}
	#endregion
}
