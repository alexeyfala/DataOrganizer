using System.Text.Json.Serialization;

namespace DataOrganizer.DTO.Clipboard.Persistence;

/// <summary>
/// Base type for a single clipboard history entry persisted to disk.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PersistedTextEntry), "text")]
[JsonDerivedType(typeof(PersistedImageEntry), "image")]
[JsonDerivedType(typeof(PersistedFilesEntry), "files")]
public abstract class PersistedClipboardEntry
{
	#region Properties
	/// <summary>
	/// SHA-256 of the source data; used for change detection and deduplication.
	/// </summary>
	public byte[] Hash { get; set; } = [];
	#endregion
}
