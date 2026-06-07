using DataOrganizer.DTO.Clipboard;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Tracks the system clipboard in the background and exposes an in-memory
/// history (newest first) capped at <see cref="HistoryLimit" /> entries.
/// </summary>
public interface IClipboardHistoryService : IAsyncDisposable
{
	#region Properties
	/// <summary>
	/// History entries, newest first.
	/// </summary>
	ObservableCollection<ClipboardHistoryEntryBase> Entries { get; }

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
	void Merge(IReadOnlyList<ClipboardHistoryEntryBase> entries);

	/// <summary>
	/// Restores <paramref name="entry" /> into the system clipboard and moves it
	/// to the top of <see cref="Entries" />.
	/// </summary>
	Task RestoreAsync(ClipboardHistoryEntryBase entry);

	/// <summary>
	/// Starts background polling. Safe to call more than once.
	/// </summary>
	Task StartAsync(CancellationToken token = default);

	/// <summary>
	/// Stops background polling without disposing the service.
	/// </summary>
	void Stop();
	#endregion
}
