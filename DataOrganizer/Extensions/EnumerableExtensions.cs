using Cysharp.Text;
using DataOrganizer.Abstract;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Models;
using Entities.Enums;
using Repository.DTO;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DataOrganizer.Extensions;

public static class EnumerableExtensions
{
	#region Methods
	/// <summary>
	/// <c>True</c> when all elements in the hierarchy satisfy a certain condition.
	/// </summary>
	public static bool AllBy(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Predicate<ExplorerModelBaseDto> condition)
	{
		Stack<ExplorerModelBaseDto> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (!condition(item))
			{
				return false;
			}

			if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}

		return true;
	}

	/// <summary>
	/// <c>True</c> when the hierarchy contains <see cref="ExplorerModelBaseDto" /> with the certain condition.
	/// </summary>
	public static bool ContainsBy(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Predicate<ExplorerModelBaseDto> condition)
	{
		Stack<ExplorerModelBaseDto> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (condition(item))
			{
				return true;
			}

			if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}

		return false;
	}

	/// <summary>
	/// <c>True</c> when the hierarchy contains <see cref="FileModelDto" /> with the certain condition.
	/// </summary>
	public static bool ContainsFileBy(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Predicate<FileModelDto> condition)
	{
		Stack<ExplorerModelBaseDto> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (item is FileModelDto file && condition(file))
			{
				return true;
			}

			if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children)
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
	public static bool ContainsId(this IEnumerable<ExplorerModelBaseDto> hierarchy, Guid id)
	{
		Stack<ExplorerModelBaseDto> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (item.Id == id)
			{
				return true;
			}

			if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerModelBaseDto" /> by condition.
	/// </summary>
	/// <returns>Flat sequence <see cref="ExplorerModelBaseDto" />.</returns>
	public static IEnumerable<ExplorerModelBaseDto> FilterBy(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Predicate<ExplorerModelBaseDto> condition)
	{
		Stack<ExplorerModelBaseDto> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (condition(item))
			{
				yield return item;
			}

			if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}
	}

	/// <summary>
	/// Filters a hierarchical sequence by a list of identifiers <paramref name="identifiers"/>.
	/// Returns a flat sequence of <see cref="FileModelDto" />.
	/// </summary>
	public static IEnumerable<FileModelDto> FilterFilesById(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		IEnumerable<Guid> identifiers)
	{
		Dictionary<Guid, FileModelDto> filesById = GetFiles(hierarchy).ToDictionary(x => x.Id);

		foreach (Guid id in identifiers)
		{
			if (filesById.TryGetValue(id, out FileModelDto? file))
			{
				yield return file;
			}
		}
	}

	/// <summary>
	/// Performs a search for the <see cref="ExplorerModelBaseDto" /> object in a sequence with a condition.
	/// </summary>
	public static ExplorerModelBaseDto? FindBy(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Predicate<ExplorerModelBaseDto> condition)
	{
		Stack<ExplorerModelBaseDto> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (condition(item))
			{
				return item;
			}

			if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Performs a search for the <see cref="ExplorerModelBaseDto" /> object in the sequence by identifier.
	/// </summary>
	public static ExplorerModelBaseDto? FindById(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Guid id) => FindBy(hierarchy, x => x.Id == id);

	/// <summary>
	/// Performs a search for the <see cref="FileModelDto" /> object in a sequence with a condition.
	/// </summary>
	public static FileModelDto? FindFileBy(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Func<FileModelDto, bool> condition)
	{
		return GetFiles(hierarchy).FirstOrDefault(condition);
	}

	/// <summary>
	/// Performs a search for the <see cref="FolderModelDto" /> object in a sequence with a condition.
	/// </summary>
	public static FolderModelDto? FindFolderBy(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Func<FolderModelDto, bool> condition)
	{
		return GetFolders(hierarchy).FirstOrDefault(condition);
	}

	/// <summary>
	/// Performs a transformation of a hierarchical sequence into a flat one.
	/// </summary>
	public static IEnumerable<DatasetRecordBase> Flatten(this IEnumerable<DatasetRecordBase> hierarchy)
	{
		Stack<DatasetRecordBase> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			DatasetRecordBase item = stack.Pop();

			yield return item;

			if (item is RecordsGroup group)
			{
				foreach (DatasetRecordBase child in group.Children)
				{
					stack.Push(child);
				}
			}
		}
	}

	/// <summary>
	/// Counts objects in hierarchy.
	/// </summary>
	public static int GetCount(this IEnumerable<DatasetRecordBase> hierarchy)
	{
		return hierarchy
			.Flatten()
			.Count();
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerModelBaseDto" /> by type <see cref="FileModelDto" />.
	/// </summary>
	/// <returns>Flat list <see cref="FileModelDto" />.</returns>
	public static IEnumerable<FileModelDto> GetFiles(this IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		Stack<ExplorerModelBaseDto> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (item is FileModelDto file)
			{
				yield return file;
			}
			else if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerModelBaseDto" /> by condition.
	/// </summary>
	/// <returns>Flat sequence <see cref="FileModelDto" />.</returns>
	public static IEnumerable<FileModelDto> GetFilesBy(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Func<FileModelDto, bool> condition)
	{
		return GetFiles(hierarchy).Where(condition);
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerModelBaseDto" /> by type <see cref="FolderModelDto" />.
	/// </summary>
	/// <returns>Flat sequence <see cref="FolderModelDto" />.</returns>
	public static IEnumerable<FolderModelDto> GetFolders(this IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		Stack<ExplorerModelBaseDto> stack = new(hierarchy);

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (item is FolderModelDto folder)
			{
				yield return folder;

				foreach (ExplorerModelBaseDto child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerModelBaseDto" /> by condition.
	/// </summary>
	/// <returns>Flat sequence <see cref="FolderModelDto" />.</returns>
	public static IEnumerable<FolderModelDto> GetFoldersBy(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Func<FolderModelDto, bool> condition)
	{
		return GetFolders(hierarchy).Where(condition);
	}

	/// <summary>
	/// Returns a string representation of the sequence <see cref="HotkeyModelDto" />.
	/// </summary>
	public static string GetHotkeysPresentation(this CodeMaskPair[] hotKeys)
	{
		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		if (hotKeys.Length != 0 && hotKeys[0].Mask.IsNotDefault())
		{
			builder.Append(hotKeys[0].Mask);

			builder.Append(' ');

			builder.Append('+');

			builder.Append(' ');
		}

		for (int i = 0; i < hotKeys.Length; i++)
		{
			builder.Append(hotKeys[i].ConvertToKey());

			if (i == hotKeys.Length - 1)
			{
				continue;
			}

			builder.Append(',');

			builder.Append(' ');
		}

		return builder.ToString();
	}

	/// <summary>
	/// Sorts <paramref name="records"/> in a required order.
	/// </summary>
	public static DatasetRecordBase[] Sort(
		this ICollection<DatasetRecordBase> records,
		ListSortDirection direction)
	{
		RecordsGroup[] groups = direction switch
		{
			ListSortDirection.Ascending => [.. records.OfType<RecordsGroup>().OrderBy(x => x.Name)],
			ListSortDirection.Descending => [.. records.OfType<RecordsGroup>().OrderByDescending(x => x.Name)],
			_ => throw new NotImplementedException()
		};

		KeyValueRecord[] keyValues = direction switch
		{
			ListSortDirection.Ascending => [.. records.OfSpecificType<DatasetRecordBase, KeyValueRecord>().OrderBy(x => x.Key)],
			ListSortDirection.Descending => [.. records.OfSpecificType<DatasetRecordBase, KeyValueRecord>().OrderByDescending(x => x.Key)],
			_ => throw new NotImplementedException()
		};

		ValueRecord[] values = direction switch
		{
			ListSortDirection.Ascending => [.. records.OfSpecificType<DatasetRecordBase, ValueRecord>().OrderBy(x => x.Value)],
			ListSortDirection.Descending => [.. records.OfSpecificType<DatasetRecordBase, ValueRecord>().OrderByDescending(x => x.Value)],
			_ => throw new NotImplementedException()
		};

		foreach (RecordsGroup group in groups)
		{
			DatasetRecordBase[] sorted = group
				.Children
				.Sort(direction);

			group
				.Children
				.ClearAddRange(sorted);
		}

		return [.. groups, .. keyValues, .. values];
	}

	/// <summary>
	/// Sorts the sequence <see cref="ExplorerModelBaseDto" /> by <see cref="ExplorerModelBaseDto.Index" /> recursively.
	/// </summary>
	public static ExplorerModelBaseDto[] SortByIndexRecursively(this ExplorerModelBaseDto[] hierarchy)
	{
		hierarchy
			.OfType<FolderModelDto>()
			.ForEach(SortChildrenByIndexRecursively);

		return [.. hierarchy.OrderBy(x => x.Index)];
	}

	/// <summary>
	/// Redistributes <see cref="FolderModelDto.Children" /> objects by index <see cref="ExplorerModelBaseDto.Index" /><br />
	/// into <see cref="FolderModelDto" /> recursively.
	/// </summary>
	public static void SortChildrenByIndexRecursively(this FolderModelDto target)
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
			.OfType<FolderModelDto>()
			.ForEach(SortChildrenByIndexRecursively);
	}

	/// <summary>
	/// Transforms a sequence of <see cref="HotkeyModelDto" /> to a sequence of <see cref="CodeMaskPair" />.
	/// </summary>
	public static IEnumerable<CodeMaskPair> ToCodeMaskPairs(this IEnumerable<HotkeyModelDto> sequence)
	{
		return sequence.Select(x => new CodeMaskPair
		{
			Code = x.Code,
			Mask = x.Mask
		});
	}

	/// <summary>
	/// Converts flat sequences <see cref="FolderModelDto" /> and <see cref="FileModelDto" /> into a single hierarchical one.
	/// </summary>
	public static IEnumerable<ExplorerModelBaseDto> ToHierarchical(
		this FolderModelDto[] folders,
		FileModelDto[] files)
	{
		Dictionary<Guid, FolderModelDto> foldersById = folders.ToDictionary(x => x.Id);

		foreach (FileModelDto file in files)
		{
			if (file.ParentId is not { } parentId || !foldersById.TryGetValue(parentId, out FolderModelDto? parent))
			{
				yield return file;

				continue;
			}

			parent
				.Children
				.Add(file);

			file.Parent = parent;
		}

		foreach (FolderModelDto folder in folders)
		{
			if (folder.ParentId is not { } parentId || !foldersById.TryGetValue(parentId, out FolderModelDto? parent))
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
	/// Transforms a sequence of <see cref="CodeMaskPair" /> to a sequence of <see cref="HotkeyModelDto" />.
	/// </summary>
	public static IEnumerable<HotkeyModelDto> ToHotkeyModelsDto(
		this CodeMaskPair[] sequence,
		Guid id = default,
		Guid ownerId = default)
	{
		for (int i = 0; i < sequence.Length; i++)
		{
			CodeMaskPair x = sequence[i];

			yield return new()
			{
				Code = x.Code,
				Id = id,
				Index = i,
				Mask = x.Mask,
				OwnerId = ownerId
			};
		}
	}

	/// <summary>
	/// Counts files and folders in hierarchy.
	/// </summary>
	internal static FilesFoldersNumberPair GetCount(this IEnumerable<ExplorerModelBaseDto> hierarchy)
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
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		ref uint files,
		ref uint folders)
	{
		foreach (ExplorerModelBaseDto item in hierarchy)
		{
			if (item.EntityType == EntityType.File || item.EntityType == EntityType.DataSet)
			{
				files++;

				continue;
			}

			if (item.EntityType == EntityType.Folder)
			{
				folders++;

				if (item is FolderModelDto folder)
				{
					CountObjects(folder.Children, ref files, ref folders);
				}
			}
		}
	}
	#endregion
}
