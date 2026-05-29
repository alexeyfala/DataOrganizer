namespace DataOrganizer.DTO.Clipboard;

/// <summary>
/// Base type for one record in the in-memory system clipboard history.
/// </summary>
public abstract class ClipboardHistoryEntryBase
{
	#region Properties
	/// <summary>
	/// SHA-256 of the source data; used for change detection and deduplication.
	/// </summary>
	public required byte[] Hash { get; init; }

	/// <summary>
	/// <c>True</c> when this entry exposes an openable URL.
	/// </summary>
	public virtual bool IsUrl => false;
	#endregion
}
