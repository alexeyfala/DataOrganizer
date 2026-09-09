using Repository.Dto;

namespace DataOrganizer.Dto;

public sealed record EditingHotkeysResult(bool IsSaved, CodeMaskPair[] NewHotkeys);
