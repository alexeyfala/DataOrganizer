using DataOrganizer.Windows;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Performs application lifecycle control.
/// </summary>
public interface IAppController
{
	#region Methods
	/// <summary>
	/// Launches the application.
	/// </summary>
	Task LaunchAppAsync(ConsoleWindow? console, CancellationToken token = default);
	#endregion
}
