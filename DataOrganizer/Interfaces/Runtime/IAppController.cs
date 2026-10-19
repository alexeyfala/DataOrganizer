using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Runtime;

/// <summary>
/// Launches the application and stops it when its exit is requested.
/// </summary>
public interface IAppController
{
	#region Methods
	/// <summary>
	/// Launches the application.
	/// </summary>
	Task LaunchAppAsync(CancellationToken token = default);
	#endregion
}
