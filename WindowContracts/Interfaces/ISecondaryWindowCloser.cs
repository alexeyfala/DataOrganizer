using System.Threading.Tasks;

namespace WindowContracts.Interfaces;

/// <summary>
/// Closes the windows beside the main window: the console and the notepad.
/// </summary>
public interface ISecondaryWindowCloser
{
	#region Methods
	/// <summary>
	/// Closes the console and the notepad, and waits for a console that is still saving its log.
	/// </summary>
	Task CloseSecondaryWindowsAsync();
	#endregion
}
