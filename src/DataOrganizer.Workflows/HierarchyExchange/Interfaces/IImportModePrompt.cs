using DataOrganizer.Workflows.HierarchyExchange.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Workflows.HierarchyExchange.Interfaces;

/// <summary>
/// Asks the user how imported objects join the existing ones.
/// </summary>
public interface IImportModePrompt
{
	#region Methods
	/// <summary>
	/// Selects import variant.
	/// </summary>
	Task<ImportMode> SelectImportModeAsync(CancellationToken token = default);
	#endregion
}
