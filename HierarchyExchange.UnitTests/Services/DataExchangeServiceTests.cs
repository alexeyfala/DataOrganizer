using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DialogContracts.Dto;
using DialogContracts.Interfaces;
using Entities.Models;
using FileSystemOperations.Interfaces;
using FileSystemOperations.Services;
using GlobalHotkeys.Interfaces;
using GlobalHotkeys.Services;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using HierarchyExchange.Dto;
using HierarchyExchange.Enums;
using HierarchyExchange.Interfaces;
using HierarchyExchange.Services;
using HierarchyStorage.Interfaces;
using HierarchyStorage.Services;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Repository.Dto;
using Repository.Enums;
using Repository.Interfaces.Database;
using Repository.Services.Database;
using Shared.Common;
using Shared.Interfaces;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TestSupport.Common;
using TestSupport.Database;
using TestSupport.Models;

namespace HierarchyExchange.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(DataExchangeService)}"" type")]
internal class DataExchangeServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="DataExchangeService.ExportDataAsync" />: every folder and file of the database, with contents and editor
	/// state, is written to the chosen JSON file.
	/// </summary>
	[Test]
	public async Task ExportDataAsync_Exports_To_Json()
	{
		// Arrange
		string filePath = RandomValues.CreateFileName(10, KnownFileExtensions.Json);

		FolderEntity[] folders = [.. EntityFactory.CreateFolders(2)];

		FileEntity[] files = [.. EntityFactory.CreateFiles(2)];

		ExplorerItemBase[] entities = [.. folders, .. files];

		using MemoryStream stream = new();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetAllFoldersAsync(Arg.Any<CancellationToken>())
				.Returns(folders);

			dbAccess
				.GetAllFilesAsync(OptionalFileProperties.Contents | OptionalFileProperties.EditorState, Arg.Any<CancellationToken>())
				.Returns(files);

			fileSystem
				.CreateSequentialWrite(filePath)
				.Returns(stream);

			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SaveFileAsync(Arg.Any<FileSaveParameters>())
				.Returns(filePath);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ExportDataAsync();

		// Assert
		fileSystem
			.Received(1)
			.CreateSequentialWrite(filePath);

		await serializer.Received(1).SerializeAsync(
			stream,
			Arg.Is<ExplorerItemBase[]>(x => x.SequenceEqual(entities)),
			JsonDefaults.Options,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ExportDataAsync" />: the database is copied into the chosen SQLite file.
	/// </summary>
	[Test]
	public async Task ExportDataAsync_Exports_To_Sqlite()
	{
		// Arrange
		string filePath = RandomValues.CreateFileName(10, KnownFileExtensions.Sqlite);

		IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SaveFileAsync(Arg.Any<FileSaveParameters>())
				.Returns(filePath);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<SqliteExchangeFormat>()
				.As<IExchangeFormat>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ExportDataAsync();

		// Assert
		await dbBackupService
			.Received(1)
			.CopyToAsync(filePath, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ExportDataAsync" />: every folder and file of the database, with contents and editor
	/// state, is written to the chosen XML file.
	/// </summary>
	[Test]
	public async Task ExportDataAsync_Exports_To_Xml()
	{
		// Arrange
		string filePath = RandomValues.CreateFileName(10, KnownFileExtensions.Xml);

		FolderEntity[] folders = [.. EntityFactory.CreateFolders(2)];

		FileEntity[] files = [.. EntityFactory.CreateFiles(2)];

		ExplorerItemBase[] entities = [.. folders, .. files];

		using MemoryStream stream = new();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IXmlSerializer serializer = Substitute.For<IXmlSerializer>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetAllFoldersAsync(Arg.Any<CancellationToken>())
				.Returns(folders);

			dbAccess
				.GetAllFilesAsync(OptionalFileProperties.Contents | OptionalFileProperties.EditorState, Arg.Any<CancellationToken>())
				.Returns(files);

			fileSystem
				.CreateSequentialWrite(filePath)
				.Returns(stream);

			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SaveFileAsync(Arg.Any<FileSaveParameters>())
				.Returns(filePath);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<XmlExchangeFormat>()
				.As<IExchangeFormat>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ExportDataAsync();

		// Assert
		fileSystem
			.Received(1)
			.CreateSequentialWrite(filePath);

		serializer
			.Received(1)
			.Serialize(stream, Arg.Is<ExplorerItemBase[]>(x => x.SequenceEqual(entities)));
	}

	/// <summary>
	/// <see cref="DataExchangeService.ExportDataAsync" />: the file picker offers each format as a file type, in a fixed
	/// order, and suggests JSON.
	/// </summary>
	[Test]
	public async Task ExportDataAsync_Offers_The_File_Types_Of_The_Formats()
	{
		// Arrange
		FileSaveParameters? parameters = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			// The picker is closed, so the export stops at the choice.
			picker
				.SaveFileAsync(Arg.Do<FileSaveParameters>(x => parameters = x))
				.Returns((string?)null);

			builder.RegisterInstance(picker);

			// The file picker lists the formats in the order they are registered.
			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<XmlExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<SqliteExchangeFormat>()
				.As<IExchangeFormat>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ExportDataAsync();

		// Assert
		parameters!.FileTypes.Select(static x => (x.Name, string.Join(", ", x.Patterns), string.Join(", ", x.MimeTypes!)))
			.Should()
			.Equal(
				("JSON File", "*.json", "application/json"),
				("XML File", "*.xml", "application/xml"),
				("SQLite Database File", "*.sqlite", "application/x-sqlite3"));

		parameters.DefaultExtension
			.Should()
			.Be("json");
	}

	/// <summary>
	/// <see cref="DataExchangeService.ExportDataAsync" />: a file of a type no format knows is not written.
	/// </summary>
	[Test]
	public async Task ExportDataAsync_Writes_Nothing_To_A_File_Of_Another_Type()
	{
		// Arrange
		IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SaveFileAsync(Arg.Any<FileSaveParameters>())
				.Returns(RandomValues.CreateFileName(10, KnownFileExtensions.Txt));

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(picker);

			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<XmlExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<SqliteExchangeFormat>()
				.As<IExchangeFormat>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ExportDataAsync();

		// Assert
		fileSystem
			.DidNotReceive()
			.CreateSequentialWrite(Arg.Any<string>());

		await dbBackupService
			.DidNotReceive()
			.CopyToAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: the folders and files of an XML file are added to the database,
	/// dated with the time of the import, since the file keeps no dates.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Adds_The_Objects_Of_An_Xml_File()
	{
		// Arrange
		FolderEntity[] folders = [.. EntityFactory.CreateFolders(2)];

		FileEntity[] files = [.. EntityFactory.CreateFiles(2)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Xml)]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IXmlSerializer serializer = Substitute.For<IXmlSerializer>();

			serializer
				.LoadDocumentAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(new XDocument(new XElement("ArrayOfEntry")));

			serializer
				.Deserialize<ExplorerItemBase[]>(Arg.Any<XDocument>())
				.Returns([.. folders, .. files]);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<XmlExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ImportDataAsync([]);

		// Assert
		await dbAccess.Received(1).AddFoldersAsync(
			Arg.Is<IEnumerable<FolderEntity>>(x => x.SequenceEqual(folders)),
			Arg.Any<CancellationToken>());

		await dbAccess.Received(1).AddFilesAsync(
			Arg.Is<IEnumerable<FileEntity>>(x => x.SequenceEqual(files)),
			Arg.Any<CancellationToken>());

		folders.Concat<ExplorerItemBase>(files)
			.Should()
			.AllSatisfy(x => x.CreatedAt.Should().Be(time.GetLocalNow().LocalDateTime))
			.And
			.AllSatisfy(x => x.UpdatedAt.Should().Be(time.GetLocalNow().LocalDateTime));
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: appending a database file adds its folders and files under new
	/// identifiers with their own dates, and keeps the objects already in the database.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Appends_The_Objects_Of_A_Database_File()
	{
		// Arrange
		string filePath = RandomValues.CreateFileName(10, KnownFileExtensions.Sqlite);

		FolderEntity[] folders = [.. EntityFactory.CreateFolders(2)];

		FileEntity[] files = [.. EntityFactory.CreateFiles(2)];

		ExplorerItemBase[] entities = [.. folders, .. files];

		Guid[] ids = [.. entities.Select(static x => x.Id)];

		DateTime[] createdAt = [.. entities.Select(static x => x.CreatedAt)];

		DateTime[] updatedAt = [.. entities.Select(static x => x.UpdatedAt)];

		LoadedHierarchy mapped = ItemDtoFactory.CreateLoadedHierarchy([ItemDtoFactory.CreateFolderDto()]);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IDbImporter dbImporter = Substitute.For<IDbImporter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IImportModePrompt importModePrompt = Substitute.For<IImportModePrompt>();

			importModePrompt
				.SelectImportModeAsync()
				.Returns(ImportMode.Append);

			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([filePath]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			dbImporter
				.IsValidSqliteDatabase(filePath)
				.Returns(true);

			dbImporter
				.LoadEntities(filePath)
				.Returns(new LoadedEntities
				{
					Files = files,
					Folders = folders
				});

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			entityLoader
				.Map(Arg.Any<IReadOnlyCollection<FolderEntity>>(), Arg.Any<IReadOnlyCollection<FileEntity>>())
				.Returns(mapped);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(dbImporter);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(importModePrompt);

			builder.RegisterInstance(picker);

			builder
				.RegisterType<SqliteExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([ItemDtoFactory.CreateFolderDto()]);

		// Assert
		result!.Imported
			.Should()
			.BeSameAs(mapped);

		await dbAccess.Received(1).AddFoldersAsync(
			Arg.Is<IEnumerable<FolderEntity>>(x => x.SequenceEqual(folders)),
			Arg.Any<CancellationToken>());

		await dbAccess.Received(1).AddFilesAsync(
			Arg.Is<IEnumerable<FileEntity>>(x => x.SequenceEqual(files)),
			Arg.Any<CancellationToken>());

		await dbImporter
			.DidNotReceive()
			.ClearDatabaseAsync(Arg.Any<CancellationToken>());

		entities.Select(static x => x.Id)
			.Should()
			.NotIntersectWith(ids);

		entities.Select(static x => x.CreatedAt)
			.Should()
			.Equal(createdAt);

		entities.Select(static x => x.UpdatedAt)
			.Should()
			.Equal(updatedAt);
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: with objects already in the tree, the mode chosen by the user
	/// decides the import, so appending keeps the database and the tree.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Appends_When_Append_Is_Chosen()
	{
		// Arrange
		IDbImporter dbImporter = Substitute.For<IDbImporter>();

		Collection<ExplorerItemDtoBase> hierarchy = [ItemDtoFactory.CreateFolderDto()];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IImportModePrompt importModePrompt = Substitute.For<IImportModePrompt>();

			importModePrompt
				.SelectImportModeAsync()
				.Returns(ImportMode.Append);

			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Json)]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeAsync<ExplorerItemBase[]>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns([]);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(dbImporter);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(importModePrompt);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<EntityLoader>()
				.As<IEntityLoader>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync(hierarchy);

		// Assert
		result!.Variant
			.Should()
			.Be(ImportMode.Append);

		await dbImporter
			.DidNotReceive()
			.ClearDatabaseAsync(Arg.Any<CancellationToken>());

		hierarchy
			.Should()
			.ContainSingle();
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: returns null and restores the backup when JSON deserialization yields no data.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Cannot_Import_From_Json()
	{
		// Arrange
		IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Json)]);

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeAsync<ExplorerItemBase[]>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(default(ExplorerItemBase[]));

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.BeNull();

		await dbBackupService
			.Received(1)
			.RestoreFromBackupAsync(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: returns null and restores the backup when the SQLite source is invalid.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Cannot_Import_From_SQLite()
	{
		// Arrange
		IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Sqlite)]);

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(picker);

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<SqliteExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.BeNull();

		await dbBackupService
			.Received(1)
			.RestoreFromBackupAsync(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: returns null and restores the backup when XML deserialization yields no data.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Cannot_Import_From_Xml()
	{
		// Arrange
		IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Xml)]);

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IXmlSerializer serializer = Substitute.For<IXmlSerializer>();

			serializer
				.LoadDocumentAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(new XDocument(new XElement("ArrayOfEntry")));

			serializer
				.Deserialize<ExplorerItemBase[]>(Arg.Any<XDocument>())
				.Returns(default(ExplorerItemBase[]));

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(dbBackupService);

			builder
				.RegisterType<XmlExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.BeNull();

		await dbBackupService
			.Received(1)
			.RestoreFromBackupAsync(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: the objects of a JSON file are dated with the time of the import
	/// in either mode, since the file keeps no dates.
	/// </summary>
	[TestCase(ImportMode.Append)]
	[TestCase(ImportMode.Replace)]
	public async Task ImportDataAsync_Dates_The_Objects_Of_A_Json_File_With_The_Import_Time(ImportMode variant)
	{
		// Arrange
		ExplorerItemBase[] entities = [.. EntityFactory.CreateFolders(2), .. EntityFactory.CreateFiles(2)];

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IImportModePrompt importModePrompt = Substitute.For<IImportModePrompt>();

			importModePrompt
				.SelectImportModeAsync()
				.Returns(variant);

			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Json)]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeAsync<ExplorerItemBase[]>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(entities);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(importModePrompt);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ImportDataAsync([ItemDtoFactory.CreateFolderDto()]);

		// Assert
		entities
			.Should()
			.AllSatisfy(x => x.CreatedAt.Should().Be(time.GetLocalNow().LocalDateTime))
			.And
			.AllSatisfy(x => x.UpdatedAt.Should().Be(time.GetLocalNow().LocalDateTime));
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: the imported objects get new identifiers, and the links between
	/// them follow: a folder keeps its subfolder, the subfolder its file, the file its hotkey.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Gives_The_Imported_Objects_New_Identifiers()
	{
		// Arrange
		FolderEntity folder = EntityFactory.CreateFolder();

		FolderEntity subfolder = EntityFactory.CreateFolder(parentId: folder.Id);

		FileEntity file = EntityFactory.CreateFile(parentId: subfolder.Id);

		HotkeyEntity hotkey = new()
		{
			Code = KeyCode.VcA,
			Id = Guid.NewGuid(),
			Mask = EventMask.LeftCtrl,
			OwnerId = file.Id
		};

		file
			.Hotkeys
			.Add(hotkey);

		Guid[] ids = [folder.Id, subfolder.Id, file.Id, hotkey.Id];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Json)]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeAsync<ExplorerItemBase[]>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns([folder, subfolder, file]);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ImportDataAsync([]);

		// Assert
		Guid[] newIds = [folder.Id, subfolder.Id, file.Id, hotkey.Id];

		newIds
			.Should()
			.NotIntersectWith(ids);

		subfolder.ParentId
			.Should()
			.Be(folder.Id);

		file.ParentId
			.Should()
			.Be(subfolder.Id);

		hotkey.OwnerId
			.Should()
			.Be(file.Id);
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: hotkeys that could not be read do not stop the import
	/// and are removed from the file.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Imports_A_File_With_Unreadable_Hotkeys()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		file
			.Hotkeys
			.Add(new()
			{
				Code = KeyCode.VcUndefined,
				Id = Guid.NewGuid(),
				Index = 0,
				Mask = EventMask.LeftCtrl,
				OwnerId = file.Id
			});

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Sqlite)]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			dbAccess
				.DeleteHotkeysAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			IDbImporter dbImporter = Substitute.For<IDbImporter>();

			dbImporter
				.IsValidSqliteDatabase(Arg.Any<string>())
				.Returns(true);

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns(ItemDtoFactory.CreateLoadedHierarchy([file]));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(dbImporter);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(picker);

			builder
				.RegisterType<SqliteExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<UnreadableHotkeysFinder>()
				.As<IUnreadableHotkeysFinder>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.NotBeNull();

		result.Imported.Items
			.Should()
			.Contain(file);

		file
			.Hotkeys
			.Should()
			.BeEmpty();

		await dbAccess
			.Received(1)
			.DeleteHotkeysAsync(file.Id, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: returns a non-null result when importing from a JSON file.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Imports_From_Json()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Json)]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeAsync<ExplorerItemBase[]>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns([]);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<EntityLoader>()
				.As<IEntityLoader>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: returns a non-null result when importing from a valid SQLite file.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Imports_From_SQLite()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Sqlite)]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			IDbImporter dbImporter = Substitute.For<IDbImporter>();

			dbImporter
				.IsValidSqliteDatabase(Arg.Any<string>())
				.Returns(true);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(dbImporter);

			builder.RegisterInstance(picker);

			builder
				.RegisterType<SqliteExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<EntityLoader>()
				.As<IEntityLoader>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: returns a non-null result when importing from an XML file.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Imports_From_Xml()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Xml)]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IXmlSerializer serializer = Substitute.For<IXmlSerializer>();

			serializer
				.LoadDocumentAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(new XDocument(new XElement("ArrayOfEntry")));

			serializer
				.Deserialize<ExplorerItemBase[]>(Arg.Any<XDocument>())
				.Returns([]);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<XmlExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<EntityLoader>()
				.As<IEntityLoader>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: with objects already in the tree, nothing is imported when the
	/// user chooses no mode.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Imports_Nothing_When_No_Mode_Is_Chosen()
	{
		// Arrange
		IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IImportModePrompt importModePrompt = Substitute.For<IImportModePrompt>();

			// No choice is the scenario, though it is also the default answer.
			importModePrompt
				.SelectImportModeAsync()
				.Returns(ImportMode.None);

			builder.RegisterInstance(importModePrompt);

			builder.RegisterInstance(picker);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([ItemDtoFactory.CreateFolderDto()]);

		// Assert
		result
			.Should()
			.BeNull();

		await picker
			.DidNotReceive()
			.SelectFilesAsync(Arg.Any<FileOpenParameters>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: without a copy of the database to roll back to, the file is not
	/// read.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Imports_Nothing_Without_A_Backup()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Json)]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			// No copy of the database can be made.
			dbBackupService
				.CreateBackupAsync()
				.Returns((DatabaseBackup?)null);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(picker);

			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.BeNull();

		fileSystem
			.DidNotReceive()
			.OpenSequentialRead(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: a replacing import leaves the tree it is given as it is, since the
	/// caller puts the imported objects in place of it.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Leaves_The_Tree_Unchanged_On_Replacement()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		Collection<ExplorerItemDtoBase> hierarchy = [folder];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IImportModePrompt importModePrompt = Substitute.For<IImportModePrompt>();

			importModePrompt
				.SelectImportModeAsync()
				.Returns(ImportMode.Replace);

			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Json)]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeAsync<ExplorerItemBase[]>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns([.. EntityFactory.CreateFolders(2)]);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(importModePrompt);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<EntityLoader>()
				.As<IEntityLoader>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync(hierarchy);

		// Assert
		result!.Variant
			.Should()
			.Be(ImportMode.Replace);

		hierarchy
			.Should()
			.Equal(folder);
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: the file picker offers all formats together, then each format as
	/// a file type, in a fixed order.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Offers_The_File_Types_Of_The_Formats()
	{
		// Arrange
		FileOpenParameters? parameters = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker.SelectFilesAsync(Arg.Do<FileOpenParameters>(x => parameters = x));

			builder.RegisterInstance(picker);

			// The file picker lists the formats in the order they are registered.
			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<XmlExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<SqliteExchangeFormat>()
				.As<IExchangeFormat>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ImportDataAsync([]);

		// Assert
		parameters!.FileTypes.Select(static x => (x.Name, string.Join(", ", x.Patterns), string.Join(", ", x.MimeTypes!)))
			.Should()
			.Equal(
				("All Supported Files", "*.json, *.xml, *.sqlite", "application/json, application/xml, application/x-sqlite3"),
				("JSON File", "*.json", "application/json"),
				("XML File", "*.xml", "application/xml"),
				("SQLite Database File", "*.sqlite", "application/x-sqlite3"));
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: a replacing import of a database file puts the file in place of
	/// the database and reads the objects back from it.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Puts_A_Database_File_In_Place_Of_The_Database()
	{
		// Arrange
		string filePath = RandomValues.CreateFileName(10, KnownFileExtensions.Sqlite);

		LoadedHierarchy loaded = ItemDtoFactory.CreateLoadedHierarchy([ItemDtoFactory.CreateFolderDto()]);

		IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

		IDbImporter dbImporter = Substitute.For<IDbImporter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([filePath]);

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			dbImporter
				.IsValidSqliteDatabase(filePath)
				.Returns(true);

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns(loaded);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(dbImporter);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(picker);

			builder
				.RegisterType<SqliteExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result!.Imported
			.Should()
			.BeSameAs(loaded);

		await dbBackupService
			.Received(1)
			.RestoreFromBackupAsync(filePath, Arg.Any<CancellationToken>());

		dbImporter
			.DidNotReceive()
			.LoadEntities(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: appended objects at the top of the tree follow the objects already
	/// there in their own order, while objects inside folders keep their places.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Puts_The_Appended_Objects_After_The_Tree()
	{
		// Arrange
		FolderDto first = ItemDtoFactory.CreateFolderDto();

		first.Index = 3;

		FolderDto last = ItemDtoFactory.CreateFolderDto();

		last.Index = 7;

		FolderEntity folder = EntityFactory.CreateFolder();

		folder.Index = 20;

		FileEntity file = EntityFactory.CreateFile();

		file.Index = 10;

		FileEntity child = EntityFactory.CreateFile(parentId: folder.Id);

		child.Index = 5;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IImportModePrompt importModePrompt = Substitute.For<IImportModePrompt>();

			importModePrompt
				.SelectImportModeAsync()
				.Returns(ImportMode.Append);

			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Json)]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeAsync<ExplorerItemBase[]>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns([folder, file, child]);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(importModePrompt);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ImportDataAsync([last, first]);

		// Assert
		file.Index
			.Should()
			.Be(8);

		folder.Index
			.Should()
			.Be(9);

		child.Index
			.Should()
			.Be(5);
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: a replacing import of a JSON file clears the database first, then
	/// adds the folders and files of the file and gives back what they map to.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Replaces_The_Objects_Of_The_Database_With_Those_Of_A_Json_File()
	{
		// Arrange
		FolderEntity[] folders = [.. EntityFactory.CreateFolders(2)];

		FileEntity[] files = [.. EntityFactory.CreateFiles(2)];

		LoadedHierarchy mapped = ItemDtoFactory.CreateLoadedHierarchy([ItemDtoFactory.CreateFolderDto()]);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IDbImporter dbImporter = Substitute.For<IDbImporter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Json)]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeAsync<ExplorerItemBase[]>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns([.. folders, .. files]);

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			entityLoader
				.Map(
					Arg.Is<IReadOnlyCollection<FolderEntity>>(x => x.SequenceEqual(folders)),
					Arg.Is<IReadOnlyCollection<FileEntity>>(x => x.SequenceEqual(files)))
				.Returns(mapped);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(dbImporter);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result!.Imported
			.Should()
			.BeSameAs(mapped);

		Received.InOrder(() =>
		{
			dbImporter.ClearDatabaseAsync(Arg.Any<CancellationToken>());

			dbAccess.AddFoldersAsync(
				Arg.Is<IEnumerable<FolderEntity>>(x => x.SequenceEqual(folders)),
				Arg.Any<CancellationToken>());

			dbAccess.AddFilesAsync(
				Arg.Is<IEnumerable<FileEntity>>(x => x.SequenceEqual(files)),
				Arg.Any<CancellationToken>());
		});
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: a hotkey name the library no longer knows is replaced in an XML
	/// file before the file is read, so the file is not rejected because of it.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Replaces_Unknown_Hotkey_Names_Of_An_Xml_File()
	{
		// Arrange
		string? code = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Xml)]);

			IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

			dbBackupService
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			XDocument document = new(new XElement(
				"ArrayOfEntry",
				new XElement(HotkeyEntity.HotkeyElementName, new XElement(nameof(HotkeyEntity.Code), "VcNoSuchKey"))));

			IXmlSerializer serializer = Substitute.For<IXmlSerializer>();

			serializer
				.LoadDocumentAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(document);

			serializer
				.Deserialize<ExplorerItemBase[]>(Arg.Do<XDocument>(x => code = x.Descendants(nameof(HotkeyEntity.Code)).Single().Value))
				.Returns([]);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<XmlExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ImportDataAsync([]);

		// Assert
		code
			.Should()
			.Be(nameof(KeyCode.VcUndefined));
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: a file of a type no format knows fails the import, which rolls the
	/// database back.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Restores_The_Backup_For_A_File_Of_Another_Type()
	{
		// Arrange
		DatabaseBackup backup = DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>());

		IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Txt)]);

			dbBackupService
				.CreateBackupAsync()
				.Returns(backup);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(picker);

			builder
				.RegisterType<JsonExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<XmlExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<SqliteExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.BeNull();

		await dbBackupService
			.Received(1)
			.RestoreFromBackupAsync(backup.FilePath, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: a database file that is put in place but cannot be read fails
	/// the import, which rolls the database back.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Restores_The_Backup_When_A_Database_File_Cannot_Be_Read()
	{
		// Arrange
		string filePath = RandomValues.CreateFileName(10, KnownFileExtensions.Sqlite);

		DatabaseBackup backup = DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>());

		IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync(Arg.Any<FileOpenParameters>())
				.Returns([filePath]);

			dbBackupService
				.CreateBackupAsync()
				.Returns(backup);

			IDbImporter dbImporter = Substitute.For<IDbImporter>();

			dbImporter
				.IsValidSqliteDatabase(filePath)
				.Returns(true);

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			// The database in place cannot be read.
			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns((LoadedHierarchy?)null);

			builder.RegisterInstance(dbBackupService);

			builder.RegisterInstance(dbImporter);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(picker);

			builder
				.RegisterType<SqliteExchangeFormat>()
				.As<IExchangeFormat>();

			builder
				.RegisterType<BackedUpWriter>()
				.As<IBackedUpWriter>();
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.BeNull();

		Received.InOrder(() =>
		{
			dbBackupService.RestoreFromBackupAsync(filePath, Arg.Any<CancellationToken>());

			dbBackupService.RestoreFromBackupAsync(backup.FilePath, Arg.Any<CancellationToken>());
		});
	}
	#endregion
}
