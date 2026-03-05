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
	#endregion
}
