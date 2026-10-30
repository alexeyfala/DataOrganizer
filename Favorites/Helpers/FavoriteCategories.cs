using Favorites.Dto;
using Hierarchy.Dto;
using Hierarchy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Favorites.Helpers;

/// <summary>
/// Gathers the favorite files of a tree into categories by their folder.
/// </summary>
public static class FavoriteCategories
{
	#region Data
	/// <summary>
	/// Name of the category of the favorite files outside every folder.
	/// </summary>
	private const string RootName = "Root";

	/// <summary>
	/// Identifier of the category of the favorite files outside every folder.
	/// </summary>
	private static readonly Guid RootId = new("210B84EF-06EA-4B70-97E8-DC4BE4DD6195");
	#endregion

	#region Methods
	/// <summary>
	/// Returns a category per folder that holds favorite files, numbered in the order of the walk of the tree.
	/// </summary>
	public static List<FavoriteCategory> Gather(IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		List<FavoriteCategory> categories = [];

		Gather(hierarchy, categories);

		return categories;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Adds the category of the favorite files of one level of the tree, then those of the folders beneath it.
	/// </summary>
	private static void Gather(IEnumerable<ExplorerItemDtoBase> hierarchy, List<FavoriteCategory> categories)
	{
		List<FileDto> files = [.. hierarchy
			.OfType<FileDto>()
			.Where(x => x.IsFavorite)];

		if (files.Count > 0)
		{
			FolderDto? parent = files[0].Parent;

			categories.Add(new()
			{
				Children = files,
				EncryptionStatus = parent?.EncryptionStatus ?? EncryptionStatus.None,
				Id = parent?.Id ?? RootId,
				Index = categories.Count,
				Name = parent?.Name ?? RootName
			});
		}

		foreach (FolderDto folder in hierarchy.OfType<FolderDto>())
		{
			Gather(folder.Children, categories);
		}
	}
	#endregion
}
