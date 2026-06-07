namespace DataOrganizer.DTO.Clipboard.Persistence;

/// <summary>
/// Persisted plain-text entry, optionally carrying HTML / RTF companion formats.
/// The URL vs. plain-text distinction is re-derived on load, so it is not stored.
/// </summary>
public sealed class PersistedTextEntry : PersistedClipboardEntryBase
{
	#region Properties
	/// <summary>
	/// Optional HTML companion format.
	/// </summary>
	public string? Html { get; set; }

	/// <summary>
	/// Optional RTF companion format.
	/// </summary>
	public string? Rtf { get; set; }

	/// <summary>
	/// Plain text content.
	/// </summary>
	public string Text { get; set; } = string.Empty;
	#endregion
}
