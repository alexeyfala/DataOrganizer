using Hierarchy.Dto;

namespace Clipboard.Messages;

/// <summary>
/// Notification raised when a file leaves the copy history.
/// </summary>
public sealed record CopyHistoryFileRemovedMessage(FileDto File);
