using Hierarchy.Dto;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Session keys that open the protected contents of explorer objects.
/// </summary>
public interface IContentKeys
{
	#region Methods
	/// <summary>
	/// Drops the session keys of every password keeper.
	/// </summary>
	void DiscardAllKeys();

	/// <summary>
	/// Drops the session keys of a folder and of every folder beneath it, whatever their contents show.
	/// </summary>
	void DiscardKeys(FolderDto folder);
	#endregion
}
