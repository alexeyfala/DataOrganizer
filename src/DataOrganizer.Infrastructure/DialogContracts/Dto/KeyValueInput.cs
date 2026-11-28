namespace DataOrganizer.Infrastructure.DialogContracts.Dto;

/// <summary>
/// A key and an optional value entered by the user.
/// </summary>
/// <param name="Key">Entered key.</param>
/// <param name="Value">Entered value, if any.</param>
public sealed record KeyValueInput(string Key, string? Value = null);
