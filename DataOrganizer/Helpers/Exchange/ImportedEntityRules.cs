using DataOrganizer.Dto.Entities;
using Entities.Models;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Exchange;

/// <summary>
/// Rules for the folders and files read from a file of an exchange format before they join the database.
/// </summary>
internal static class ImportedEntityRules
{
	#region Methods
	/// <summary>
	/// Puts the top-level objects after those of <paramref name="hierarchy" />, in their order.
	/// </summary>
	public static void PlaceAfter(
		IReadOnlyCollection<ExplorerItemDtoBase> hierarchy,
		FolderEntity[] folders,
		FileEntity[] files)
	{
		if (hierarchy.Count == 0)
		{
			return;
		}

		int startIndex = hierarchy.Max(x => x.Index) + 1;

		folders
			.OfType<ExplorerItemBase>()
			.Concat(files)
			.Where(x => x.ParentId is null)
			.OrderBy(x => x.Index)
			.ForEach(x =>
			{
				x.Index = startIndex;

				startIndex++;
			});
	}

	/// <summary>
	/// Gives the folders, the files and the hotkeys new identifiers, keeping the links between them.
	/// </summary>
	public static void RegenerateIds(FolderEntity[] folders, FileEntity[] files)
	{
		files.ForEach(file =>
		{
			Guid newFileId = Guid.NewGuid();

			file.Id = newFileId;

			file.Hotkeys.ForEach(hotkey =>
			{
				hotkey.Id = Guid.NewGuid();

				hotkey.OwnerId = newFileId;
			});
		});

		ILookup<Guid?, FolderEntity> foldersByParent = folders.ToLookup(x => x.ParentId);

		ILookup<Guid?, FileEntity> filesByParent = files.ToLookup(x => x.ParentId);

		folders.ForEach(folder =>
		{
			Guid newFolderId = Guid.NewGuid();

			Guid oldFolderId = folder.Id;

			folder.Id = newFolderId;

			foldersByParent[oldFolderId].ForEach(x => x.ParentId = newFolderId);

			filesByParent[oldFolderId].ForEach(x => x.ParentId = newFolderId);
		});
	}

	/// <summary>
	/// Gives the folders and the files <paramref name="now" /> as the dates of creation and change.
	/// </summary>
	public static void SetDates(
		FolderEntity[] folders,
		FileEntity[] files,
		DateTime now)
	{
		folders
			.Concat<ExplorerItemBase>(files)
			.ForEach(x => x.CreatedAt = x.UpdatedAt = now);
	}
	#endregion
}
