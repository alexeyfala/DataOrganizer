using DataOrganizer.Helpers.Dataset;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Models.Dataset;
using Entities.Enums;
using Entities.Models;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace DataOrganizer.Helpers.Hierarchy;

/// <summary>
/// Objects of the virtual file system made up for trying the application on large data: deep nesting, thousands of
/// objects, thousands of files in one folder, and large texts and datasets.
/// </summary>
internal sealed class LargeSampleHierarchy
{
	#region Data
	/// <summary>
	/// Number of folders nested in one another in the deep branch.
	/// </summary>
	private const int DeepLevelCount = 50;

	/// <summary>
	/// Number of records of each type in the group of the dataset with one group.
	/// </summary>
	private const int GroupRecordCount = 334;

	/// <summary>
	/// Number of records in the JSON written on one line.
	/// </summary>
	private const int MinifiedRecordCount = 4000;

	/// <summary>
	/// Number of records of each type in the large dataset.
	/// </summary>
	private const int RecordCount = 3334;

	/// <summary>
	/// Start of the name of the folder of a run.
	/// </summary>
	private const string RunFolderPrefix = "Large samples";

	/// <summary>
	/// Least length of the long text, in bytes.
	/// </summary>
	private const int TextLength = 5 * 1024 * 1024;

	/// <summary>
	/// Number of folder levels under the folder of the tree branch.
	/// </summary>
	private const int TreeDepth = 3;

	/// <summary>
	/// Number of files in each folder on the last level of the tree branch.
	/// </summary>
	private const int TreeFileCount = 2;

	/// <summary>
	/// Number of subfolders in each folder of the tree branch above its last level.
	/// </summary>
	private const int TreeFolderCount = 10;

	/// <summary>
	/// Number of files in the wide folder.
	/// </summary>
	private const int WideFileCount = 2000;

	/// <summary>
	/// Number of objects put into each folder so far, by the identifier of the folder.
	/// </summary>
	private readonly Dictionary<Guid, int> _childCounts = [];

	/// <summary>
	/// Contents of every file of the deep, tree and wide branches.
	/// </summary>
	private readonly byte[] _fileContents;

	/// <summary>
	/// Objects made so far, each after its folder.
	/// </summary>
	private readonly List<ExplorerItemBase> _items = [];

	/// <summary>
	/// Time of the run, when every object is created and updated.
	/// </summary>
	private readonly DateTime _now;

	/// <summary>
	/// Source of the records of the datasets.
	/// </summary>
	private readonly SampleRecords _records = new();
	#endregion

	#region Constructors
	private LargeSampleHierarchy(DateTime now)
	{
		_now = now;

		_fileContents = TextDefaults
			.Encoding
			.GetBytes(SampleFaker.Create().Lorem.Sentence());
	}
	#endregion

	#region Methods
	/// <summary>
	/// Creates the objects of a large run under a folder of its own at a position of the root; each object comes after its
	/// folder.
	/// </summary>
	public static ExplorerItemBase[] Create(int rootIndex, DateTime now)
	{
		return new LargeSampleHierarchy(now).Build(rootIndex);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the C# sample repeated until the text reaches its length.
	/// </summary>
	private static string CreateLongText()
	{
		int copies = (TextLength / TextDefaults.Encoding.GetByteCount(SampleText.CSharp)) + 1;

		return string.Join(
			Environment.NewLine + Environment.NewLine,
			Enumerable.Repeat(SampleText.CSharp, copies));
	}

	/// <summary>
	/// Returns the text of a dataset that holds records.
	/// </summary>
	private static string SerializeRecords(IEnumerable<DatasetRecordBase> records) => JsonSerializer.Serialize(records, JsonDefaults.Options);

	/// <summary>
	/// Nests folders in one another under the folder of the deep branch and puts a file into the last of them.
	/// </summary>
	private void AddDeepContents(FolderEntity folder)
	{
		FolderEntity last = folder;

		for (int level = 1; level <= DeepLevelCount; level++)
		{
			last = AddFolder(last, $"Level {level}");
		}

		AddFile(
			last,
			"bottom.txt",
			EntityKind.File,
			_fileContents);
	}

	/// <summary>
	/// Adds a file or a dataset at the next position of a folder.
	/// </summary>
	private void AddFile(
		FolderEntity parent,
		string name,
		EntityKind kind,
		byte[] contents)
	{
		_items.Add(new FileEntity
		{
			Contents = contents,
			CreatedAt = _now,
			Id = Guid.NewGuid(),
			Index = TakeIndex(parent),
			Kind = kind,
			Name = name,
			ParentId = parent.Id,
			UpdatedAt = _now
		});
	}

	/// <summary>
	/// Adds a folder at the next position of another one, or to the root when there is no parent.
	/// </summary>
	private FolderEntity AddFolder(
		FolderEntity? parent,
		string name,
		string? note = null)
	{
		FolderEntity folder = new()
		{
			CreatedAt = _now,
			Id = Guid.NewGuid(),
			Index = parent is null ? 0 : TakeIndex(parent),
			Kind = EntityKind.Folder,
			Name = name,
			Note = note is null ? null : TextDefaults.Encoding.GetBytes(note),
			ParentId = parent?.Id,
			UpdatedAt = _now
		};

		_items.Add(folder);

		return folder;
	}

	/// <summary>
	/// Fills the folder of large contents: a long text, a text on one line and datasets with many records.
	/// </summary>
	private void AddLargeContents(FolderEntity folder)
	{
		RecordsGroup group = _records
			.CreateGroups(1)
			.Single();

		// The records of a group are drawn all at once, so the group starts collapsed.
		group.IsExpanded = false;

		group
			.Children
			.AddRange(_records.CreateRandomRecords(GroupRecordCount));

		// One row is one object, in the order of the folder.
		(string Name, EntityKind Kind, string Text)[] files =
		[
			("Group", EntityKind.Dataset, SerializeRecords([group])),
			("large.cs", EntityKind.File, CreateLongText()),
			("minified.json", EntityKind.File, JsonSerializer.Serialize(_records.CreateValueRecords(MinifiedRecordCount))),
			("Records", EntityKind.Dataset, SerializeRecords(_records.CreateRandomRecords(RecordCount)))
		];

		foreach ((string name, EntityKind kind, string text) in files)
		{
			AddFile(
				folder,
				name,
				kind,
				TextDefaults.Encoding.GetBytes(text));
		}
	}

	/// <summary>
	/// Fills a folder of the tree branch with subfolders above the last level and with files on it.
	/// </summary>
	private void AddTreeContents(FolderEntity folder, string path, int level)
	{
		if (level == TreeDepth)
		{
			for (int number = 1; number <= TreeFileCount; number++)
			{
				AddFile(
					folder,
					$"File {number}.txt",
					EntityKind.File,
					_fileContents);
			}

			return;
		}

		for (int number = 1; number <= TreeFolderCount; number++)
		{
			// The name holds the path of the folder in the branch, such as 1.2.1.
			string childPath = path.Length == 0 ? $"{number}" : $"{path}.{number}";

			AddTreeContents(
				AddFolder(folder, $"Folder {childPath}"),
				childPath,
				level + 1);
		}
	}

	/// <summary>
	/// Fills the wide folder with files.
	/// </summary>
	private void AddWideContents(FolderEntity folder)
	{
		for (int number = 1; number <= WideFileCount; number++)
		{
			AddFile(
				folder,
				$"File {number}.txt",
				EntityKind.File,
				_fileContents);
		}
	}

	/// <summary>
	/// Makes the objects of the run, the folder of the run first.
	/// </summary>
	private ExplorerItemBase[] Build(int rootIndex)
	{
		FolderEntity root = AddFolder(
			parent: null,
			name: $"{RunFolderPrefix} {_now.ToString(SampleHierarchy.RunFolderDateFormat, CultureInfo.InvariantCulture)}",
			note: SampleNotes.LargeRunFolder);

		root.Index = rootIndex;

		// Each object takes the next position of its folder, so the objects are made in the order a folder sorts them.
		AddDeepContents(AddFolder(root, "Deep"));

		AddLargeContents(AddFolder(root, "Large"));

		AddTreeContents(
			AddFolder(root, "Tree"),
			path: string.Empty,
			level: 0);

		AddWideContents(AddFolder(root, "Wide"));

		return [.. _items];
	}

	/// <summary>
	/// Returns the next position of a folder.
	/// </summary>
	private int TakeIndex(FolderEntity folder)
	{
		ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(_childCounts, folder.Id, out _);

		return count++;
	}
	#endregion
}
