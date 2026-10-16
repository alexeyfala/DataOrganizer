namespace DataOrganizer.Models.Clipboard;

/// <summary>
/// Text entry whose whole trimmed content matches an absolute http(s) URL.
/// </summary>
public sealed class ClipboardUrlEntry : ClipboardTextEntryBase
{
	#region Properties
	/// <inheritdoc />
	public override bool IsUrl => true;

	/// <summary>
	/// Trimmed http(s) URL (whole-string match) used by the "open externally" action.
	/// </summary>
	public required string Url { get; init; }
	#endregion
}
