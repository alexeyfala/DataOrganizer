namespace FileExecution.Dto;

/// <summary>
/// Program chosen to open a file; the default leaves the choice to the operating system.
/// </summary>
/// <param name="IsCancelled"><c>True</c> when the user gave up choosing, so the file is not opened.</param>
/// <param name="AppPath">Executable of the chosen program; <c>null</c> when the operating system opens the file.</param>
public readonly record struct AppResolution(bool IsCancelled, string? AppPath);
