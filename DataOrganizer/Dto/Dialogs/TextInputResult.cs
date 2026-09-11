namespace DataOrganizer.Dto.Dialogs;

/// <summary>
/// The text a dialog returns, valid only when the user confirmed the input.
/// </summary>
public sealed record TextInputResult(bool IsConfirmed = false, string? Value = null);
