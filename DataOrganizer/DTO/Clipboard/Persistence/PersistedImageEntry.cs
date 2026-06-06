namespace DataOrganizer.DTO.Clipboard.Persistence;

/// <summary>
/// Persisted image entry backed by the original full-size PNG bytes.
/// </summary>
public sealed class PersistedImageEntry : PersistedClipboardEntry
{
	#region Properties
	/// <summary>
	/// Original full-size PNG bytes.
	/// </summary>
	public byte[] OriginalPng { get; set; } = [];
	#endregion
}
