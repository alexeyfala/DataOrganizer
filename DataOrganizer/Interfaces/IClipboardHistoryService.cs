using DataOrganizer.DTO;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Tracks the system clipboard in the background and exposes an in-memory
/// history (newest first) capped at <see cref="HistoryLimit" /> entries.
/// </summary>
public interface IClipboardHistoryService
{
	#region Properties
	/// <summary>
	/// Maximum number of entries kept in history. Matches the Windows
	/// system clipboard (Win+V) limit of 25 records.
	/// </summary>
	static int HistoryLimit { get; } = 25;

	/// <summary>
	/// History entries, newest first.
	/// </summary>
	ObservableCollection<ClipboardHistoryEntry> Entries { get; }

	/// <summary>
	/// <c>True</c> while the background polling timer is active.
	/// </summary>
	bool IsRunning { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Restores <paramref name="entry" /> into the system clipboard and moves it
	/// to the top of <see cref="Entries" />.
	/// </summary>
	Task RestoreAsync(ClipboardHistoryEntry entry);

	/// <summary>
	/// Starts background polling. Safe to call more than once.
	/// </summary>
	Task StartAsync(CancellationToken token = default);
	#endregion
}
