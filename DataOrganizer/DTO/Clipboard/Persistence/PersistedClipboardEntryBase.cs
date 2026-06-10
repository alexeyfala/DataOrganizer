using System.Text.Json.Serialization;

namespace DataOrganizer.DTO.Clipboard.Persistence;

/// <summary>
/// Base type for a single clipboard history entry persisted to disk.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PersistedTextEntry), "Text")]
[JsonDerivedType(typeof(PersistedImageEntry), "Image")]
[JsonDerivedType(typeof(PersistedFilesEntry), "Files")]
public abstract class PersistedClipboardEntryBase
{
	#region Properties
	/// <summary>
	/// SHA-256 of the source data; used for change detection and deduplication.
	/// </summary>
	public byte[] Hash { get; set; } = [];

	/// <summary>
	/// <c>True</c> when the entry is pinned.
	/// </summary>
	public bool IsPinned { get; set; }
	#endregion
}
