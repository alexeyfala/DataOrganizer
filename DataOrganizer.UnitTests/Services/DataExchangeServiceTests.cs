using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Platform.Storage;
using AwesomeAssertions;
using DataOrganizer.Dto;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Services;
using DataOrganizer.Windows;
using Entities.Models;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TestSupport.Common;
using TestSupport.Database;
using TestSupport.Dto;
using TestSupport.Models;

namespace DataOrganizer.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(DataExchangeService)}"" type")]
internal class DataExchangeServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="DataExchangeService.AppendFromSqliteAsync" />: appends entities from a SQLite source and maps them via the entity loader.
	/// </summary>
	[Test]
	public async Task AppendFromSqliteAsync_Maps_The_Appended_Entities()
	{
		// Arrange
		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.LoadEntities(Arg.Any<string>())
				.Returns(new LoadedEntities
				{
					Files = [.. EntityFactory.CreateFiles(5)],
					Folders = [.. EntityFactory.CreateFolders(5)]
				});

			dbAccess
				.AddFoldersAsync(Arg.Any<IEnumerable<FolderEntity>>())
				.Returns(true);

			dbAccess
				.AddFilesAsync(Arg.Any<IEnumerable<FileEntity>>())
				.Returns(true);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(dbAccess);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		bool result = await sut.AppendFromSqliteAsync(
			string.Empty,
			[],
			[]);

		// Assert
		result
			.Should()
			.BeTrue();

		entityLoader
			.Received()
			.Map(Arg.Any<IEnumerable<FolderEntity>>(), Arg.Any<IEnumerable<FileEntity>>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ExportDataAsync" />: serializes data to a JSON file via the serializer.
	/// </summary>
	[Test]
	public async Task ExportDataAsync_Exports_To_Json()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			fileSystem
				.CreateSequentialWrite(Arg.Any<string>())
				.Returns(new MemoryStream());

			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SaveFileAsync<EditorWindow>(Arg.Any<FilePickerSaveOptions>())
				.Returns(RandomValues.CreateFileName(10, KnownFileExtensions.Json));

			builder.RegisterInstance(picker);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(serializer);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ExportDataAsync();

		// Assert
		fileSystem
			.Received()
			.CreateSequentialWrite(Arg.Any<string>());

		await serializer.Received().SerializeAsync(
			Arg.Any<Stream>(),
			Arg.Any<ExplorerItemBase[]>(),
			Arg.Any<JsonSerializerOptions>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ExportDataAsync" />: backs up the database when exporting to a SQLite file.
	/// </summary>
	[Test]
	public async Task ExportDataAsync_Exports_To_Sqlite()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SaveFileAsync<EditorWindow>(Arg.Any<FilePickerSaveOptions>())
				.Returns(RandomValues.CreateFileName(10, KnownFileExtensions.Sqlite));

			builder.RegisterInstance(picker);

			builder.RegisterInstance(dbAccess);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ExportDataAsync();

		// Assert
		await dbAccess
			.Received()
			.CopyDatabaseAsync(Arg.Any<CopyDatabaseParameters>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ExportDataAsync" />: serializes data to an XML file via the serializer.
	/// </summary>
	[Test]
	public async Task ExportDataAsync_Exports_To_Xml()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IXmlSerializer serializer = Substitute.For<IXmlSerializer>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			fileSystem
				.CreateSequentialWrite(Arg.Any<string>())
				.Returns(new MemoryStream());

			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SaveFileAsync<EditorWindow>(Arg.Any<FilePickerSaveOptions>())
				.Returns(RandomValues.CreateFileName(10, KnownFileExtensions.Xml));

			builder.RegisterInstance(picker);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(serializer);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ExportDataAsync();

		// Assert
		fileSystem
			.Received()
			.CreateSequentialWrite(Arg.Any<string>());

		serializer
			.Received()
			.Serialize(Arg.Any<Stream>(), Arg.Any<ExplorerItemBase[]>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: returns null and restores the backup when JSON deserialization yields no data.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Cannot_Import_From_Json()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync<EditorWindow>(Arg.Any<FilePickerOpenOptions>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Json)]);

			dbAccess
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

			builder.RegisterInstance(dbAccess);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.BeNull();

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: returns null and restores the backup when the SQLite source is invalid.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Cannot_Import_From_SQLite()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync<EditorWindow>(Arg.Any<FilePickerOpenOptions>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Sqlite)]);

			dbAccess
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			builder.RegisterInstance(picker);

			builder.RegisterInstance(dbAccess);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.BeNull();

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ImportDataAsync" />: returns null and restores the backup when XML deserialization yields no data.
	/// </summary>
	[Test]
	public async Task ImportDataAsync_Cannot_Import_From_Xml()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SelectFilesAsync<EditorWindow>(Arg.Any<FilePickerOpenOptions>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Xml)]);

			dbAccess
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

			builder.RegisterInstance(dbAccess);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.BeNull();

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());
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
				.SelectFilesAsync<EditorWindow>(Arg.Any<FilePickerOpenOptions>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Sqlite)]);

			dbAccess
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			dbAccess
				.DeleteHotkeysAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			dbAccess
				.IsValidSqliteDatabase(Arg.Any<string>())
				.Returns(true);

			dbAccess
				.RestoreFromBackupAsync(Arg.Any<string>())
				.Returns(true);

			IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns([file]);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(picker);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		ImportDataResult? result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.NotBeNull();

		result
			.ImportedItems
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
				.SelectFilesAsync<EditorWindow>(Arg.Any<FilePickerOpenOptions>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Json)]);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			dbAccess
				.ClearDatabaseAsync()
				.Returns(true);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

#pragma warning disable CA2012 // Use ValueTasks correctly
			serializer
				.DeserializeAsync<ExplorerItemBase[]>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(new ValueTask<ExplorerItemBase[]?>([]));
#pragma warning restore CA2012 // Use ValueTasks correctly

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);
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
				.SelectFilesAsync<EditorWindow>(Arg.Any<FilePickerOpenOptions>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Sqlite)]);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			dbAccess
				.IsValidSqliteDatabase(Arg.Any<string>())
				.Returns(true);

			dbAccess
				.RestoreFromBackupAsync(Arg.Any<string>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(picker);
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
				.SelectFilesAsync<EditorWindow>(Arg.Any<FilePickerOpenOptions>())
				.Returns([RandomValues.CreateFileName(10, KnownFileExtensions.Xml)]);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.CreateBackupAsync()
				.Returns(DatabaseFactory.CreateDatabaseBackup(Substitute.For<IFileSystem>()));

			dbAccess
				.ClearDatabaseAsync()
				.Returns(true);

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

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);
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
	/// <see cref="DataExchangeService.ImportEntitiesAsync" />: imports entities, stamps their dates, and maps them via the entity loader for both append and replace variants.
	/// </summary>
	[TestCase(ImportMode.Append)]
	[TestCase(ImportMode.Replace)]
	public async Task ImportEntitiesAsync_Stamps_Dates_And_Maps_Entities(ImportMode variant)
	{
		// Arrange
		ExplorerItemBase[] entities = [.. EntityFactory
			.CreateFolders(5)
			.Concat<ExplorerItemBase>(EntityFactory.CreateFiles(5))];

		entities.ForEach(x => x.CreatedAt = x.UpdatedAt = default);

		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			if (variant == ImportMode.Replace)
			{
				dbAccess
					.ClearDatabaseAsync()
					.Returns(true);
			}

			dbAccess
				.AddFoldersAsync(Arg.Any<IEnumerable<FolderEntity>>())
				.Returns(true);

			dbAccess
				.AddFilesAsync(Arg.Any<IEnumerable<FileEntity>>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(entityLoader);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		bool result = await sut.ImportEntitiesAsync(
			entities,
			variant,
			[],
			[]);

		// Assert
		result
			.Should()
			.BeTrue();

		entities
			.Should()
			.NotContain(x => x.CreatedAt == default || x.UpdatedAt == default);

		entityLoader
			.Received()
			.Map(Arg.Any<IEnumerable<FolderEntity>>(), Arg.Any<IEnumerable<FileEntity>>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ReplaceFromSqliteAsync" />: replaces data from an embedded SQLite source, clearing the hierarchy.
	/// </summary>
	[Test]
	public async Task ReplaceFromSqliteAsync_Clears_The_Hierarchy_And_Reloads_It()
	{
		// Arrange
		Collection<ExplorerItemDtoBase> hierarchy = [.. ItemDtoFactory
			.CreateFolderDtos(5)
			.Concat<ExplorerItemDtoBase>(ItemDtoFactory.CreateFileDtos(5))];

		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns([]);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.RestoreFromBackupAsync(Arg.Any<string>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(entityLoader);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		bool result = await sut.ReplaceFromSqliteAsync(
			string.Empty,
			[],
			hierarchy);

		// Assert
		result
			.Should()
			.BeTrue();

		hierarchy
			.Should()
			.BeEmpty();

		await entityLoader
			.Received()
			.LoadHierarchyAsync();
	}

	/// <summary>
	/// <see cref="DataExchangeService.ReplaceFromSqliteAsync" />: a database that is in place but cannot
	/// be read is reported as a failed import, so the copy taken before it is restored.
	/// </summary>
	[Test]
	public async Task ReplaceFromSqliteAsync_Fails_When_The_Database_Cannot_Be_Read()
	{
		// Arrange
		Collection<ExplorerItemDtoBase> hierarchy = [.. ItemDtoFactory.CreateFolderDtos(5)];

		List<ExplorerItemDtoBase> objects = [];

		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			entityLoader
				.LoadHierarchyAsync(Arg.Any<CancellationToken>())
				.Returns((ExplorerItemDtoBase[]?)null);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.RestoreFromBackupAsync(Arg.Any<string>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(entityLoader);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		bool result = await sut.ReplaceFromSqliteAsync(
			string.Empty,
			objects,
			hierarchy);

		// Assert
		result
			.Should()
			.BeFalse();

		objects
			.Should()
			.BeEmpty();

		hierarchy
			.Should()
			.HaveCount(5);
	}
	#endregion
}
