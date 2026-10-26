using Hierarchy.Dto;

namespace DataOrganizer.Messages.Editor;

/// <summary>
/// Notification raised to request closing a file open in the built-in editor.
/// </summary>
public sealed record CloseEditingFileMessage(FileDto File);
