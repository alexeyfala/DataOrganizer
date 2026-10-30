using Hierarchy.Dto;

namespace EditorMessages.Messages;

/// <summary>
/// Notification raised to request closing a file open in the built-in editor.
/// </summary>
public sealed record CloseEditingFileMessage(FileDto File);
