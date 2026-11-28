using System;

namespace DataOrganizer.SystemIntegration.FileExecution.Messages;

/// <summary>
/// Notification raised to request closing a file currently executing in the operating system.
/// </summary>
/// <param name="FileId">Id of the file to close.</param>
public sealed record CloseExecutingFileMessage(Guid FileId);
