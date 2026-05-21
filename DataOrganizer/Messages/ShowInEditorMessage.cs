using Avalonia.Controls;
using System;

namespace DataOrganizer.Messages;

/// <summary>
/// Notification raised to show object in the editor.
/// </summary>
public sealed record ShowInEditorMessage(Guid Id, Window Window);
