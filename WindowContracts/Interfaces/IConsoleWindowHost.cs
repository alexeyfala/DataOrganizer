using System.Threading.Tasks;

namespace WindowContracts.Interfaces;

/// <summary>
/// Owns the lifetime of the console window: configures and shows the window on demand.
/// </summary>
public interface IConsoleWindowHost
{
	#region Methods
	/// <summary>
	/// Creates, configures and shows the console window.
	/// </summary>
	Task ConfigureAndShowAsync();
	#endregion
}
