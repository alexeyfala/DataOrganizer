using DataOrganizer.Windows;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Views;

/// <summary>
/// Owns the lifetime of <see cref="ConsoleWindow" />: configures and shows the window on demand.
/// </summary>
public interface IConsoleWindowHost
{
	#region Methods
	/// <summary>
	/// Creates, configures and shows <see cref="ConsoleWindow" />.
	/// </summary>
	Task ConfigureAndShowAsync();
	#endregion
}
