namespace DataOrganizer.Messages;

/// <summary>
/// Notification raised to request displaying (<c>True</c>) or hiding (<c>False</c>) a progress bar in the editor.
/// </summary>
public sealed record ShowProgressBarMessage(bool IsVisible);
