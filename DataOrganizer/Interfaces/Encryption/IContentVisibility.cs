using DataOrganizer.DTO.Entities;
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
	/// Drops the session keys of the folders an object covers, whatever their contents show.
	/// </summary>
	void DiscardKeys(ExplorerModelBaseDto item);

	/// <summary>
	/// Hides contents of the whole hierarchy.
	/// </summary>
	void HideAllContents(IEnumerable<ExplorerModelBaseDto> hierarchy);

	/// <summary>
	/// Hides file contents.
	/// </summary>
	void HideFileContents(FileModelDto file);

	/// <summary>
	/// Hides file contents in folder.
	/// </summary>
	void HideFolderContents(FolderModelDto folder);

	/// <summary>
	/// Shows file contents.
	/// </summary>
	Task<bool> ShowFileContentsAsync(FileModelDto file, CancellationToken token = default);

	/// <summary>
	/// Shows file contents in folder.
	/// </summary>
	Task ShowFolderContentsAsync(FolderModelDto folder, CancellationToken token = default);
	#endregion
}
