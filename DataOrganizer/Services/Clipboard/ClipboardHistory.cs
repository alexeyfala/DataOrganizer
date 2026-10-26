using CommunityToolkit.Mvvm.Messaging;
using Cysharp.Text;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Messages.Clipboard;
using DataOrganizer.Models.Clipboard;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using TextProcessing.Constants;

namespace DataOrganizer.Services.Clipboard;

/// <inheritdoc cref="IClipboardHistory" />
public sealed class ClipboardHistory : IClipboardHistory
{
	#region Properties
	/// <inheritdoc />
	public ObservableCollection<ClipboardLogEntryBase> Entries { get; } = [];
	#endregion

	#region Data
	/// <summary>
	/// Maximum number of entries kept in log.
	/// </summary>
	internal const int LogLimit = 100;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <summary>
	/// Hash of the most recently observed clipboard payload.
	/// </summary>
	private byte[]? _lastHash;

	/// <summary>
	/// The entry just restored to the system clipboard, awaiting re-baseline to the clipboard's
	/// actual representation on the next poll tick that sees its marker.
	/// </summary>
	private ClipboardLogEntryBase? _restoredEntry;
	#endregion

	#region Constructors
	public ClipboardHistory(ILogger logger, IMessenger messenger)
	{
		_logger = logger;

		_messenger = messenger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public int Clear(bool preservePinned)
	{
		if (preservePinned)
		{
			for (int i = Entries.Count - 1; i >= 0; i--)
			{
				if (!Entries[i].IsPinned)
				{
					Entries.RemoveAt(i);
				}
			}
		}
		else
		{
			Entries.Clear();
		}

		int remaining = Entries.Count;

		_lastHash = null;

		ClearActive();

		return remaining;
	}

	/// <inheritdoc />
	public void HandleClipboardCleared()
	{
		if (_restoredEntry is not null)
		{
			return;
		}

		_lastHash = null;

		ClearActive();
	}

	/// <inheritdoc />
	public void HandleNewPayload(
		byte[] hash,
		Func<ClipboardLogEntryBase> entryFactory,
		bool isSensitive,
		bool isRestored)
	{
		if (_restoredEntry is { } restored)
		{
			_restoredEntry = null;

			// A payload without the marker was copied over the restored entry, so it goes the usual way.
			if (isRestored)
			{
				_lastHash = hash;

				if (!HashEquals(restored.Hash, hash))
				{
					RebaselineRestored(restored, entryFactory());
				}

				return;
			}
		}

		if (IsEchoOrUnchanged(hash))
		{
			return;
		}

		if (isSensitive)
		{
			_logger.LogWarning("Skipping clipboard entry: a sensitivity marker was detected.");

			return;
		}

		InsertOrMoveToTop(hash, entryFactory);
	}

	/// <inheritdoc />
	public void MarkRestored(ClipboardLogEntryBase entry, bool keepPosition)
	{
		_restoredEntry = entry;

		// With a pinned-open window, only highlight so entries don't jump under the user.
		if (keepPosition)
		{
			MarkActive(entry);
		}
		else
		{
			MoveToTop(entry);
		}
	}

	/// <inheritdoc />
	public void Merge(IReadOnlyList<ClipboardLogEntryBase> entries)
	{
		try
		{
			// Pinned go atop (exempt from the cap), unpinned are appended (capped) — keeps the pinned-atop
			// invariant. Does not raise a change notification — the caller decides whether to persist.
			foreach (ClipboardLogEntryBase entry in entries)
			{
				if (Entries.FirstOrDefault(x => HashEquals(x.Hash, entry.Hash)) is { } existing)
				{
					// Promote the in-memory copy if the saved one was pinned; preserves the pin dedup used to drop.
					if (entry.IsPinned && !existing.IsPinned)
					{
						// Read before flipping: the new pin appends to the end of the pinned block.
						int target = GetPinnedCount();

						existing.IsPinned = true;

						Entries.Move(Entries.IndexOf(existing), target);
					}

					continue;
				}

				if (entry.IsPinned)
				{
					Entries.Insert(GetPinnedCount(), entry);

					continue;
				}

				if (Entries.Count - GetPinnedCount() >= LogLimit)
				{
					continue;
				}

				Entries.Add(entry);
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <inheritdoc />
	public bool Remove(ClipboardLogEntryBase entry)
	{
		int index = Entries.IndexOf(entry);

		if (index < 0)
		{
			return false;
		}

		bool wasActive = entry.IsActive;

		Entries.RemoveAt(index);

		// Removing the entry currently on the system clipboard: forget the change-detection baseline so
		// the emptied clipboard is not mistaken for the previous payload on the next poll tick.
		if (wasActive)
		{
			_lastHash = null;
		}

		return true;
	}

	/// <inheritdoc />
	public void TogglePin(ClipboardLogEntryBase entry)
	{
		try
		{
			int index = Entries.IndexOf(entry);

			if (index < 0)
			{
				return;
			}

			bool pin = !entry.IsPinned;

			// Read before flipping: once unpinned in place, PinnedCount would stop at this entry and read 0.
			int pinnedCount = GetPinnedCount();

			entry.IsPinned = pin;

			// Pinning appends to the end of the pinned block; unpinning drops just below the remaining pins.
			int target = pin ? pinnedCount : pinnedCount - 1;

			if (index != target)
			{
				Entries.Move(index, target);
			}

			NotifyChanged(ClipboardLogChangeKind.Updated);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <summary>
	/// Hashes a text payload together with whether it carries companion formats, so the same text
	/// copied as plain (e.g. Notepad) and as formatted (e.g. Word) is treated as two distinct entries.
	/// Trade-off: identical text with only different HTML/RTF formatting hashes the same and is deduped.
	/// </summary>
	internal static byte[] ComputeTextEntryHash(
		string text,
		string? html,
		string? rtf)
	{
		using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

		hash.AppendData(TextDefaults
			.Encoding
			.GetBytes(text));

		// Only the presence of companion formats — not their payloads, which Office can re-render
		// non-deterministically per read (delayed rendering) and would change the hash every poll tick.
		hash.AppendData([(byte)((html is null ? 0 : 1) | (rtf is null ? 0 : 2))]);

		return hash.GetHashAndReset();
	}

	/// <summary>
	/// Hashes the file list, including kind marker so a folder and a file with the same
	/// path don't collide. Order-sensitive — selection order is preserved by the OS.
	/// </summary>
	internal static byte[] HashFiles(IReadOnlyList<ClipboardFileSystemEntry> entries)
	{
		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		foreach (ClipboardFileSystemEntry entry in entries)
		{
			builder.Append(entry.IsFolder ? 'D' : 'F');

			builder.Append('|');

			builder.Append(entry.Path);

			builder.Append('\0');
		}

		return SHA256.HashData(TextDefaults.Encoding.GetBytes(builder.ToString()));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when two payload hashes are byte-equal.
	/// </summary>
	private static bool HashEquals(byte[] left, byte[] right) => left.AsSpan().SequenceEqual(right);

	/// <summary>
	/// Clears the active-entry highlight from every entry.
	/// </summary>
	private void ClearActive()
	{
		if (Entries.FirstOrDefault(static entry => entry.IsActive) is not { } active)
		{
			return;
		}

		active.IsActive = false;
	}

	/// <summary>
	/// Returns the number of pinned entries, which by invariant form a contiguous block atop <see cref="Entries" />;
	/// also the index of the first unpinned entry.
	/// </summary>
	private int GetPinnedCount() => Entries.Count(x => x.IsPinned);

	/// <summary>
	/// Inserts <paramref name="entry" /> below the pinned block and enforces the log cap on
	/// unpinned entries only (pinned ones are exempt from trimming).
	/// </summary>
	private void InsertAtTop(ClipboardLogEntryBase entry)
	{
		Entries.Insert(GetPinnedCount(), entry);

		while (Entries.Count - GetPinnedCount() > LogLimit)
		{
			// The last entry is always unpinned (pinned ones sit atop), so trimming never drops a pin.
			Entries.RemoveAt(Entries.Count - 1);
		}

		MarkActive(entry);

		NotifyChanged(ClipboardLogChangeKind.Updated);

		NotifyEntryCountChanged();
	}

	/// <summary>
	/// Moves an existing entry with the same <paramref name="hash" /> to the top, otherwise inserts a new one.
	/// </summary>
	private void InsertOrMoveToTop(byte[] hash, Func<ClipboardLogEntryBase> entryFactory)
	{
		if (Entries.FirstOrDefault(x => HashEquals(x.Hash, hash)) is { } existing)
		{
			_logger.LogDebug($"Moving existing clipboard entry to top: {existing.GetType().Name}.");

			MoveToTop(existing);

			return;
		}

		ClipboardLogEntryBase entry = entryFactory();

		_logger.LogDebug($"Captured new clipboard entry: {entry.GetType().Name}.");

		InsertAtTop(entry);
	}

	/// <summary>
	/// <c>True</c> when <paramref name="hash" /> is a self-echo (from our own restore) or
	/// matches the last observed payload.
	/// </summary>
	private bool IsEchoOrUnchanged(byte[] hash)
	{
		if (_lastHash is { } prev && HashEquals(prev, hash))
		{
			return true;
		}

		_lastHash = hash;

		return false;
	}

	/// <summary>
	/// Marks <paramref name="entry" /> (already in <see cref="Entries" />) as the active
	/// current-clipboard entry, clearing the flag on all others.
	/// </summary>
	private void MarkActive(ClipboardLogEntryBase entry)
	{
		if (Entries.FirstOrDefault(static other => other.IsActive) is { } previous && !ReferenceEquals(previous, entry))
		{
			previous.IsActive = false;
		}

		entry.IsActive = true;
	}

	/// <summary>
	/// Moves <paramref name="entry" /> to the top of its block.
	/// </summary>
	private void MoveToTop(ClipboardLogEntryBase entry)
	{
		try
		{
			int index = Entries.IndexOf(entry);

			if (index < 0)
			{
				InsertAtTop(entry);

				return;
			}

			MarkActive(entry);

			// Pinned entries go to the very top; unpinned ones stay below the pinned block.
			int target = entry.IsPinned ? 0 : GetPinnedCount();

			if (index == target)
			{
				return;
			}

			Entries.Move(index, target);

			NotifyChanged(ClipboardLogChangeKind.Updated);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <summary>
	/// Publishes a <see cref="ClipboardLogChangedMessage" /> so listeners can react to a change.
	/// </summary>
	private void NotifyChanged(ClipboardLogChangeKind kind) => _messenger.Send(new ClipboardLogChangedMessage(kind));

	/// <summary>
	/// Publishes a <see cref="ClipboardLogEntryCountChangedMessage" /> after entries are added or removed.
	/// </summary>
	private void NotifyEntryCountChanged() => _messenger.Send(new ClipboardLogEntryCountChangedMessage());

	/// <summary>
	/// Replaces the just-restored <paramref name="restored" /> entry with <paramref name="rebaselined" />
	/// (the content as the clipboard handed it back), so its persisted hash matches what a future
	/// session will capture for the same content.
	/// </summary>
	private void RebaselineRestored(ClipboardLogEntryBase restored, ClipboardLogEntryBase rebaselined)
	{
		rebaselined.IsPinned = restored.IsPinned;

		int index = Entries.IndexOf(restored);

		if (index < 0)
		{
			InsertAtTop(rebaselined);

			return;
		}

		Entries[index] = rebaselined;

		MarkActive(rebaselined);

		_logger.LogDebug($"Re-baselined restored clipboard entry: {rebaselined.GetType().Name}.");

		NotifyChanged(ClipboardLogChangeKind.Updated);
	}
	#endregion
}
