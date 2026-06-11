using CommunityToolkit.Mvvm.ComponentModel;

namespace DataOrganizer.DTO.Clipboard;

/// <summary>
/// Base type for one record in the in-memory system clipboard history.
/// </summary>
public abstract partial class ClipboardHistoryEntryBase : ObservableObject
{
	#region Properties
	/// <summary>
	/// Content for the entry's hover tooltip; <c>null</c> shows none. Overridden where needed.
	/// </summary>
	public virtual string? ContentToolTip => null;

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
	/// <c>True</c> when this entry exposes an openable URL.
	/// </summary>
	public virtual bool IsUrl => false;

	/// <summary>
	/// Emoji glyph shown as a type badge over the entry in the clipboard history list.
	/// </summary>
	public abstract string TypeGlyph { get; }

	/// <summary>
	/// Human-readable payload type, shown as the badge tooltip. Localized by the caller.
	/// </summary>
	public abstract string TypeToolTip { get; }
	#endregion
}
