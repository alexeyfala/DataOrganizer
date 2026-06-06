using DataOrganizer.DTO.Clipboard;
using DataOrganizer.DTO.Clipboard.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.Helpers;

/// <summary>
/// Maps clipboard history entries between the in-memory domain model and the on-disk persisted model.
/// </summary>
public static partial class ClipboardHistoryMapper
{
	#region Methods
	/// <summary>
	/// Reconstructs domain entries from a persisted history. Entries that cannot be
	/// mapped (unknown type) are skipped.
	/// </summary>
	public static List<ClipboardHistoryEntryBase> ToDomain(PersistedClipboardHistory history)
	{
		List<ClipboardHistoryEntryBase> result = new(history.Entries.Count);

		foreach (PersistedClipboardEntry entry in history.Entries)
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
	public static PersistedClipboardHistory ToPersisted(IEnumerable<ClipboardHistoryEntryBase> entries)
	{
		PersistedClipboardHistory history = new();

		foreach (ClipboardHistoryEntryBase entry in entries)
		{
			if (ToPersistedEntry(entry) is { } persisted)
			{
				history.Entries.Add(persisted);
			}
		}

		return history;
	}

	/// <summary>
	/// Matches when the entire input is an http(s) URL.
	/// </summary>
	[GeneratedRegex(@"^https?://\S+$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
	public static partial Regex WholeStringUrlRegex();
	#endregion

	#region Helpers
	/// <summary>
	/// Builds a domain text entry, choosing <see cref="ClipboardUrlEntry" /> when the text is a whole URL.
	/// </summary>
	/// <remarks>
	/// Mirrors the URL detection used while capturing; kept local so persistence stays self-contained.
	/// </remarks>
	private static ClipboardHistoryEntryBase BuildTextEntry(PersistedTextEntry entry)
	{
		string trimmed = entry.Text.Trim();

		return WholeStringUrlRegex().IsMatch(trimmed)
			? new ClipboardUrlEntry
			{
				Text = entry.Text,
				Html = entry.Html,
				Rtf = entry.Rtf,
				Url = trimmed,
				Hash = entry.Hash
			}
			: new ClipboardTextEntry
			{
				Text = entry.Text,
				Html = entry.Html,
				Rtf = entry.Rtf,
				Hash = entry.Hash
			};
	}

	/// <summary>
	/// Maps a single persisted entry to its domain counterpart; <c>null</c> for unknown types.
	/// </summary>
	private static ClipboardHistoryEntryBase? ToDomainEntry(PersistedClipboardEntry entry) => entry switch
	{
		PersistedImageEntry image => new ClipboardImageEntry
		{
			OriginalPng = image.OriginalPng,
			Hash = image.Hash
		},
		PersistedFilesEntry files => new ClipboardFilesEntry
		{
			FileSystemEntries = [.. files.Files.Select(static x => new ClipboardFileSystemEntry(x.Path, x.IsFolder))],
			Hash = files.Hash
		},
		PersistedTextEntry text => BuildTextEntry(text),
		_ => null
	};

	/// <summary>
	/// Maps a single domain entry to its persisted counterpart; <c>null</c> for unknown types.
	/// </summary>
	private static PersistedClipboardEntry? ToPersistedEntry(ClipboardHistoryEntryBase entry) => entry switch
	{
		ClipboardImageEntry image => new PersistedImageEntry
		{
			OriginalPng = image.OriginalPng,
			Hash = image.Hash
		},
		ClipboardFilesEntry files => new PersistedFilesEntry
		{
			Files = [.. files.FileSystemEntries.Select(static x => new PersistedFileSystemEntry(x.Path, x.IsFolder))],
			Hash = files.Hash
		},
		// Catches ClipboardUrlEntry too (derived from ClipboardTextEntry); URL is re-derived on load.
		ClipboardTextEntry text => new PersistedTextEntry
		{
			Text = text.Text,
			Html = text.Html,
			Rtf = text.Rtf,
			Hash = text.Hash
		},
		_ => null
	};
	#endregion
}
