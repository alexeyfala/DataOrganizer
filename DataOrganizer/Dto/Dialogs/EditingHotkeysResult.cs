using Repository.Dto;

namespace DataOrganizer.Dto.Dialogs;

/// <summary>
/// The hotkeys a file has after the hotkey editor, applied only when the user saved them.
/// </summary>
public sealed record EditingHotkeysResult(bool IsSaved, KeyStroke[] NewHotkeys);
