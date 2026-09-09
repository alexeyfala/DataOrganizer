using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;
using System.Collections.Generic;

namespace DataOrganizer.Dto;

public sealed record ImportDataResult(
	IEnumerable<ExplorerItemDtoBase> ImportedItems,
	ImportMode Variant);
