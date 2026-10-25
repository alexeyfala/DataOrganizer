using Repository.Dto;

namespace DialogContracts.Dto;

public sealed record EditingHotkeysResult(bool IsSaved, KeyStroke[] NewHotkeys);
