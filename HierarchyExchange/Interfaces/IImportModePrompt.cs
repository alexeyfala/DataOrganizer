using HierarchyExchange.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace HierarchyExchange.Interfaces;

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
