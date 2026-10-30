using Clipboard.Models;
using System.Collections.Generic;

namespace Clipboard.Interfaces;

/// <summary>
/// Adds entries that come from elsewhere, such as a stored log, to the clipboard log.
/// </summary>
public interface IClipboardLogMerger : IClipboardLogEntries
{
	#region Methods
	/// <summary>
	/// Merges <paramref name="entries" /> into <see cref="IClipboardLogEntries.Entries" /> below the current ones,
	/// skipping hash duplicates and enforcing the history cap. Raises no change notification.
	/// </summary>
	void Merge(IReadOnlyList<ClipboardLogEntryBase> entries);
	#endregion
}
