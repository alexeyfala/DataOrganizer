using DataOrganizer.Hierarchy.Dto;

namespace DataOrganizer.SystemIntegration.Clipboard.Messages;

/// <summary>
/// Notification raised when a file leaves the copy history.
/// </summary>
/// <param name="File">File that left the copy history.</param>
public sealed record CopyHistoryFileRemovedMessage(FileDto File);
