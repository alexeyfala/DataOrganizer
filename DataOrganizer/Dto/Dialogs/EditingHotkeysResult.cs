using Repository.Dto;

namespace DataOrganizer.Dto.Dialogs;

public sealed record EditingHotkeysResult(bool IsSaved, KeyStroke[] NewHotkeys);
