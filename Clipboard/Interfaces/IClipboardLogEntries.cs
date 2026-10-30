using Clipboard.Models;
using System.Collections.ObjectModel;

namespace Clipboard.Interfaces;

/// <summary>
/// In-memory log of the system clipboard, newest first, capped at a fixed number of entries.
/// </summary>
public interface IClipboardLogEntries
{
	#region Properties
	/// <summary>
	/// History entries, newest first.
	/// </summary>
	ObservableCollection<ClipboardLogEntryBase> Entries { get; }
	#endregion
}
