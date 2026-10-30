using Clipboard.Enums;

namespace Clipboard.Messages;

/// <summary>
/// Notification raised by the clipboard log service when its in-memory entries change.
/// </summary>
public sealed record ClipboardLogChangedMessage(ClipboardLogChangeKind Kind);
