using DataOrganizer.Database.Repository.Dto;

namespace DataOrganizer.Infrastructure.DialogContracts.Dto;

/// <summary>
/// The hotkeys a file has after the hotkey editor, applied only when the user saved them.
/// </summary>
/// <param name="IsSaved">Whether the user saved the hotkeys.</param>
/// <param name="NewHotkeys">Hotkeys of the file after editing.</param>
public sealed record EditingHotkeysResult(bool IsSaved, KeyStroke[] NewHotkeys);
