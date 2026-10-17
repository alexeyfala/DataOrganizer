using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Updates;

/// <summary>
/// Notifies about a newer application version and opens its release page on request.
/// </summary>
public interface IUpdateNotifier
{
	#region Methods
	/// <summary>
	/// Shows the update notification when a newer version is available.
	/// </summary>
	Task NotifyIfUpdateAvailableAsync(CancellationToken token = default);
	#endregion
}
