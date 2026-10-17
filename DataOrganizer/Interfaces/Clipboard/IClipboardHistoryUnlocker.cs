using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Clipboard;

/// <summary>
/// Opens the saved clipboard history with its password before the history is shown.
/// </summary>
public interface IClipboardHistoryUnlocker
{
	#region Methods
	/// <summary>
	/// Asks for the password while the saved history is locked, unlocking and merging it; a cancelled prompt leaves the
	/// history in memory only.
	/// </summary>
	Task UnlockIfRequiredAsync();
	#endregion
}
