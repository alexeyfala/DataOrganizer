using DataOrganizer.Dto.Settings;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Settings;

/// <summary>
/// Ends the settings session once the settings are closed.
/// </summary>
public interface ISettingsCommit
{
	#region Methods
	/// <summary>
	/// Saves and applies the edits marked to be saved, or rolls the previewed theme back; returns the saved settings,
	/// <c>null</c> when nothing is saved.
	/// </summary>
	Task<AppSettings?> EndAsync(CancellationToken token = default);
	#endregion
}
