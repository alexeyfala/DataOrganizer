using DataOrganizer.Enums.Exchange;
using Hierarchy.Dto;

namespace DataOrganizer.Dto.Exchange;

/// <summary>
/// Result of an import: the imported objects and how they were put into the tree.
/// </summary>
/// <param name="Imported">The imported objects.</param>
/// <param name="Variant">How the imported objects were put into the tree.</param>
public sealed record ImportDataResult(
	LoadedHierarchy Imported,
	ImportMode Variant);
