using CommunityToolkit.Mvvm.ComponentModel;

namespace DataOrganizer.Models.Clipboard;

/// <summary>
/// Base type for one record in the in-memory system clipboard log.
/// </summary>
public abstract partial class ClipboardLogEntryBase : ObservableObject
{
	#region Properties
	/// <summary>
	/// SHA-256 of the source data; used for change detection and deduplication.
	/// </summary>
	public required byte[] Hash { get; init; }

	/// <summary>
	/// <c>True</c> while this entry holds the current system-clipboard content (just restored or copied).
	/// </summary>
	[ObservableProperty]
	public partial bool IsActive { get; set; }

	/// <summary>
	/// <c>True</c> when the entry is pinned.
	/// </summary>
	[ObservableProperty]
	public partial bool IsPinned { get; set; }

	/// <summary>
	/// <c>True</c> when the entry content is treated as a secret.
	/// </summary>
	public bool IsSensitive { get; init; }

	/// <summary>
	/// <c>True</c> when this entry exposes an openable URL.
	/// </summary>
	public virtual bool IsUrl => false;

	/// <summary>
	/// Text used to match this entry against a search query; <c>null</c> excludes it from matching.
	/// </summary>
	public virtual string? SearchableText => null;
	#endregion
}
