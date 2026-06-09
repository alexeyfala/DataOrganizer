using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using System.Collections.Generic;

namespace DataOrganizer.DTO;

public sealed record ImportDataResult(
	IEnumerable<ExplorerModelBaseDto> ImportedItems,
	ImportListVariant Variant);
