using System.Collections.Generic;

namespace DataOrganizer.Models.Clipboard;

/// <summary>
/// Clipboard entry holding a captured list of filesystem entries.
/// </summary>
public sealed class ClipboardFilesEntry : ClipboardLogEntryBase
{
	#region Properties
	/// <summary>
	/// File / folder items captured for this entry.
	/// </summary>
	public required IReadOnlyList<ClipboardFileSystemEntry> FileSystemEntries { get; init; }
	#endregion
}
