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
	/// Returns <c>True</c> if the hierarchy contains <see cref="FileModelDto" /> with the certain condition.
	/// </summary>
	public static bool ConatainsBy(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Predicate<FileModelDto> condition)
	{
		Stack<ExplorerModelBaseDto> stack = new(hierarchy);

		foreach (ExplorerModelBaseDto item in hierarchy.Reverse())
		{
			stack.Push(item);
		}

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (item is FileModelDto file && condition(file))
			{
				return true;
			}

			if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children.Reverse())
				{
					stack.Push(child);
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Returns <c>True</c> if the hierarchy contains an object with the given identifier.
	/// </summary>
	public static bool ConatainsId(this IEnumerable<ExplorerModelBaseDto> hierarchy, in Guid id)
	{
		Stack<ExplorerModelBaseDto> stack = new(hierarchy);

		foreach (ExplorerModelBaseDto item in hierarchy.Reverse())
		{
			stack.Push(item);
		}

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (item.Id == id)
			{
				return true;
			}

			if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children.Reverse())
				{
					stack.Push(child);
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Filters a hierarchical sequence by a list of identifiers <paramref name="identifiers"/>.
	/// Returns a flat sequence of <see cref="FileModelDto" />.
	/// </summary>
	public static IEnumerable<FileModelDto> FilterFilesById(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		List<Guid> identifiers)
	{
		foreach (Guid id in identifiers)
		{
			if (FindById(hierarchy, id) is FileModelDto file)
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

		foreach (ExplorerModelBaseDto item in hierarchy.Reverse())
		{
			stack.Push(item);
		}

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (condition(item))
			{
				return item;
			}

			if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children.Reverse())
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
	/// Performs a recursive search for the <see cref="FileModelDto" /> object in a sequence with a condition.
	/// </summary>
	public static FileModelDto? FindFileRecursively(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Func<FileModelDto, bool> condition)
	{
		return GetFilesRecursively(hierarchy).FirstOrDefault(condition);
	}

	/// <summary>
	/// Performs a recursive search for the <see cref="FolderModelDto" /> object in a sequence with a condition.
	/// </summary>
	public static FolderModelDto? FindFolderRecursively(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Func<FolderModelDto, bool> condition)
	{
		return GetFoldersRecursively(hierarchy).FirstOrDefault(condition);
	}

	/// <summary>
	/// Performs a transformation of a hierarchical sequence into a flat one.
	/// </summary>
	public static IEnumerable<DatasetRecordBase> Flatten(this IEnumerable<DatasetRecordBase> hierarchy)
	{
		Stack<DatasetRecordBase> stack = new();

		foreach (DatasetRecordBase item in hierarchy.Reverse())
		{
			stack.Push(item);
		}

		while (stack.Count > 0)
		{
			DatasetRecordBase current = stack.Pop();

			yield return current;

			if (current is RecordsGroup group)
			{
				foreach (DatasetRecordBase child in group.Children.Reverse())
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
			.ToArray()
			.Length;
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerModelBaseDto" /> by condition.
	/// </summary>
	/// <returns>Flat list <see cref="FileModelDto" />.</returns>
	public static IEnumerable<FileModelDto> GetFilesRecursively(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Func<FileModelDto, bool> condition)
	{
		return GetFilesRecursively(hierarchy).Where(condition);
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerModelBaseDto" /> by type <see cref="FileModelDto" />.
	/// </summary>
	/// <returns>Flat list <see cref="FileModelDto" />.</returns>
	public static IEnumerable<FileModelDto> GetFilesRecursively(this IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		foreach (ExplorerModelBaseDto dto in hierarchy)
		{
			if (dto is FileModelDto file)
			{
				yield return file;
			}
			else if (dto is FolderModelDto folder)
			{
				foreach (FileModelDto item in GetFilesRecursively(folder.Children))
				{
					yield return item;
				}
			}
		}
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerModelBaseDto" /> by type <see cref="FolderModelDto" />.
	/// </summary>
	/// <returns>Flat list <see cref="FolderModelDto" />.</returns>
	public static IEnumerable<FolderModelDto> GetFoldersRecursively(this IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		foreach (FolderModelDto folder in hierarchy.OfType<FolderModelDto>())
		{
			yield return folder;

			foreach (FolderModelDto subFolder in GetFoldersRecursively(folder.Children))
			{
				yield return subFolder;
			}
		}
	}

	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerModelBaseDto" /> by condition.
	/// </summary>
	/// <returns>Flat list <see cref="FolderModelDto" />.</returns>
	public static IEnumerable<FolderModelDto> GetFoldersRecursively(
		this IEnumerable<ExplorerModelBaseDto> hierarchy,
		Func<FolderModelDto, bool> condition)
	{
		return GetFoldersRecursively(hierarchy).Where(condition);
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
		in ListSortDirection direction)
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
		foreach (FileModelDto file in files)
		{
			if (file.ParentId is null || folders.All(x => x.Id != file.ParentId))
			{
				yield return file;
			}

			if (folders.FirstOrDefault(x => x.Id == file.ParentId) is not { } parent)
			{
				continue;
			}

			parent
				.Children
				.Add(file);

			file.Parent = parent;
		}

		foreach (FolderModelDto folder in folders)
		{
			if (folder.ParentId is null || folders.All(x => x.Id != folder.ParentId))
			{
				yield return folder;
			}
			else if (folders.FirstOrDefault(x => x.Id == folder.ParentId) is { } parent)
			{
				parent
					.Children
					.Add(folder);

				folder.Parent = parent;
			}
		}
	}

	/// <summary>
	/// Transforms a sequence of <see cref="CodeMaskPair" /> to a sequence of <see cref="HotkeyModelDto" />.
	/// </summary>
	public static IEnumerable<HotkeyModelDto> ToHotkeyModelsDto(
		this IEnumerable<CodeMaskPair> sequence,
		Guid id = default,
		Guid ownerId = default)
	{
		return sequence.Select(x => new HotkeyModelDto
		{
			Code = x.Code,
			Id = id,
			Mask = x.Mask,
			OwnerId = ownerId
		});
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

	#region Service
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
			if (item.EntityType == Entities.Enums.EntityType.File)
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
