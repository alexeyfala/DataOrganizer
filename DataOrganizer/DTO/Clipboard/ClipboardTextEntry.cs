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
	public override string TypeGlyph => IsFormattedText()
		? "🅱️"
		: "🔤";

	/// <inheritdoc />
	public override string TypeToolTip => IsFormattedText()
		? Strings.FormattedText
		: Strings.PlainText;
	#endregion

	#region Helpers
	/// <summary>
	/// Returns <c>True</c> if <see cref="Text" /> is formatted text.
	/// </summary>
	private bool IsFormattedText() => Html is not null || Rtf is not null;
	#endregion
}
