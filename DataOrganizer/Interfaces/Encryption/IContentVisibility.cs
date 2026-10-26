using Hierarchy.Dto;
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
	/// Hides contents of the whole hierarchy.
	/// </summary>
	void HideAllContents(IEnumerable<ExplorerItemDtoBase> hierarchy);

	/// <summary>
	/// Hides file contents.
	/// </summary>
	void HideFileContents(FileDto file);

	/// <summary>
	/// Hides the file contents in a folder.
	/// </summary>
	void HideFolderContents(FolderDto folder);

	/// <summary>
	/// Shows file contents.
	/// </summary>
	Task<bool> ShowFileContentsAsync(FileDto file, CancellationToken token = default);

	/// <summary>
	/// Shows the file contents in a folder.
	/// </summary>
	Task ShowFolderContentsAsync(FolderDto folder, CancellationToken token = default);
	#endregion
}
