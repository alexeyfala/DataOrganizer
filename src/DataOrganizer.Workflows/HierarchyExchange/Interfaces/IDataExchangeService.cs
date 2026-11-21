using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Workflows.HierarchyExchange.Dto;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Workflows.HierarchyExchange.Interfaces;

/// <summary>
/// Provides methods to import/export data.
/// </summary>
public interface IDataExchangeService
{
	#region Methods
	/// <summary>
	/// Exports every object; the file picker shows the counts of <paramref name="hierarchy" />.
	/// </summary>
	Task ExportDataAsync(IEnumerable<ExplorerItemDtoBase> hierarchy, CancellationToken token = default);

	/// <summary>
	/// Exports the objects whose identifiers are in <paramref name="ids" />; the file picker shows their counts, taken from
	/// <paramref name="hierarchy" />.
	/// </summary>
	Task ExportPartAsync(
		IEnumerable<ExplorerItemDtoBase> hierarchy,
		IReadOnlySet<Guid> ids,
		CancellationToken token = default);

	/// <summary>
	/// Imports data.
	/// </summary>
	/// <remarks>
	/// It takes no cancellation: the import changes the database in several steps, a stop between them would leave it half
	/// changed, and the rollback to the copy has to run to its end.
	/// </remarks>
	Task<ImportDataResult?> ImportDataAsync(IReadOnlyCollection<ExplorerItemDtoBase> hierarchy);
	#endregion
}
