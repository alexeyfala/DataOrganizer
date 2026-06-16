using DataOrganizer.DTO.Clipboard;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Clipboard;

/// <summary>
/// Tracks the system clipboard in the background and exposes an in-memory
/// log (newest first) capped at <see cref="HistoryLimit" /> entries.
/// </summary>
public interface IClipboardLogService : IAsyncDisposable
{
	#region Properties
	/// <summary>
	/// History entries, newest first.
	/// </summary>
	ObservableCollection<ClipboardLogEntryBase> Entries { get; }

	/// <summary>
	/// <c>True</c> while the background polling is active.
	/// </summary>
	bool IsRunning { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Clears <see cref="Entries" />, forgets the last observed payload and empties the
	/// system clipboard, so cleared content is not re-captured until a new copy occurs.
	/// </summary>
	Task ClearAsync();

	/// <summary>
	/// Clears <see cref="Entries" /> and forgets the last observed payload, without
	/// touching the system clipboard.
	/// </summary>
	Task ClearEntriesAsync();

	/// <summary>
	/// Merges <paramref name="entries" /> into <see cref="Entries" /> below the current ones,
	/// skipping hash duplicates and enforcing the history cap. Raises no change notification.
	/// </summary>
	void Merge(IReadOnlyList<ClipboardLogEntryBase> entries);

	/// <summary>
	/// Removes <paramref name="entry" /> from <see cref="Entries" />, including pinned ones. When the entry
	/// is the active one, the system clipboard is emptied too.
	/// </summary>
	Task RemoveAsync(ClipboardLogEntryBase entry);

	/// <summary>
	/// Restores <paramref name="entry" /> into the system clipboard. Moves it to the top of
	/// <see cref="Entries" /> unless <paramref name="keepPosition" /> is set.
	/// </summary>
	Task RestoreAsync(ClipboardLogEntryBase entry, bool keepPosition = false);

	/// <summary>
	/// Starts background polling. Safe to call more than once.
	/// </summary>
	Task StartAsync(CancellationToken token = default);

	/// <summary>
	/// Stops background polling without disposing the service.
	/// </summary>
	void Stop();

	/// <summary>
	/// Toggles the pinned state of <paramref name="entry" /> and repositions it: pinned entries are
	/// kept atop the history and survive clearing / trimming.
	/// </summary>
	void TogglePin(ClipboardLogEntryBase entry);
	#endregion
}
