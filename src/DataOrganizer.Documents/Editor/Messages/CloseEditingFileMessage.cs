using DataOrganizer.Hierarchy.Dto;

namespace DataOrganizer.Documents.Editor.Messages;

/// <summary>
/// Notification raised to request closing a file open in the built-in editor.
/// </summary>
public sealed record CloseEditingFileMessage(FileDto File);
