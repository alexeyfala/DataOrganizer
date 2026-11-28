using System;

namespace DataOrganizer.Documents.Editor.Messages;

/// <summary>
/// Notification raised to show object in the editor.
/// </summary>
/// <param name="Id">Id of the object to show.</param>
public sealed record ShowInEditorMessage(Guid Id);
