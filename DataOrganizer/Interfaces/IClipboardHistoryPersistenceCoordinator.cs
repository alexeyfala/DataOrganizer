using DataOrganizer.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Coordinates on-disk persistence (encryption + storage) of the clipboard history. Listens to the
/// service's change notifications and saves / erases accordingly.
/// </summary>
public interface IClipboardHistoryPersistenceCoordinator : IAsyncDisposable
{
	#region Properties
	/// <summary>
	/// <c>True</c> when persistence is enabled in settings but the on-disk history has not
	/// been unlocked yet this session (the caller should prompt for a password).
	/// </summary>
	bool RequiresUnlock { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Disables on-disk persistence: erases the stored journal and key and forgets the
	/// in-session key. Called when the user turns persistence off.
	/// </summary>
	void DisablePersistence();

	/// <summary>
	/// Unlocks (or creates) the on-disk history with <paramref name="password" />, merges the
	/// loaded entries into the service and enables persistence for the session.
	/// </summary>
	Task<ClipboardHistoryUnlockStatus> TryUnlockAndMergeAsync(byte[] password, CancellationToken token = default);
	#endregion
}
