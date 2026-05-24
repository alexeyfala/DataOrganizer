using DataOrganizer.DTO.Entities.Models;

namespace DataOrganizer.Messages;

/// <summary>
/// Notification raised to request closing a file currently executing in the operating system.
/// </summary>
public sealed record CloseExecutingFileMessage(FileModelDto File);
