using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Exchange;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Exchange;

/// <summary>
/// Provides methods to import/export data.
/// </summary>
public interface IDataExchangeService
{
	#region Methods
	/// <summary>
	/// Exports data.
	/// </summary>
	Task ExportDataAsync(CancellationToken token = default);

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
