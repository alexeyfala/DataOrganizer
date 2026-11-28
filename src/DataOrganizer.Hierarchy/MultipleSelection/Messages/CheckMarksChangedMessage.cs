using System;
using System.Collections.Generic;

namespace DataOrganizer.Hierarchy.MultipleSelection.Messages;

/// <summary>
/// Notification raised when objects of the tree are checked or unchecked.
/// </summary>
/// <param name="Ids">Ids of the objects whose check mark changed.</param>
public sealed record CheckMarksChangedMessage(IReadOnlySet<Guid> Ids);
