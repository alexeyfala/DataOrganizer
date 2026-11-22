using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.Enums;
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
	/// <inheritdoc cref="IAutoLockControl" />
	private readonly IAutoLockControl _autoLockControl;

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
	/// <summary>
	/// Initializes a new instance of the <see cref="DeletionScenario" /> class.
	/// </summary>
	public DeletionScenario(
		IAutoLockControl autoLockControl,
		IContentKeys contentKeys,
		ICopyHistoryRecorder copyHistoryRecorder,
		IHierarchyEditor hierarchyEditor,
		IOpenFilesCloser openFilesCloser)
	{
		_autoLockControl = autoLockControl;

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
		// The open files and the countdown belong to the UI, so the deletion goes on in the context of the caller.
		if (!await _hierarchyEditor
			.DeleteAsync(item, hierarchy, token)
			.ConfigureAwait(true))
		{
			return false;
		}

		ForgetDeleted(item);

		RefreshAutoLockAfter([item]);

		return true;
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<ExplorerItemDtoBase>> DeleteManyAsync(
		IReadOnlyList<ExplorerItemDtoBase> items,
		Collection<ExplorerItemDtoBase> hierarchy,
		CancellationToken token = default)
	{
		// The open files and the countdown belong to the UI, so the deletion goes on in the context of the caller.
		IReadOnlyList<ExplorerItemDtoBase> deleted = await _hierarchyEditor
			.DeleteManyAsync(items, hierarchy, token)
			.ConfigureAwait(true);

		foreach (ExplorerItemDtoBase item in deleted)
		{
			ForgetDeleted(item);
		}

		RefreshAutoLockAfter(deleted);

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

	/// <summary>
	/// Brings the auto-lock countdown in line with the shown contents once deleted objects took some of them away.
	/// </summary>
	private void RefreshAutoLockAfter(IEnumerable<ExplorerItemDtoBase> deleted)
	{
		// A refresh starts the countdown over while other contents stay shown, so hidden objects never trigger it.
		if (!deleted.ContainsBy(static x => x.EncryptionStatus == EncryptionStatus.Decrypted))
		{
			return;
		}

		_autoLockControl.RefreshAutoLock();
	}
	#endregion
}
