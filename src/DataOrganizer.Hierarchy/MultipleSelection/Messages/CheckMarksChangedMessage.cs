using System;
using System.Collections.Generic;

namespace DataOrganizer.Hierarchy.MultipleSelection.Messages;

/// <summary>
/// Notification raised when objects of the tree are checked or unchecked.
/// </summary>
public sealed record CheckMarksChangedMessage(IReadOnlySet<Guid> Ids);
