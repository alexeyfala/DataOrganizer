namespace Clipboard.Models;

/// <summary>
/// Image clipboard entry backed by the original full-size PNG bytes.
/// </summary>
public sealed class ClipboardImageEntry : ClipboardLogEntryBase
{
	#region Properties
	/// <summary>
	/// Original full-size PNG bytes.
	/// </summary>
	public required byte[] OriginalPng { get; init; }
	#endregion
}
