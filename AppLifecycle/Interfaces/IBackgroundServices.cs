using System.Threading;
using System.Threading.Tasks;

namespace AppLifecycle.Interfaces;

/// <summary>
/// Starts and stops the services that run in the background for the whole application: the hotkey tracking, the
/// clipboard history with its saving and the auto-lock countdown.
/// </summary>
public interface IBackgroundServices
{
	#region Methods
	/// <summary>
	/// Starts the saving of the clipboard history and the services the settings turn on.
	/// </summary>
	void Start(CancellationToken token = default);

	/// <summary>
	/// Stops the services.
	/// </summary>
	Task StopAsync();
	#endregion
}
