using System;

namespace EditorMessages.Messages;

/// <summary>
/// Notification raised to show object in the editor.
/// </summary>
public sealed record ShowInEditorMessage(Guid Id);
