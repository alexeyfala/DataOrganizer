namespace DataOrganizer.DTO;

public sealed record KeyValueInputParameters(
	string DefaultButtonText,
	string? Key = null,
	string? KeyHint = null,
	string? Value = null,
	string? ValueHint = null);
