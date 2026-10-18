using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;

namespace DataOrganizer.Dto;

public sealed record ImportDataResult(
	LoadedHierarchy Imported,
	ImportMode Variant);
