using Repository.DTO;

namespace DataOrganizer.DTO;

public sealed record EditingHotkeysResult(bool IsSaved, CodeMaskPair[] NewHotkeys);
