using System.Threading;
using System.Threading.Tasks;

namespace Clipboard.Interfaces;

/// <summary>
/// Tracking of the system clipboard in the background.
/// </summary>
public interface IClipboardLogRunner
{
	#region Properties
	/// <summary>
	/// <c>True</c> while the background polling is active.
	/// </summary>
	bool IsRunning { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Clears the entries and forgets the last observed payload, without touching the system clipboard.
	/// </summary>
	Task ClearEntriesAsync();

	/// <summary>
	/// Starts background polling. Safe to call more than once.
	/// </summary>
	Task StartAsync(CancellationToken token = default);

	/// <summary>
	/// Stops background polling without disposing the service.
	/// </summary>
	void Stop();
	#endregion
}
