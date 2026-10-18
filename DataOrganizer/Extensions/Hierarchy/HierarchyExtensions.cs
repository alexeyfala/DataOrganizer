using DataOrganizer.Dto;
using DataOrganizer.Dto.Entities;
using DynamicData;
using Entities.Enums;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace DataOrganizer.Extensions.Hierarchy;

internal static class HierarchyExtensions
{
	#region Methods
	/// <summary>
	/// <c>True</c> when all elements in the hierarchy satisfy a certain condition.
	/// </summary>
	public static bool AllBy(
		this IEnumerable<ExplorerItemDtoBase> hierarchy,
		Predicate<ExplorerItemDtoBase> condition)
	{
		Stack<ExplorerItemDtoBase> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerItemDtoBase item = stack.Pop();

			if (!condition(item))
			{
				return false;
			}

			if (item is FolderDto folder)
			{
				foreach (ExplorerItemDtoBase child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}

		return true;
	}

	/// <summary>
	/// <c>True</c> when the hierarchy contains <see cref="ExplorerItemDtoBase" /> with the certain condition.
	/// </summary>
	public static bool ContainsBy(
		this IEnumerable<ExplorerItemDtoBase> hierarchy,
		Predicate<ExplorerItemDtoBase> condition)
	{
		Stack<ExplorerItemDtoBase> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerItemDtoBase item = stack.Pop();

			if (condition(item))
			{
				return true;
			}

			if (item is FolderDto folder)
			{
				foreach (ExplorerItemDtoBase child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}

		return false;
	}

	/// <summary>
	/// <c>True</c> when the hierarchy contains <see cref="FileDto" /> with the certain condition.
	/// </summary>
	public static bool ContainsFileBy(
		this IEnumerable<ExplorerItemDtoBase> hierarchy,
		Predicate<FileDto> condition)
	{
		Stack<ExplorerItemDtoBase> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerItemDtoBase item = stack.Pop();

			if (item is FileDto file && condition(file))
			{
				return true;
			}

			if (item is FolderDto folder)
			{
				foreach (ExplorerItemDtoBase child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}

		return false;
	}

	/// <summary>
	/// <c>True</c> when the hierarchy contains an object with the given identifier.
	/// </summary>
	public static bool ContainsId(this IEnumerable<ExplorerItemDtoBase> hierarchy, Guid id)
	{
		Stack<ExplorerItemDtoBase> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerItemDtoBase item = stack.Pop();

			if (item.Id == id)
			{
				return true;
			}

			if (item is FolderDto folder)
			{
				foreach (ExplorerItemDtoBase child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerItemDtoBase" /> by condition.
	/// </summary>
	/// <returns>Flat sequence <see cref="ExplorerItemDtoBase" />.</returns>
	public static IEnumerable<ExplorerItemDtoBase> FilterBy(
		this IEnumerable<ExplorerItemDtoBase> hierarchy,
		Predicate<ExplorerItemDtoBase> condition)
	{
		Stack<ExplorerItemDtoBase> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerItemDtoBase item = stack.Pop();

			if (condition(item))
			{
				yield return item;
			}

			if (item is FolderDto folder)
			{
				foreach (ExplorerItemDtoBase child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}
	}

	/// <summary>
	/// Filters a hierarchical sequence by a list of identifiers <paramref name="identifiers"/>.
	/// Returns a flat sequence of <see cref="FileDto" />.
	/// </summary>
	public static IEnumerable<FileDto> FilterFilesById(
		this IEnumerable<ExplorerItemDtoBase> hierarchy,
		IEnumerable<Guid> identifiers)
	{
		Dictionary<Guid, FileDto> filesById = GetFiles(hierarchy).ToDictionary(x => x.Id);

		foreach (Guid id in identifiers)
		{
			if (filesById.TryGetValue(id, out FileDto? file))
			{
				yield return file;
			}
		}
	}

	/// <summary>
	/// Performs a search for the <see cref="ExplorerItemDtoBase" /> object in a sequence with a condition.
	/// </summary>
	public static ExplorerItemDtoBase? FindBy(
		this IEnumerable<ExplorerItemDtoBase> hierarchy,
		Predicate<ExplorerItemDtoBase> condition)
	{
		Stack<ExplorerItemDtoBase> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerItemDtoBase item = stack.Pop();

			if (condition(item))
			{
				return item;
			}

			if (item is FolderDto folder)
			{
				foreach (ExplorerItemDtoBase child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Performs a search for the <see cref="ExplorerItemDtoBase" /> object in the sequence by identifier.
	/// </summary>
	public static ExplorerItemDtoBase? FindById(
		this IEnumerable<ExplorerItemDtoBase> hierarchy,
		Guid id) => FindBy(hierarchy, x => x.Id == id);

	/// <summary>
	/// Performs a search for the <see cref="FileDto" /> object in a sequence with a condition.
	/// </summary>
	public static FileDto? FindFileBy(
		this IEnumerable<ExplorerItemDtoBase> hierarchy,
		Func<FileDto, bool> condition)
	{
		return GetFiles(hierarchy).FirstOrDefault(condition);
	}

	/// <summary>
	/// Performs a search for the <see cref="FolderDto" /> object in a sequence with a condition.
	/// </summary>
	public static FolderDto? FindFolderBy(
		this IEnumerable<ExplorerItemDtoBase> hierarchy,
		Func<FolderDto, bool> condition)
	{
		return GetFolders(hierarchy).FirstOrDefault(condition);
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerItemDtoBase" /> by type <see cref="FileDto" />.
	/// </summary>
	/// <returns>Flat list <see cref="FileDto" />.</returns>
	public static IEnumerable<FileDto> GetFiles(this IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		Stack<ExplorerItemDtoBase> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerItemDtoBase item = stack.Pop();

			if (item is FileDto file)
			{
				yield return file;
			}
			else if (item is FolderDto folder)
			{
				foreach (ExplorerItemDtoBase child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerItemDtoBase" /> by condition.
	/// </summary>
	/// <returns>Flat sequence <see cref="FileDto" />.</returns>
	public static IEnumerable<FileDto> GetFilesBy(
		this IEnumerable<ExplorerItemDtoBase> hierarchy,
		Func<FileDto, bool> condition)
	{
		return GetFiles(hierarchy).Where(condition);
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerItemDtoBase" /> by type <see cref="FolderDto" />.
	/// </summary>
	/// <returns>Flat sequence <see cref="FolderDto" />.</returns>
	public static IEnumerable<FolderDto> GetFolders(this IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		Stack<ExplorerItemDtoBase> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerItemDtoBase item = stack.Pop();

			if (item is FolderDto folder)
			{
				yield return folder;

				foreach (ExplorerItemDtoBase child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerItemDtoBase" /> by condition.
	/// </summary>
	/// <returns>Flat sequence <see cref="FolderDto" />.</returns>
	public static IEnumerable<FolderDto> GetFoldersBy(
		this IEnumerable<ExplorerItemDtoBase> hierarchy,
		Func<FolderDto, bool> condition)
	{
		return GetFolders(hierarchy).Where(condition);
	}

	/// <summary>
	/// Sorts the sequence <see cref="ExplorerItemDtoBase" /> by <see cref="EntityDtoBase.Index" /> recursively.
	/// </summary>
	public static ExplorerItemDtoBase[] SortByIndexRecursively(this ExplorerItemDtoBase[] hierarchy)
	{
		hierarchy
			.OfType<FolderDto>()
			.ForEach(SortChildrenByIndexRecursively);

		return [.. hierarchy.OrderBy(x => x.Index)];
	}

	/// <summary>
	/// Redistributes <see cref="FolderDto.Children" /> objects by index <see cref="EntityDtoBase.Index" /><br />
	/// into <see cref="FolderDto" /> recursively.
	/// </summary>
	public static void SortChildrenByIndexRecursively(this FolderDto target)
	{
		if (!target.Children.Any())
		{
			return;
		}

		target
			.Children
			.SortBy(x => x.Index);

		target
			.Children
			.OfType<FolderDto>()
			.ForEach(SortChildrenByIndexRecursively);
	}

	/// <summary>
	/// Converts flat sequences <see cref="FolderDto" /> and <see cref="FileDto" /> into a single hierarchical one.
	/// </summary>
	public static IEnumerable<ExplorerItemDtoBase> ToHierarchical(
		this FolderDto[] folders,
		FileDto[] files)
	{
		Dictionary<Guid, FolderDto> foldersById = folders.ToDictionary(x => x.Id);

		foreach (FileDto file in files)
		{
			if (file.ParentId is not { } parentId || !foldersById.TryGetValue(parentId, out FolderDto? parent))
			{
				yield return file;

				continue;
			}

			parent
				.Children
				.Add(file);

			file.Parent = parent;
		}

		foreach (FolderDto folder in folders)
		{
			if (folder.ParentId is not { } parentId || !foldersById.TryGetValue(parentId, out FolderDto? parent))
			{
				yield return folder;

				continue;
			}

			parent
				.Children
				.Add(folder);

			folder.Parent = parent;
		}
	}

	/// <summary>
	/// Counts files and folders in the hierarchy.
	/// </summary>
	internal static HierarchyCounts GetCount(this IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		uint files = default;

		uint folders = default;

		CountObjects(hierarchy, ref files, ref folders);

		return new()
		{
			Files = files,
			Folders = folders
		};
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Counts files and folders.
	/// </summary>
	private static void CountObjects(
		IEnumerable<ExplorerItemDtoBase> hierarchy,
		ref uint files,
		ref uint folders)
	{
		foreach (ExplorerItemDtoBase item in hierarchy)
		{
			if (item.Kind == EntityKind.File || item.Kind == EntityKind.Dataset)
			{
				files++;

				continue;
			}

			if (item.Kind == EntityKind.Folder)
			{
				folders++;

				if (item is FolderDto folder)
				{
					CountObjects(folder.Children, ref files, ref folders);
				}
			}
		}
	}
	#endregion
}
