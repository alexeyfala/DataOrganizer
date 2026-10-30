using Clipboard.Models;
using System.Threading.Tasks;

namespace Clipboard.Interfaces;

/// <summary>
/// What the user does with the entries of the clipboard log.
/// </summary>
public interface IClipboardLogService : IClipboardLogEntries
{
	#region Methods
	/// <summary>
	/// Clears <see cref="IClipboardLogEntries.Entries" />, forgets the last observed payload and empties the
	/// system clipboard, so cleared content is not re-captured until a new copy occurs.
	/// </summary>
	Task ClearAllAsync();

	/// <summary>
	/// Removes <paramref name="entry" /> from <see cref="IClipboardLogEntries.Entries" />, including pinned ones. When the
	/// entry is the active one, the system clipboard is emptied too.
	/// </summary>
	Task RemoveAsync(ClipboardLogEntryBase entry);

	/// <summary>
	/// Restores <paramref name="entry" /> into the system clipboard. Moves it to the top of
	/// <see cref="IClipboardLogEntries.Entries" /> unless <paramref name="keepPosition" /> is set.
	/// </summary>
	Task RestoreAsync(ClipboardLogEntryBase entry, bool keepPosition = false);

	/// <summary>
	/// Toggles the pinned state of <paramref name="entry" /> and repositions it: pinned entries are
	/// kept atop the history and survive clearing / trimming.
	/// </summary>
	void TogglePin(ClipboardLogEntryBase entry);
	#endregion
}
