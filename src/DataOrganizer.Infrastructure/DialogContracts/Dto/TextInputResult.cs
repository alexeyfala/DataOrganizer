namespace DataOrganizer.Infrastructure.DialogContracts.Dto;

/// <summary>
/// The text a dialog returns, valid only when the user confirmed the input.
/// </summary>
/// <param name="IsConfirmed">Whether the user confirmed the input.</param>
/// <param name="Value">Entered text.</param>
public sealed record TextInputResult(bool IsConfirmed = false, string? Value = null);
