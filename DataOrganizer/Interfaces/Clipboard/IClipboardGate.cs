using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Clipboard;

/// <summary>
/// Application-wide mutual exclusion around clipboard mutations, so operations from different
/// clipboard components cannot interleave.
/// </summary>
public interface IClipboardGate
{
	#region Methods
	/// <summary>
	/// Releases the gate.
	/// </summary>
	void Release();

	/// <summary>
	/// Asynchronously waits to enter the gate.
	/// </summary>
	Task WaitAsync();
	#endregion
}
