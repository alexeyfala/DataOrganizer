using Shared.Properties;

namespace DataOrganizer.DTO.Clipboard;

/// <summary>
/// Text entry whose whole trimmed content matches an absolute http(s) URL.
/// </summary>
public sealed class ClipboardUrlEntry : ClipboardTextEntry
{
	#region Properties
	/// <inheritdoc />
	public override bool IsUrl => true;

	/// <inheritdoc />
	public override string TypeGlyph => "🔗";

	/// <inheritdoc />
	public override string TypeToolTip => Strings.Hyperlink;

	/// <summary>
	/// Trimmed http(s) URL (whole-string match) used by the "open externally" action.
	/// </summary>
	public required string Url { get; init; }
	#endregion
}
