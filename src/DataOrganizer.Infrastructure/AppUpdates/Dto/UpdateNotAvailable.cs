namespace DataOrganizer.Infrastructure.AppUpdates.Dto;

/// <summary>
/// No newer version is available, or the check did not run.
/// </summary>
public sealed record UpdateNotAvailable : UpdateCheckResult;
