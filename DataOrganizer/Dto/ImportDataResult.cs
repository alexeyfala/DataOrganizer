using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;
using System.Collections.Generic;

namespace DataOrganizer.Dto;

public sealed record ImportDataResult(
	IEnumerable<ExplorerModelBaseDto> ImportedItems,
	ImportMode Variant);
