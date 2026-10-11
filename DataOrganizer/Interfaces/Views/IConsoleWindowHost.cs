using DataOrganizer.Windows;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Views;

/// <summary>
/// Owns the lifetime of <see cref="ConsoleWindow" />: keeps the log written before the window opens,
/// then configures and shows the window on demand.
/// </summary>
public interface IConsoleWindowHost
{
	#region Methods
	/// <summary>
	/// Creates, configures and shows <see cref="ConsoleWindow" />.
	/// </summary>
	Task ConfigureAndShowAsync();

	/// <summary>
	/// Appends <paramref name="text" /> to the log of the console.
	/// </summary>
	void Write(string text);
	#endregion
}
