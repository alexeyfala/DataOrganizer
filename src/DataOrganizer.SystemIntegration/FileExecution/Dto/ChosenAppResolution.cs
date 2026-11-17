namespace DataOrganizer.SystemIntegration.FileExecution.Dto;

/// <summary>
/// The file opens in a program the user chose.
/// </summary>
/// <param name="AppPath">Executable of the chosen program.</param>
public sealed record ChosenAppResolution(string AppPath) : AppResolution;
