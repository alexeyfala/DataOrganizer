using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Platform.Storage;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.Windows;
using Entities.Models;
using NSubstitute;
using Repository.DTO;
using Repository.Interfaces;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DataExchangeService)}"" type")]
internal class DataExchangeServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="DataExchangeService.AppendFromSQLiteAsync" />: appends entities from a SQLite source and maps them via the entity loader.
	/// </summary>
	[Test]
	public async Task AppendFromSQLiteAsync_Does_Work()
	{
		// Arrange
		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.LoadFromDb(Arg.Any<string>())
				.Returns(new LoadFromDbResult
				{
					Files = [.. TestUtils.CreateFiles(5)],
					Folders = [.. TestUtils.CreateFolders(5)]
				});

			dbAccess
				.AddFoldersAsync(Arg.Any<IEnumerable<FolderModel>>())
				.Returns(true);

			dbAccess
				.AddFilesAsync(Arg.Any<IEnumerable<FileModel>>())
				.Returns(true);

			builder.RegisterInstance(entityLoader);

			builder.RegisterInstance(dbAccess);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		bool result = await sut.AppendFromSQLiteAsync(
			string.Empty,
			[],
			[]);

		// Assert
		result
			.Should()
			.BeTrue();

		entityLoader
			.Received()
			.Map(Arg.Any<IEnumerable<FolderModel>>(), Arg.Any<IEnumerable<FileModel>>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ExportDataAsync" />: serializes data to a JSON file via the serializer.
	/// </summary>
	[Test]
	public async Task ExportDataAsync_Exports_To_Json()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			fileSystem
				.CreateSequentialWrite(Arg.Any<string>())
				.Returns(new MemoryStream());

			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SaveFileAsync<EditorWindow>(Arg.Any<FilePickerSaveOptions>())
				.Returns(TestUtils.CreateRandomFileName(10, DataExchangeService.JsonExt));

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
			Arg.Any<ExplorerModelBase[]>(),
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
				.Returns(TestUtils.CreateRandomFileName(10, AppUtils.SQLiteExtension));

			builder.RegisterInstance(picker);

			builder.RegisterInstance(dbAccess);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		await sut.ExportDataAsync();

		// Assert
		await dbAccess
			.Received()
			.BackupSqliteDatabaseAsync(Arg.Any<BackupSqliteParameters>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ExportDataAsync" />: serializes data to an XML file via the serializer.
	/// </summary>
	[Test]
	public async Task ExportDataAsync_Exports_To_Xml()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IXmlSerializerWrapper serializer = Substitute.For<IXmlSerializerWrapper>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			fileSystem
				.CreateSequentialWrite(Arg.Any<string>())
				.Returns(new MemoryStream());

			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SaveFileAsync<EditorWindow>(Arg.Any<FilePickerSaveOptions>())
				.Returns(TestUtils.CreateRandomFileName(10, DataExchangeService.XmlExt));

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
			.Serialize(Arg.Any<Stream>(), Arg.Any<ExplorerModelBase[]>());
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
				.Returns([TestUtils.CreateRandomFileName(10, DataExchangeService.JsonExt)]);

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateRandomFileName(10));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.DeserializeAsync<ExplorerModelBase[]>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(default(ExplorerModelBase[]));

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
				.Returns([TestUtils.CreateRandomFileName(10, AppUtils.SQLiteExtension)]);

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateRandomFileName(10));

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
				.Returns([TestUtils.CreateRandomFileName(10, DataExchangeService.XmlExt)]);

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateRandomFileName(10));

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IXmlSerializerWrapper serializer = Substitute.For<IXmlSerializerWrapper>();

			serializer
				.Deserialize<ExplorerModelBase[]>(Arg.Any<Stream>())
				.Returns(default(ExplorerModelBase[]));

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
				.Returns([TestUtils.CreateRandomFileName(10, DataExchangeService.JsonExt)]);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateRandomFileName(10));

			dbAccess
				.ClearDatabaseAsync()
				.Returns(true);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

#pragma warning disable CA2012 // Use ValueTasks correctly
			serializer
				.DeserializeAsync<ExplorerModelBase[]>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(new ValueTask<ExplorerModelBase[]?>([]));
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
				.Returns([TestUtils.CreateRandomFileName(10, AppUtils.SQLiteExtension)]);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateRandomFileName(10));

			dbAccess
				.IsValidSQLiteDatabase(Arg.Any<string>())
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
				.Returns([TestUtils.CreateRandomFileName(10, DataExchangeService.XmlExt)]);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.BackupDatabaseAsync()
				.Returns(TestUtils.CreateRandomFileName(10));

			dbAccess
				.ClearDatabaseAsync()
				.Returns(true);

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(Arg.Any<string>())
				.Returns(new MemoryStream());

			IXmlSerializerWrapper serializer = Substitute.For<IXmlSerializerWrapper>();

			serializer
				.Deserialize<ExplorerModelBase[]>(Arg.Any<Stream>())
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
	[TestCase(ImportListVariant.Append)]
	[TestCase(ImportListVariant.Replace)]
	public async Task ImportEntitiesAsync_Does_Work(ImportListVariant variant)
	{
		// Arrange
		ExplorerModelBase[] entities = [.. TestUtils
			.CreateFolders(5)
			.Concat<ExplorerModelBase>(TestUtils.CreateFiles(5))];

		entities.ForEach(x => x.CreatedDate = x.UpdatedDate = default);

		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			if (variant == ImportListVariant.Replace)
			{
				dbAccess
					.ClearDatabaseAsync()
					.Returns(true);
			}

			dbAccess
				.AddFoldersAsync(Arg.Any<IEnumerable<FolderModel>>())
				.Returns(true);

			dbAccess
				.AddFilesAsync(Arg.Any<IEnumerable<FileModel>>())
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
			.NotContain(x => x.CreatedDate == default || x.UpdatedDate == default);

		entityLoader
			.Received()
			.Map(Arg.Any<IEnumerable<FolderModel>>(), Arg.Any<IEnumerable<FileModel>>());
	}

	/// <summary>
	/// <see cref="DataExchangeService.ReplaceFromSQLiteAsync" />: replaces data from an embedded SQLite source, clearing the hierarchy.
	/// </summary>
	[Test]
	public async Task ReplaceFromSQLiteAsync_Does_Work()
	{
		// Arrange
		Collection<ExplorerModelBaseDto> hierarchy = [.. TestUtils
			.CreateFoldersDto(5)
			.Concat<ExplorerModelBaseDto>(TestUtils.CreateFilesDto(5))];

		IEntityLoader entityLoader = Substitute.For<IEntityLoader>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.RestoreFromBackupAsync(Arg.Any<string>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(entityLoader);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		bool result = await sut.ReplaceFromSQLiteAsync(
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
			.LoadFromEmbeddedDbAsync();
	}
	#endregion
}
