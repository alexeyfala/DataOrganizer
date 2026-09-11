namespace DataOrganizer.Dto.Dialogs;

/// <summary>
/// A key and an optional value entered by the user.
/// </summary>
public sealed record KeyValueInput(string Key, string? Value = null);
