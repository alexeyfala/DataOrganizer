using DataOrganizer.Dto.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Shows and hides the protected contents of explorer objects within a session.
/// </summary>
public interface IContentVisibility
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

	/// <summary>
	/// Hides contents of the whole hierarchy.
	/// </summary>
	void HideAllContents(IEnumerable<ExplorerItemDtoBase> hierarchy);

	/// <summary>
	/// Hides file contents.
	/// </summary>
	void HideFileContents(FileDto file);

	/// <summary>
	/// Hides file contents in folder.
	/// </summary>
	void HideFolderContents(FolderDto folder);

	/// <summary>
	/// Shows file contents.
	/// </summary>
	Task<bool> ShowFileContentsAsync(FileDto file, CancellationToken token = default);

	/// <summary>
	/// Shows file contents in folder.
	/// </summary>
	Task ShowFolderContentsAsync(FolderDto folder, CancellationToken token = default);
	#endregion
}
