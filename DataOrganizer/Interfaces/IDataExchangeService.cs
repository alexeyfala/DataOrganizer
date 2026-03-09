using DataOrganizer.DTO.Entities.Abstract;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

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
	Task<bool> ImportDataAsync(Collection<ExplorerModelBaseDto> hierarchy, CancellationToken token = default);
	#endregion
}
