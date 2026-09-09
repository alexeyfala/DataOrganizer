using Cysharp.Text;
using DataOrganizer.Dto;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Models.Dataset;
using Entities.Enums;
using Repository.Dto;
using Shared.Extensions;
using Shared.Properties;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DataOrganizer.Extensions;

internal static class EnumerableExtensions
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
	/// Filters a hierarchical sequence of <see cref="ExplorerItemDtoBase" /> by hotkeys that could not be read.
	/// </summary>
	/// <returns>Flat sequence <see cref="FileDto" />.</returns>
	public static IEnumerable<FileDto> GetFilesWithUnreadableHotkeys(
		this IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		return GetFilesBy(hierarchy, x => x.Hotkeys.Any(IsUnreadable));
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
	/// Returns a string representation of the sequence <see cref="HotkeyDto" />.
	/// </summary>
	public static string GetHotkeysPresentation(this KeyStroke[] hotKeys)
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
	/// Builds the text that names the files whose hotkeys could not be read, a file per line,
	/// under the given <paramref name="header"/>.
	/// </summary>
	public static string GetUnreadableHotkeysPresentation(this FileDto[] files, string header)
	{
		const int maxNames = 3;

		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		builder.Append(header);

		builder.Append(':');

		int names = Math.Min(files.Length, maxNames);

		for (int i = 0; i < names; i++)
		{
			builder.Append(Environment.NewLine);

			builder.Append('"');

			builder.Append(files[i].Name);

			builder.Append('"');
		}

		if (files.Length > maxNames)
		{
			builder.Append(Environment.NewLine);

			builder.AppendFormat(Strings.AndMore, files.Length - maxNames);
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
	/// Sorts the sequence <see cref="ExplorerItemDtoBase" /> by <see cref="ExplorerItemDtoBase.Index" /> recursively.
	/// </summary>
	public static ExplorerItemDtoBase[] SortByIndexRecursively(this ExplorerItemDtoBase[] hierarchy)
	{
		hierarchy
			.OfType<FolderDto>()
			.ForEach(SortChildrenByIndexRecursively);

		return [.. hierarchy.OrderBy(x => x.Index)];
	}

	/// <summary>
	/// Redistributes <see cref="FolderDto.Children" /> objects by index <see cref="ExplorerItemDtoBase.Index" /><br />
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
	/// Transforms a sequence of <see cref="HotkeyDto" /> to a sequence of <see cref="KeyStroke" />.
	/// </summary>
	public static IEnumerable<KeyStroke> ToKeyStrokes(this IEnumerable<HotkeyDto> sequence)
	{
		return sequence.Select(x => new KeyStroke
		{
			Code = x.Code,
			Mask = x.Mask
		});
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
	/// Transforms a sequence of <see cref="KeyStroke" /> to a sequence of <see cref="HotkeyDto" />.
	/// </summary>
	public static IEnumerable<HotkeyDto> ToHotkeyDtos(
		this KeyStroke[] sequence,
		Guid id = default,
		Guid ownerId = default)
	{
		for (int i = 0; i < sequence.Length; i++)
		{
			KeyStroke x = sequence[i];

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
			if (item.EntityType == EntityKind.File || item.EntityType == EntityKind.DataSet)
			{
				files++;

				continue;
			}

			if (item.EntityType == EntityKind.Folder)
			{
				folders++;

				if (item is FolderDto folder)
				{
					CountObjects(folder.Children, ref files, ref folders);
				}
			}
		}
	}

	/// <summary>
	/// <c>True</c> when a hotkey holds a key or a mask that the library no longer has.
	/// </summary>
	private static bool IsUnreadable(HotkeyDto hotkey)
	{
		return hotkey.Code == KeyCode.VcUndefined || hotkey.Mask == EventMask.None;
	}
	#endregion
}
