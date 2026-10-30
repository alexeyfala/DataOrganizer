namespace Clipboard.Models;

/// <summary>
/// Base of the text entries of the clipboard log, optionally carrying HTML / RTF companion formats.
/// </summary>
public abstract class ClipboardTextEntryBase : ClipboardLogEntryBase
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
	/// RTF version of <see cref="Text" /> when the source app provided one.
	/// </summary>
	public required string? Rtf { get; init; }

	/// <inheritdoc />
	public override string? SearchableText => Text;

	/// <summary>
	/// Plain text content.
	/// </summary>
	public required string Text { get; init; }
	#endregion
}
