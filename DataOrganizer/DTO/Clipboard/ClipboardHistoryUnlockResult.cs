using DataOrganizer.Enums.Clipboard;
using System.Collections.Generic;

namespace DataOrganizer.DTO.Clipboard;

/// <summary>
/// Result of unlocking the persisted clipboard history: the outcome and the entries
/// loaded from the previous session (empty unless <see cref="Status" /> is
/// <see cref="ClipboardHistoryLogStatus.Unlocked" />).
/// </summary>
/// <param name="Status">The unlock outcome.</param>
/// <param name="Entries">Entries loaded from disk, newest first.</param>
public sealed record ClipboardHistoryUnlockResult(
	ClipboardHistoryLogStatus Status,
	IReadOnlyList<ClipboardHistoryEntryBase> Entries);
