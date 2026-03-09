using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.Enums;
using Entities.Abstract;
using System.Collections.Generic;
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
	/// Appends data from SQLite database.
	/// </summary>
	Task<bool> AppendFromSQLiteAsync(
		string filePath,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default);

	/// <summary>
	/// Exports data.
	/// </summary>
	Task ExportDataAsync(CancellationToken token = default);

	/// <summary>
	/// Imports data.
	/// </summary>
	Task<bool> ImportDataAsync(Collection<ExplorerModelBaseDto> hierarchy, CancellationToken token = default);

	/// <summary>
	/// Imports entities.
	/// </summary>
	Task<bool> ImportEntitiesAsync(
		ExplorerModelBase[] entities,
		ImportListVariant variant,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token);

	/// <summary>
	/// Replaces with data from SQLite database.
	/// </summary>
	Task<bool> ReplaceFromSQLiteAsync(
		string filePath,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default);
	#endregion
}
