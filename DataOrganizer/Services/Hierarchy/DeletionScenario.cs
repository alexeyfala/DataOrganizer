using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions.Hierarchy;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Hierarchy;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Hierarchy;

/// <inheritdoc cref="IDeletionScenario" />
public sealed class DeletionScenario : IDeletionScenario
{
	#region Data
	/// <inheritdoc cref="IContentKeys" />
	private readonly IContentKeys _contentKeys;

	/// <inheritdoc cref="ICopyHistoryRecorder" />
	private readonly ICopyHistoryRecorder _copyHistoryRecorder;

	/// <inheritdoc cref="IHierarchyEditor" />
	private readonly IHierarchyEditor _hierarchyEditor;

	/// <inheritdoc cref="IOpenFilesCloser" />
	private readonly IOpenFilesCloser _openFilesCloser;
	#endregion

	#region Constructors
	public DeletionScenario(
		IContentKeys contentKeys,
		ICopyHistoryRecorder copyHistoryRecorder,
		IHierarchyEditor hierarchyEditor,
		IOpenFilesCloser openFilesCloser)
	{
		_contentKeys = contentKeys;

		_copyHistoryRecorder = copyHistoryRecorder;

		_hierarchyEditor = hierarchyEditor;

		_openFilesCloser = openFilesCloser;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> DeleteAsync(
		ExplorerItemDtoBase item,
		Collection<ExplorerItemDtoBase> hierarchy,
		CancellationToken token = default)
	{
		if (!await _hierarchyEditor
			.DeleteAsync(item, hierarchy, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		// A folder leaves the database together with every file of its subtree.
		FileDto[] deletedFiles = item switch
		{
			FileDto file => [file],
			FolderDto folder => [.. folder.Children.GetFiles()],
			_ => []
		};

		foreach (FileDto deletedFile in deletedFiles)
		{
			_openFilesCloser.Close([deletedFile]);

			_copyHistoryRecorder.Remove(deletedFile);
		}

		if (item is FolderDto deletedFolder)
		{
			_contentKeys.DiscardKeys(deletedFolder);
		}

		return true;
	}
	#endregion
}
