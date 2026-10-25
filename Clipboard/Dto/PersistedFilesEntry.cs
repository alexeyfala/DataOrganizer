using System.Collections.Generic;

namespace Clipboard.Dto;

/// <summary>
/// Persisted entry holding a captured list of filesystem items.
/// </summary>
public sealed class PersistedFilesEntry : PersistedClipboardEntryBase
{
	#region Properties
	/// <summary>
	/// Captured file / folder items.
	/// </summary>
	public List<PersistedFileSystemEntry> Files { get; set; } = [];
	#endregion
}
