using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Owns the process-wide global keyboard hook.
/// </summary>
public interface IGlobalHookRunner : IDisposable
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the hook is running.
	/// </summary>
	bool IsRunning { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Starts the hook and waits until it is actually running.
	/// </summary>
	Task StartAsync(CancellationToken token = default);

	/// <summary>
	/// Stops the hook and waits until it is actually stopped.
	/// </summary>
	Task StopAsync(CancellationToken token = default);
	#endregion
}
