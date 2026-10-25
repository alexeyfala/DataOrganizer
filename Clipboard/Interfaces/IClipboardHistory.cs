using Clipboard.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Clipboard.Interfaces;

/// <summary>
/// The entries of the clipboard history, newest first, and the rules that keep them; used on the UI thread only.
/// </summary>
public interface IClipboardHistory
{
	#region Properties
	/// <summary>
	/// History entries, newest first; pinned entries sit atop.
	/// </summary>
	ObservableCollection<ClipboardLogEntryBase> Entries { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Removes the entries, keeping the pinned ones when asked, and forgets the last observed payload; returns how many
	/// entries are left.
	/// </summary>
	int Clear(bool preservePinned);

	/// <summary>
	/// Drops the active highlight and the last observed payload once the clipboard holds nothing to capture.
	/// </summary>
	void HandleClipboardCleared();

	/// <summary>
	/// Records a payload seen on the clipboard by its hash, creating its entry only when it is new; only a payload marked as
	/// restored answers a restore.
	/// </summary>
	void HandleNewPayload(
		byte[] hash,
		Func<ClipboardLogEntryBase> entryFactory,
		bool isSensitive,
		bool isRestored);

	/// <summary>
	/// Makes a restored entry the active one, atop unless its position is kept, and waits for the clipboard to hand it back.
	/// </summary>
	void MarkRestored(ClipboardLogEntryBase entry, bool keepPosition);

	/// <summary>
	/// Merges <paramref name="entries" /> below the current ones, skipping hash duplicates and enforcing the history cap.
	/// Raises no change notification.
	/// </summary>
	void Merge(IReadOnlyList<ClipboardLogEntryBase> entries);

	/// <summary>
	/// Removes an entry, a pinned one too; <c>false</c> when it is not in the history.
	/// </summary>
	bool Remove(ClipboardLogEntryBase entry);

	/// <summary>
	/// Toggles the pinned state of an entry and repositions it: pinned entries are kept atop the history and survive
	/// clearing and trimming.
	/// </summary>
	void TogglePin(ClipboardLogEntryBase entry);
	#endregion
}
