using DataOrganizer.Helpers;
using Shared.Properties;

namespace DataOrganizer.Models.Clipboard;

/// <summary>
/// Plain-text clipboard entry, optionally carrying HTML / RTF companion formats.
/// </summary>
public sealed class ClipboardTextEntry : ClipboardTextEntryBase
{
	#region Properties
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
