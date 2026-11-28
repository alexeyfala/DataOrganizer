namespace DataOrganizer.Infrastructure.Notifications.Messages;

/// <summary>
/// Notification raised to request displaying (<c>True</c>) or hiding (<c>False</c>) a progress bar in the editor.
/// </summary>
/// <param name="IsVisible">Whether the progress bar is shown.</param>
public sealed record ShowProgressBarMessage(bool IsVisible);
