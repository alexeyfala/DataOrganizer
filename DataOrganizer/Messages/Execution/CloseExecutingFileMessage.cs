using DataOrganizer.Dto.Entities;

namespace DataOrganizer.Messages.Execution;

/// <summary>
/// Notification raised to request closing a file currently executing in the operating system.
/// </summary>
public sealed record CloseExecutingFileMessage(FileDto File);
