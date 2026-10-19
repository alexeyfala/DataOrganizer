using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Exchange;

namespace DataOrganizer.Dto.Exchange;

public sealed record ImportDataResult(
	LoadedHierarchy Imported,
	ImportMode Variant);
