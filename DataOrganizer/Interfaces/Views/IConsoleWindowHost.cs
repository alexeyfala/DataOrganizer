using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Owns the lifetime of <see cref="ConsoleWindow" /> and its <see cref="ConsoleViewModel" />:
/// creates the view model eagerly so the logger sink can attach to it, then configures and
/// shows the window on demand.
/// </summary>
public interface IConsoleWindowHost
{
	#region Properties
	/// <summary>
	/// The single <see cref="ConsoleViewModel" /> instance backed by this host.
	/// </summary>
	ConsoleViewModel ViewModel { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Creates, configures and shows <see cref="ConsoleWindow" />.
	/// </summary>
	Task ConfigureAndShowAsync();
	#endregion
}
