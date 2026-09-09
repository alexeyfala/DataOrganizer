namespace DataOrganizer.Dto;

public sealed record ValueIsValidPair(bool IsValid = false, string? Value = null);
