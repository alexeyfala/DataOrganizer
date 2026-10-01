using Bogus;
using DataOrganizer.Dto.Hierarchy;
using DataOrganizer.Helpers.Dataset;
using DataOrganizer.Helpers.Execution;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Models.Dataset;
using Entities.Enums;
using Entities.Models;
using Repository.Dto;
using Shared.Common;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
	/// Password of the encrypted folders.
	/// </summary>
	public const string KeeperPassword = "123456789";

	/// <summary>
	/// Length of the binary contents of a file, in bytes.
	/// </summary>
	private const int BinaryLength = 512;

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
	/// Share of the objects of the random branch with a made-up note.
	/// </summary>
	private const float NoteShare = 0.25f;

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
	/// Modifier held down for every key of the hotkey of a snippet.
	/// </summary>
	private const EventMask SnippetMask = EventMask.LeftCtrl;

	/// <summary>
	/// First keys of the hotkeys of the snippets, tried from left to right along the rows of the keyboard, the top row first.
	/// </summary>
	private static readonly KeyCode[] HotkeyLeaders =
	[
		KeyCode.VcQ, KeyCode.VcW, KeyCode.VcE, KeyCode.VcR, KeyCode.VcT, KeyCode.VcY, KeyCode.VcU, KeyCode.VcI, KeyCode.VcO, KeyCode.VcP,
		KeyCode.VcA, KeyCode.VcS, KeyCode.VcD, KeyCode.VcF, KeyCode.VcG, KeyCode.VcH, KeyCode.VcJ, KeyCode.VcK, KeyCode.VcL,
		KeyCode.VcZ, KeyCode.VcX, KeyCode.VcC, KeyCode.VcV, KeyCode.VcB, KeyCode.VcN, KeyCode.VcM
	];

	/// <summary>
	/// Order of the names in a folder: regardless of case, with numbers compared by their value.
	/// </summary>
	private static readonly StringComparer NameComparer = StringComparer.Create(
		CultureInfo.InvariantCulture,
		CompareOptions.IgnoreCase | CompareOptions.NumericOrdering);

	/// <summary>
	/// Identifiers of the files made so far whose contents are to be damaged once they are encrypted.
	/// </summary>
	private readonly List<Guid> _damagedFileIds = [];

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
	/// Identifiers of the folders made so far that are to be encrypted.
	/// </summary>
	private readonly List<Guid> _keeperIds = [];

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

	/// <summary>
	/// Hotkeys a snippet must not clash with: those of the saved files and those given out so far.
	/// </summary>
	private readonly List<KeyStroke[]> _takenHotkeys;

	/// <summary>
	/// Number of made-up notes given out so far.
	/// </summary>
	private int _noteCount;
	#endregion

	#region Constructors
	private SampleHierarchy(DateTime now, KeyStroke[][] takenHotkeys)
	{
		_now = now;

		_takenHotkeys = [.. takenHotkeys];

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
	/// Creates the objects of a run under a folder of its own at a position of the root; each object comes after its folder,
	/// and the hotkeys of the snippets keep clear of the given ones.
	/// </summary>
	public static SampleObjects Create(
		int rootIndex,
		DateTime now,
		KeyStroke[][] takenHotkeys)
	{
		return new SampleHierarchy(now, takenHotkeys).Build(rootIndex);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when one hotkey is a run of keys inside the other, so that typing the longer one fires both.
	/// </summary>
	private static bool Clashes(KeyStroke[] first, KeyStroke[] second)
	{
		return first.Length >= second.Length
			? first.AsSpan().IndexOf(second) >= 0
			: second.AsSpan().IndexOf(first) >= 0;
	}

	/// <summary>
	/// Returns a hotkey whose keys are all pressed with the modifier of the snippets.
	/// </summary>
	private static KeyStroke[] CreateHotkey(params ReadOnlySpan<KeyCode> keys)
	{
		KeyStroke[] hotkey = new KeyStroke[keys.Length];

		for (int i = 0; i < keys.Length; i++)
		{
			hotkey[i] = new KeyStroke
			{
				Code = keys[i],
				Mask = SnippetMask
			};
		}

		return hotkey;
	}

	/// <summary>
	/// Returns the stored keys of the hotkey of a file, in the order they are pressed.
	/// </summary>
	private static List<HotkeyEntity> CreateHotkeyEntities(KeyStroke[] hotkey, Guid ownerId) => [.. hotkey.Select((x, index) => new HotkeyEntity
	{
		Code = x.Code,
		Id = Guid.NewGuid(),
		Index = index,
		Mask = x.Mask,
		OwnerId = ownerId
	})];

	/// <summary>
	/// Returns the first half of a text, so that a structured text breaks off unfinished.
	/// </summary>
	private static string CutInHalf(string text) => text[..(text.Length / 2)];

	/// <summary>
	/// Returns a note of an object that is not encrypted in its stored binary form.
	/// </summary>
	private static byte[]? EncodeNote(string? note) => note is null ? null : TextDefaults.Encoding.GetBytes(note);

	/// <summary>
	/// Returns the text of a dataset that holds records.
	/// </summary>
	private static string SerializeRecords(IEnumerable<DatasetRecordBase> records) => JsonSerializer.Serialize(records, JsonDefaults.Options);

	/// <summary>
	/// Adds a file or a dataset to a folder.
	/// </summary>
	private FileEntity AddFile(
		FolderEntity parent,
		string name,
		EntityKind kind,
		byte[] contents,
		string? note = null,
		bool isFavorite = false,
		KeyStroke[]? hotkey = null)
	{
		Guid id = Guid.NewGuid();

		FileEntity file = new()
		{
			Contents = contents,
			CreatedAt = _now,
			Hotkeys = hotkey is null ? [] : CreateHotkeyEntities(hotkey, id),
			Id = id,
			IsFavorite = isFavorite,
			Kind = kind,
			Name = name,
			Note = EncodeNote(note),
			ParentId = parent.Id,
			UpdatedAt = _now
		};

		_items.Add(file);

		return file;
	}

	/// <summary>
	/// Adds a folder to another one, or to the root when there is no parent.
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
			Kind = EntityKind.Folder,
			Name = name,
			Note = EncodeNote(note),
			ParentId = parent?.Id,
			UpdatedAt = _now
		};

		_items.Add(folder);

		return folder;
	}

	/// <summary>
	/// Adds the files of known types and the datasets, each into the folder of its row; a row names the folder by its path,
	/// and the folders of the path are made on first use.
	/// </summary>
	private void AddKnownFiles(FolderEntity root)
	{
		// One row is one object; its place in the folder comes from its name.
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
			("Datasets", "Large", EntityKind.Dataset, SerializeRecords(_records.CreateRandomRecords(LargeRecordCount))),
			("Encrypted/Passwords", "recovery-codes.txt", EntityKind.File, SampleDocuments.CreateRecoveryCodes(_faker)),
			("Encrypted/Passwords", "secrets.json", EntityKind.File, SampleDocuments.CreateSecrets(_faker)),
			("Encrypted/Passwords", "Accounts", EntityKind.Dataset, SerializeRecords(_records.CreateAccounts(DatasetRecordCount))),
			("Encrypted/Private/Documents", "diary.md", EntityKind.File, SampleDocuments.CreateMarkdown(_faker)),
			("Encrypted/Private/Documents", "contacts.csv", EntityKind.File, SampleDocuments.CreateCsv(_faker)),
			("Encrypted/Private/Scripts", "backup.bat", EntityKind.File, SampleText.Batch),
			("Encrypted/Private", "Records", EntityKind.Dataset, SerializeRecords(_records.CreateRandomRecords(GroupedRecordCount, RecordLevels))),
			("Encrypted/Private", "Large", EntityKind.Dataset, SerializeRecords(_records.CreateRandomRecords(LargeRecordCount))),
			("Snippets", "address.txt", EntityKind.File, SampleSnippets.CreateAddress(_faker)),
			("Snippets", "bank.txt", EntityKind.File, SampleSnippets.CreateBankDetails(_faker)),
			("Snippets", "email.txt", EntityKind.File, SampleSnippets.CreateEmail(_faker)),
			("Snippets", "phone.txt", EntityKind.File, SampleSnippets.CreatePhoneNumber(_faker)),
			("Snippets", "signature.txt", EntityKind.File, SampleSnippets.CreateSignature(_faker)),
			("Snippets/Secret", "card.txt", EntityKind.File, SampleSnippets.CreateCardPin(_faker)),
			("Snippets/Secret", "door.txt", EntityKind.File, SampleSnippets.CreateDoorCode(_faker)),
			("Snippets/Secret", "router.txt", EntityKind.File, SampleSnippets.CreateRouterPassword(_faker)),
			("Snippets/Secret", "wifi.txt", EntityKind.File, SampleSnippets.CreateWifi(_faker)),
			("Broken", "Records", EntityKind.Dataset, CutInHalf(SerializeRecords(_records.CreateValueRecords(DatasetRecordCount)))),
			("Broken/Protected", "letter.txt", EntityKind.File, SampleDocuments.CreateText(_faker))
		];

		// The snippets are favorites with a hotkey: a first key, then two keys along a column of the keyboard, down it for
		// the plain snippets and up it for the encrypted ones.
		Dictionary<string, KeyCode[]> snippets = new()
		{
			["Snippets/address.txt"] = [KeyCode.VcA, KeyCode.VcZ],
			["Snippets/bank.txt"] = [KeyCode.VcS, KeyCode.VcX],
			["Snippets/email.txt"] = [KeyCode.VcD, KeyCode.VcC],
			["Snippets/phone.txt"] = [KeyCode.VcF, KeyCode.VcV],
			["Snippets/signature.txt"] = [KeyCode.VcG, KeyCode.VcB],
			["Snippets/Secret/card.txt"] = [KeyCode.VcZ, KeyCode.VcA],
			["Snippets/Secret/door.txt"] = [KeyCode.VcX, KeyCode.VcS],
			["Snippets/Secret/router.txt"] = [KeyCode.VcC, KeyCode.VcD],
			["Snippets/Secret/wifi.txt"] = [KeyCode.VcV, KeyCode.VcF]
		};

		Dictionary<string, KeyStroke[]?> hotkeys = snippets.ToDictionary(static x => x.Key, x => TakeHotkey(x.Value));

		// Only a few objects have a note, found by their path.
		Dictionary<string, string> notes = new()
		{
			["Broken"] = SampleNotes.CreateEncryptedFolder(KeeperPassword),
			["Broken/Protected/letter.txt"] = SampleNotes.DamagedContents,
			["Broken/Records"] = SampleNotes.TruncatedRecords,
			["Encrypted"] = SampleNotes.CreateEncryptedFolder(KeeperPassword),
			["Encrypted/Passwords"] = SampleNotes.Keeper,
			["Encrypted/Passwords/recovery-codes.txt"] = SampleNotes.RecoveryCodes,
			["Encrypted/Private"] = SampleNotes.Keeper,
			["Encrypted/Private/Scripts/backup.bat"] = SampleNotes.EncryptedScript,
			["Scripts"] = SampleNotes.Scripts,
			["Snippets"] = SampleNotes.CreateSnippetsFolder(
				hotkeys.Select(static x => (x.Key["Snippets/".Length..], x.Value)),
				KeeperPassword),
			["Snippets/Secret"] = SampleNotes.Keeper
		};

		// The folders that are encrypted once the objects are saved.
		HashSet<string> keepers = ["Broken/Protected", "Encrypted/Passwords", "Encrypted/Private", "Snippets/Secret"];

		// The files whose ciphertext is damaged once their folder is encrypted.
		HashSet<string> damaged = ["Broken/Protected/letter.txt"];

		Dictionary<string, FolderEntity> folders = [];

		foreach ((string folderPath, string name, EntityKind kind, string text) in files)
		{
			string path = $"{folderPath}/{name}";

			FileEntity file = AddFile(
				GetOrAddFolder(folderPath),
				name,
				kind,
				TextDefaults.Encoding.GetBytes(text),
				notes.GetValueOrDefault(path),
				snippets.ContainsKey(path),
				hotkeys.GetValueOrDefault(path));

			if (damaged.Contains(path))
			{
				_damagedFileIds.Add(file.Id);
			}
		}

		// Binary contents cannot pass through a string, so this file stays out of the table.
		AddFile(
			GetOrAddFolder("Broken"),
			"report.txt",
			EntityKind.File,
			_faker.Random.Bytes(BinaryLength),
			SampleNotes.BinaryContents);

		// Returns the folder of a path, made on first use after the folders above it.
		FolderEntity GetOrAddFolder(string path)
		{
			if (folders.TryGetValue(path, out FolderEntity? folder))
			{
				return folder;
			}

			int separator = path.LastIndexOf('/');

			folder = AddFolder(
				separator < 0 ? root : GetOrAddFolder(path[..separator]),
				path[(separator + 1)..],
				notes.GetValueOrDefault(path));

			folders.Add(path, folder);

			if (keepers.Contains(path))
			{
				_keeperIds.Add(folder.Id);
			}

			return folder;
		}
	}

	/// <summary>
	/// Fills a folder of the random branch with subfolders above the last level, files and datasets.
	/// </summary>
	private void AddRandomContents(FolderEntity folder, string path, int level)
	{
		if (level < RandomDepth)
		{
			for (int number = 1; number <= RandomFolderCount; number++)
			{
				// The name holds the path of the folder in the branch, such as 1.2.1.
				string childPath = path.Length == 0 ? $"{number}" : $"{path}.{number}";

				FolderEntity subfolder = AddFolder(
					folder,
					$"Folder {childPath}",
					CreateRandomNote());

				AddRandomContents(
					subfolder,
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
				_plainContents,
				CreateRandomNote());
		}

		for (int number = 1; number <= RandomDatasetCount; number++)
		{
			AddFile(
				folder,
				$"Dataset {number}",
				EntityKind.Dataset,
				_datasetContents,
				CreateRandomNote());
		}
	}

	/// <summary>
	/// Makes the objects of the run, the folder of the run first.
	/// </summary>
	private SampleObjects Build(int rootIndex)
	{
		FolderEntity root = AddFolder(
			parent: null,
			name: $"{RunFolderPrefix} {_now.ToString(RunFolderDateFormat, CultureInfo.InvariantCulture)}",
			note: SampleNotes.RunFolder);

		root.Index = rootIndex;

		AddKnownFiles(root);

		FolderEntity randomFolder = AddFolder(
			root,
			RandomFolderName,
			SampleNotes.RandomFolder);

		AddRandomContents(
			randomFolder,
			path: string.Empty,
			level: 0);

		SortChildren();

		return new()
		{
			DamagedFileIds = [.. _damagedFileIds],
			Items = [.. _items],
			KeeperIds = [.. _keeperIds]
		};
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
	/// Returns a made-up note for a share of the objects of the random branch, or <c>null</c> for the others.
	/// </summary>
	private string? CreateRandomNote() => _faker.Random.Bool(NoteShare)
		? SampleNotes.Create(_faker, _noteCount++)
		: null;

	/// <summary>
	/// Numbers the children of every folder: subfolders first, then files and datasets, each group by name.
	/// </summary>
	private void SortChildren()
	{
		// The folder of the run keeps the position it was given among the objects of the root.
		IEnumerable<IGrouping<Guid?, ExplorerItemBase>> families = _items
			.Where(x => x.ParentId is not null)
			.GroupBy(x => x.ParentId);

		foreach (IGrouping<Guid?, ExplorerItemBase> children in families)
		{
			foreach ((int index, ExplorerItemBase child) in children
				.OrderBy(x => x.Kind != EntityKind.Folder)
				.ThenBy(x => x.Name, NameComparer)
				.Index())
			{
				child.Index = index;
			}
		}
	}

	/// <summary>
	/// Gives out the hotkey of a snippet: the first key that leaves it clear of the hotkeys in use, then the keys of the
	/// snippet; <c>null</c> when every first key is taken.
	/// </summary>
	private KeyStroke[]? TakeHotkey(KeyCode[] keys)
	{
		foreach (KeyCode leader in HotkeyLeaders)
		{
			KeyStroke[] hotkey = CreateHotkey([leader, .. keys]);

			if (_takenHotkeys.Any(x => Clashes(x, hotkey)))
			{
				continue;
			}

			_takenHotkeys.Add(hotkey);

			return hotkey;
		}

		return null;
	}
	#endregion
}
