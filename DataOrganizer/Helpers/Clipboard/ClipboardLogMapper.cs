using DataOrganizer.Dto.Clipboard.Persistence;
using DataOrganizer.Models.Clipboard;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Clipboard;

/// <summary>
/// Maps clipboard log entries between the in-memory domain model and the on-disk persisted model.
/// </summary>
internal static class ClipboardLogMapper
{
	#region Methods
	/// <summary>
	/// Reconstructs domain entries from a persisted history. Entries that cannot be
	/// mapped (unknown type) are skipped.
	/// </summary>
	public static List<ClipboardLogEntryBase> ToDomain(PersistedClipboardLog history)
	{
		List<ClipboardLogEntryBase> result = new(history.Entries.Count);

		foreach (PersistedClipboardEntryBase entry in history.Entries)
		{
			if (ToDomainEntry(entry) is { } domain)
			{
				result.Add(domain);
			}
		}

		return result;
	}

	/// <summary>
	/// Projects domain entries into a persisted history container.
	/// </summary>
	public static PersistedClipboardLog ToPersisted(IEnumerable<ClipboardLogEntryBase> entries)
	{
		PersistedClipboardLog history = new();

		foreach (ClipboardLogEntryBase entry in entries)
		{
			if (ToPersistedEntry(entry) is { } persisted)
			{
				history.Entries.Add(persisted);
			}
		}

		return history;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Maps a single persisted entry to its domain counterpart; <c>null</c> for unknown types.
	/// </summary>
	private static ClipboardLogEntryBase? ToDomainEntry(PersistedClipboardEntryBase entry) => entry switch
	{
		PersistedImageEntry image => ClipboardLogEntryFactory.CreateImage(
			image.OriginalPng,
			image.Hash,
			isPinned: image.IsPinned),
		PersistedFilesEntry files => ClipboardLogEntryFactory.CreateFiles(
			[.. files.Files.Select(static x => new ClipboardFileSystemEntry(x.Path, x.IsFolder))],
			files.Hash,
			isPinned: files.IsPinned),
		PersistedTextEntry text => ClipboardLogEntryFactory.CreateText(
			text.Text,
			text.Html,
			text.Rtf,
			text.Hash,
			isPinned: text.IsPinned),
		_ => null
	};

	/// <summary>
	/// Maps a single domain entry to its persisted counterpart; <c>null</c> for unknown types.
	/// </summary>
	private static PersistedClipboardEntryBase? ToPersistedEntry(ClipboardLogEntryBase entry) => entry switch
	{
		ClipboardImageEntry image => new PersistedImageEntry
		{
			OriginalPng = image.OriginalPng,
			Hash = image.Hash,
			IsPinned = image.IsPinned
		},
		ClipboardFilesEntry files => new PersistedFilesEntry
		{
			Files = [.. files.FileSystemEntries.Select(static x => new PersistedFileSystemEntry(x.Path, x.IsFolder))],
			Hash = files.Hash,
			IsPinned = files.IsPinned
		},
		// A link is stored as its text, and the factory finds the link again on load.
		ClipboardTextEntryBase text => new PersistedTextEntry
		{
			Text = text.Text,
			Html = text.Html,
			Rtf = text.Rtf,
			Hash = text.Hash,
			IsPinned = text.IsPinned
		},
		_ => null
	};
	#endregion
}
