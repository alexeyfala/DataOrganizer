using System;

namespace FileExecution.Messages;

/// <summary>
/// Notification raised to request closing a file currently executing in the operating system.
/// </summary>
public sealed record CloseExecutingFileMessage(Guid FileId);
