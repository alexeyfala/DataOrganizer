namespace DataOrganizer.Documents.Editor.Messages;

/// <summary>
/// Notification raised to request when read-only mode changes in the editor.
/// </summary>
/// <param name="IsReadOnly">Whether the editor is now read-only.</param>
public sealed record EditorReadOnlyModeChangedMessage(bool IsReadOnly);
