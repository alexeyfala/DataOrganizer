using DataOrganizer.Infrastructure.Settings.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Infrastructure.Settings.Interfaces;

/// <summary>
/// Adjusts the background services to saved settings while the application runs.
/// </summary>
public interface ISettingsApplier
{
	#region Methods
	/// <summary>
	/// Applies saved settings, given what the replaced settings had on.
	/// </summary>
	Task ApplySettingsAsync(
		AppSettings settings,
		bool wasPersistingClipboard,
		int previousAutoLockMinutes,
		CancellationToken token = default);
	#endregion
}
