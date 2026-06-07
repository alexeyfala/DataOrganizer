using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using System.Collections.Generic;

namespace DataOrganizer.DTO;

public sealed class ImportDataResult
{
	#region Properties
	/// <summary>
	/// Imported items.
	/// </summary>
	public required IEnumerable<ExplorerModelBaseDto> ImportedItems { get; init; }

	/// <inheritdoc cref="ImportListVariant" />
	public required ImportListVariant Variant { get; init; }
	#endregion
}
