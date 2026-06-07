using DataOrganizer.DTO.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Monitors global keyboard input.
/// </summary>
public interface IKeyboardInputHook : IDisposable
{
	#region Properties
	/// <summary>
	/// Maximum number of hotkeys.
	/// </summary>
	static int MaxHotkeys { get; } = 8;

	/// <summary>
	/// <c>True</c> when the hook is running.
	/// </summary>
	bool IsRunning { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Starts tracking global keyboard input.
	/// </summary>
	Task StartTrackingAsync(IEnumerable<ExplorerModelBaseDto> hierarchy, CancellationToken token = default);

	/// <summary>
	/// Stops tracking global keyboard input.
	/// </summary>
	Task StopTrackingAsync(CancellationToken token = default);
	#endregion
}
