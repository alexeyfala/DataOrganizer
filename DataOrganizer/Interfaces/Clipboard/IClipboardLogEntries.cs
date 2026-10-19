using DataOrganizer.Models.Clipboard;
using System.Collections.ObjectModel;

namespace DataOrganizer.Interfaces.Clipboard;

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
