using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.Extensions;
using DataOrganizer.Hierarchy.Storage.Interfaces;
using DataOrganizer.Security.Interfaces;
using DataOrganizer.SystemIntegration.Clipboard.Interfaces;
using DataOrganizer.SystemIntegration.FileExecution.Interfaces;
using DataOrganizer.Workflows.HierarchyScenarios.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Workflows.HierarchyScenarios.Services;

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

		ForgetDeleted(item);

		return true;
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<ExplorerItemDtoBase>> DeleteManyAsync(
		IReadOnlyList<ExplorerItemDtoBase> items,
		Collection<ExplorerItemDtoBase> hierarchy,
		CancellationToken token = default)
	{
		IReadOnlyList<ExplorerItemDtoBase> deleted = await _hierarchyEditor
			.DeleteManyAsync(items, hierarchy, token)
			.ConfigureAwait(false);

		foreach (ExplorerItemDtoBase item in deleted)
		{
			ForgetDeleted(item);
		}

		return deleted;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Closes the files of a deleted object, takes them out of the copy history, forgets the keys of a deleted folder and
	/// the key of a keeper that has nothing shown left.
	/// </summary>
	private void ForgetDeleted(ExplorerItemDtoBase item)
	{
		// A folder leaves the database together with every file of its subtree.
		FileDto[] deletedFiles = item switch
		{
			FileDto file => [file],
			FolderDto folder => [.. folder.Children.GetFiles()]
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

		// The object still knows its parents, so the keeper above it is found.
		_contentKeys.DiscardUnusedKey(item);
	}
	#endregion
}
