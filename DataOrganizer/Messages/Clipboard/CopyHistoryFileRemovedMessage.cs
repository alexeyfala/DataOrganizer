using Hierarchy.Dto;

namespace DataOrganizer.Messages.Clipboard;

/// <summary>
/// Notification raised when a file leaves the copy history.
/// </summary>
public sealed record CopyHistoryFileRemovedMessage(FileDto File);
