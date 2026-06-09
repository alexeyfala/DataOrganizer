namespace DataOrganizer.DTO;

public sealed record ValueIsValidPair(bool IsValid = false, string? Value = null);
