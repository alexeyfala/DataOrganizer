using Bogus;
using DataOrganizer.Helpers.Dataset;
using DataOrganizer.Helpers.Execution;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Models.Dataset;
using Entities.Enums;
using Entities.Models;
using Shared.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace DataOrganizer.Helpers.Hierarchy;

/// <summary>
/// Objects of the virtual file system made up for trying the application: files of known types sorted into folders and a
/// branch of nested folders with files of unknown types.
/// </summary>
internal sealed class SampleHierarchy
{
	#region Data
	/// <summary>
	/// Number of records in a dataset of one type of records.
	/// </summary>
	private const int DatasetRecordCount = 20;

	/// <summary>
	/// Characters of a made-up extension.
	/// </summary>
	private const string ExtensionAlphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

	/// <summary>
	/// Length of a made-up extension.
	/// </summary>
	private const int ExtensionLength = 3;

	/// <summary>
	/// Number of records of each type on each level of the dataset of groups.
	/// </summary>
	private const int GroupedRecordCount = 2;

	/// <summary>
	/// Number of records of each type in the large dataset.
	/// </summary>
	private const int LargeRecordCount = 350;

	/// <summary>
	/// Number of datasets in each folder of the random branch.
	/// </summary>
	private const int RandomDatasetCount = 1;

	/// <summary>
	/// Number of folder levels under the folder of the random branch.
	/// </summary>
	private const int RandomDepth = 3;

	/// <summary>
	/// Number of files in each folder of the random branch.
	/// </summary>
	private const int RandomFileCount = 2;

	/// <summary>
	/// Number of subfolders in each folder of the random branch above its last level.
	/// </summary>
	private const int RandomFolderCount = 2;

	/// <summary>
	/// Name of the folder of the random branch.
	/// </summary>
	private const string RandomFolderName = "Random";

	/// <summary>
	/// Seed of the made-up extensions, which keeps them the same from run to run.
	/// </summary>
	private const int RandomSeed = 20260930;

	/// <summary>
	/// Number of nested levels of groups in the records of a dataset.
	/// </summary>
	private const int RecordLevels = 3;

	/// <summary>
	/// Number of sets of records in a dataset of the random branch.
	/// </summary>
	private const int RecordRepeats = 20;

	/// <summary>
	/// Format of the time of a run in the name of its folder.
	/// </summary>
	private const string RunFolderDateFormat = "dd.MM.yyyy HH:mm:ss";

	/// <summary>
	/// Start of the name of the folder of a run.
	/// </summary>
	private const string RunFolderPrefix = "Samples";

	/// <summary>
	/// Number of children given out so far, by the identifier of their folder.
	/// </summary>
	private readonly Dictionary<Guid, int> _childCounts = [];

	/// <summary>
	/// Contents of every dataset of the random branch.
	/// </summary>
	private readonly byte[] _datasetContents;

	/// <summary>
	/// Source of the made-up values of the texts.
	/// </summary>
	private readonly Faker _faker = SampleFaker.Create();

	/// <summary>
	/// Objects made so far, each after its folder.
	/// </summary>
	private readonly List<ExplorerItemBase> _items = [];

	/// <summary>
	/// Time of the run, when every object is created and updated.
	/// </summary>
	private readonly DateTime _now;

	/// <summary>
	/// Contents of every file of the random branch.
	/// </summary>
	private readonly byte[] _plainContents;

	/// <summary>
	/// Source of the made-up extensions.
	/// </summary>
	private readonly Random _random = new(RandomSeed);

	/// <summary>
	/// Source of the records of the datasets.
	/// </summary>
	private readonly SampleRecords _records = new();
	#endregion

	#region Constructors
	private SampleHierarchy(DateTime now)
	{
		_now = now;

		_plainContents = TextDefaults
			.Encoding
			.GetBytes(SampleDocuments.CreateText(_faker));

		// Each set makes records of its own, with values of their own.
		IEnumerable<DatasetRecordBase> records = Enumerable
			.Range(0, RecordRepeats)
			.SelectMany(_ => _records.CreateRandomRecords(levels: RecordLevels));

		_datasetContents = TextDefaults
			.Encoding
			.GetBytes(SerializeRecords(records));
	}
	#endregion

	#region Methods
	/// <summary>
	/// Creates the objects of a run under a folder of its own at a position of the root; each object comes after its folder.
	/// </summary>
	public static ExplorerItemBase[] Create(int rootIndex, DateTime now) => new SampleHierarchy(now).Build(rootIndex);
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the text of a dataset that holds records.
	/// </summary>
	private static string SerializeRecords(IEnumerable<DatasetRecordBase> records) => JsonSerializer.Serialize(records, JsonDefaults.Options);

	/// <summary>
	/// Adds a file or a dataset at the end of a folder.
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
			Index = TakeIndex(parent.Id),
			Kind = kind,
			Name = name,
			ParentId = parent.Id,
			UpdatedAt = _now
		});
	}

	/// <summary>
	/// Adds a folder at the end of another one.
	/// </summary>
	private FolderEntity AddFolder(FolderEntity parent, string name) => AddFolder(parent.Id, TakeIndex(parent.Id), name);

	/// <summary>
	/// Adds a folder at a position among the children of its parent, or of the root when there is no parent.
	/// </summary>
	private FolderEntity AddFolder(Guid? parentId, int index, string name)
	{
		FolderEntity folder = new()
		{
			CreatedAt = _now,
			Id = Guid.NewGuid(),
			Index = index,
			Kind = EntityKind.Folder,
			Name = name,
			ParentId = parentId,
			UpdatedAt = _now
		};

		_items.Add(folder);

		return folder;
	}

	/// <summary>
	/// Adds the files of known types and the datasets, each into the folder of its row, which is made on first use.
	/// </summary>
	private void AddKnownFiles(FolderEntity root)
	{
		// One row is one object, and the rows go in the order of the objects in their folders.
		(string Folder, string Name, EntityKind Kind, string Text)[] files =
		[
			("Code", "Program.cs", EntityKind.File, SampleText.CSharp),
			("Code", "Module.fs", EntityKind.File, SampleText.FSharp),
			("Code", "Module.vb", EntityKind.File, SampleText.VisualBasic),
			("Code", "main.c", EntityKind.File, SampleText.C),
			("Code", "main.cpp", EntityKind.File, SampleText.CPlusPlus),
			("Code", "Main.java", EntityKind.File, SampleText.Java),
			("Code", "main.go", EntityKind.File, SampleText.Go),
			("Code", "main.rs", EntityKind.File, SampleText.Rust),
			("Code", "Main.swift", EntityKind.File, SampleText.Swift),
			("Code", "main.dart", EntityKind.File, SampleText.Dart),
			("Scripts", "script.py", EntityKind.File, SampleText.Python),
			("Scripts", "script.js", EntityKind.File, SampleText.JavaScript),
			("Scripts", "script.ps1", EntityKind.File, SampleText.PowerShell),
			("Scripts", "script.sh", EntityKind.File, SampleText.Shell),
			("Scripts", "script.bat", EntityKind.File, SampleText.Batch),
			("Scripts", "script.rb", EntityKind.File, SampleText.Ruby),
			("Scripts", "script.lua", EntityKind.File, SampleText.Lua),
			("Scripts", "script.pl", EntityKind.File, SampleText.Perl),
			("Scripts", "script.php", EntityKind.File, SampleText.Php),
			("Web", "index.html", EntityKind.File, SampleDocuments.CreateHtml(_faker)),
			("Web", "styles.css", EntityKind.File, SampleDocuments.CreateCss(_faker)),
			("Web", "styles.scss", EntityKind.File, SampleDocuments.CreateScss(_faker)),
			("Web", "app.ts", EntityKind.File, SampleText.TypeScript),
			("Web", "App.tsx", EntityKind.File, SampleText.TypeScriptReact),
			("Web", "Index.cshtml", EntityKind.File, SampleText.Razor),
			("Data", "appsettings.json", EntityKind.File, SampleDocuments.CreateJson(_faker)),
			("Data", "settings.jsonc", EntityKind.File, SampleDocuments.CreateJsonWithComments(_faker)),
			("Data", "catalog.xml", EntityKind.File, SampleDocuments.CreateXml(_faker)),
			("Data", "transform.xsl", EntityKind.File, SampleText.Xsl),
			("Data", "config.yaml", EntityKind.File, SampleDocuments.CreateYaml(_faker)),
			("Data", "settings.ini", EntityKind.File, SampleDocuments.CreateIni(_faker)),
			("Data", "query.sql", EntityKind.File, SampleDocuments.CreateSql(_faker)),
			("Data", "users.csv", EntityKind.File, SampleDocuments.CreateCsv(_faker)),
			("Documents", "README.md", EntityKind.File, SampleDocuments.CreateMarkdown(_faker)),
			("Documents", "notes.txt", EntityKind.File, SampleDocuments.CreateText(_faker)),
			("Documents", "app.log", EntityKind.File, SampleDocuments.CreateLog(_faker)),
			("Documents", "article.tex", EntityKind.File, SampleDocuments.CreateLatex(_faker)),
			("Documents", "changes.diff", EntityKind.File, SampleDocuments.CreateDiff(_faker)),
			("Project", "App.csproj", EntityKind.File, SampleText.MsBuild),
			("Project", "MainWindow.axaml", EntityKind.File, SampleText.Xaml),
			("Project", "build.dockerfile", EntityKind.File, SampleText.Dockerfile),
			("Project", "build.mk", EntityKind.File, SampleText.Makefile),
			("Project", ".gitignore", EntityKind.File, SampleText.GitIgnore),
			("Project", ".editorconfig", EntityKind.File, SampleText.EditorConfig),
			("Datasets", "Values", EntityKind.Dataset, SerializeRecords(_records.CreateValueRecords(DatasetRecordCount))),
			("Datasets", "Key values", EntityKind.Dataset, SerializeRecords(_records.CreateKeyValueRecords(DatasetRecordCount))),
			("Datasets", "Groups", EntityKind.Dataset, SerializeRecords(_records.CreateRandomRecords(GroupedRecordCount, RecordLevels))),
			("Datasets", "Large", EntityKind.Dataset, SerializeRecords(_records.CreateRandomRecords(LargeRecordCount)))
		];

		Dictionary<string, FolderEntity> folders = [];

		foreach ((string folderName, string name, EntityKind kind, string text) in files)
		{
			if (!folders.TryGetValue(folderName, out FolderEntity? folder))
			{
				folder = AddFolder(root, folderName);

				folders.Add(folderName, folder);
			}

			AddFile(
				folder,
				name,
				kind,
				TextDefaults.Encoding.GetBytes(text));
		}
	}

	/// <summary>
	/// Fills a folder of the random branch: subfolders above the last level, then files, then datasets.
	/// </summary>
	private void AddRandomContents(FolderEntity folder, string path, int level)
	{
		if (level < RandomDepth)
		{
			for (int number = 1; number <= RandomFolderCount; number++)
			{
				// The name holds the path of the folder in the branch, such as 1.2.1.
				string childPath = path.Length == 0 ? $"{number}" : $"{path}.{number}";

				AddRandomContents(
					AddFolder(folder, $"Folder {childPath}"),
					childPath,
					level + 1);
			}
		}

		for (int number = 1; number <= RandomFileCount; number++)
		{
			AddFile(
				folder,
				CreateRandomFileName(number),
				EntityKind.File,
				_plainContents);
		}

		for (int number = 1; number <= RandomDatasetCount; number++)
		{
			AddFile(
				folder,
				$"Dataset {number}",
				EntityKind.Dataset,
				_datasetContents);
		}
	}

	/// <summary>
	/// Makes the objects of the run, the folder of the run first.
	/// </summary>
	private ExplorerItemBase[] Build(int rootIndex)
	{
		FolderEntity root = AddFolder(
			parentId: null,
			index: rootIndex,
			name: $"{RunFolderPrefix} {_now.ToString(RunFolderDateFormat, CultureInfo.InvariantCulture)}");

		AddKnownFiles(root);

		AddRandomContents(
			AddFolder(root, RandomFolderName),
			path: string.Empty,
			level: 0);

		return [.. _items];
	}

	/// <summary>
	/// Returns the name of a file of the random branch, with an extension that neither a grammar nor the check of
	/// executables knows.
	/// </summary>
	private string CreateRandomFileName(int number)
	{
		while (true)
		{
			string extension = new(_random.GetItems<char>(ExtensionAlphabet, ExtensionLength));

			string name = $"File {number}.{extension}";

			if (SyntaxRegistry.Instance.FindLanguage(name) is null && !ExecutableFileDetector.IsExecutable(name))
			{
				return name;
			}
		}
	}

	/// <summary>
	/// Returns the position of the next child of a folder.
	/// </summary>
	private int TakeIndex(Guid parentId)
	{
		ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(_childCounts, parentId, out _);

		return count++;
	}
	#endregion
}
