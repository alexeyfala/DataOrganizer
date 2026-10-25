using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Views;

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
