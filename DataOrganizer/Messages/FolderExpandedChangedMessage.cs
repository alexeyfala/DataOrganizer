using System;

namespace DataOrganizer.Messages;

/// <summary>
/// Notification raised to request when folder expanded or collapsed.
/// </summary>
public sealed record FolderExpandedChangedMessage(Guid Id, bool IsExpanded);
