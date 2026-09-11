using System;

namespace DataOrganizer.Messages.Editor;

/// <summary>
/// Notification raised to request when folder expanded or collapsed.
/// </summary>
public sealed record FolderExpandedChangedMessage(Guid Id, bool IsExpanded);
