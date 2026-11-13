using System;

namespace DataOrganizer.Documents.Editor.Messages;

/// <summary>
/// Notification raised to show object in the editor.
/// </summary>
public sealed record ShowInEditorMessage(Guid Id);
