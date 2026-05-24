namespace DataOrganizer.Messages;

/// <summary>
/// Notification raised to request when read-only mode changes in the editor.
/// </summary>
public sealed record EditorReadOnlyModeChangedMessage(bool IsReadOnly);
