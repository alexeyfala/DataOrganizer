using DataOrganizer.Enums.Clipboard;

namespace DataOrganizer.Messages;

/// <summary>
/// Notification raised by the clipboard history service when its in-memory entries change.
/// </summary>
public sealed record ClipboardHistoryChangedMessage(ClipboardHistoryChangeKind Kind);
