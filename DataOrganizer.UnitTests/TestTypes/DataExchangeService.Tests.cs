using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Platform.Storage;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.Windows;
using Entities.Abstract;
using NSubstitute;
using Repository.DTO;
using Repository.Interfaces;
using Shared.Common;
using Shared.Interfaces;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DataExchangeService)}"" type")]
internal class DataExchangeServiceTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="DataExchangeService.ExportDataAsync" />.
	/// </summary>
	[Test]
	public async Task ExportDataAsync_Exports_To_Json()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SaveFileAsync<EditorWindow>(Arg.Any<FilePickerSaveOptions>())
				.Returns(TestUtils.CreateRandomFileName(10, IFileSystemPicker.JsonExt));

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
			.WriteAllText(Arg.Any<string>(), Arg.Any<string>());

		serializer
			.Received()
			.Serialize(Arg.Any<ExplorerModelBase[]>(), Arg.Any<JsonSerializerOptions>());
	}

	/// <summary>
	/// Test of <see cref="DataExchangeService.ExportDataAsync" />.
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
		dbAccess
			.Received()
			.BackupSqliteDatabase(Arg.Any<BackupSqliteParameters>());
	}

	/// <summary>
	/// Test of <see cref="DataExchangeService.ExportDataAsync" />.
	/// </summary>
	[Test]
	public async Task ExportDataAsync_Exports_To_Xml()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		IXmlSerializerWrapper serializer = Substitute.For<IXmlSerializerWrapper>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystemPicker picker = Substitute.For<IFileSystemPicker>();

			picker
				.SaveFileAsync<EditorWindow>(Arg.Any<FilePickerSaveOptions>())
				.Returns(TestUtils.CreateRandomFileName(10, IFileSystemPicker.XmlExt));

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
			.WriteAllText(Arg.Any<string>(), Arg.Any<string>());

		serializer
			.Received()
			.Serialize(Arg.Any<ExplorerModelBase[]>());
	}

	/// <summary>
	/// Test of <see cref="DataExchangeService.ImportDataAsync" />.
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
				.Returns([TestUtils.CreateRandomFileName(10, IFileSystemPicker.JsonExt)]);

			dbAccess
				.BackupDatabase()
				.Returns(AppUtils.CreateRandomFileName(10));

			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Deserialize<ExplorerModelBase[]>(Arg.Any<string>())
				.Returns(default(ExplorerModelBase[]));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(dbAccess);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		bool result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.BeFalse();

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());
	}

	/// <summary>
	/// Test of <see cref="DataExchangeService.ImportDataAsync" />.
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
				.BackupDatabase()
				.Returns(AppUtils.CreateRandomFileName(10));

			builder.RegisterInstance(picker);

			builder.RegisterInstance(dbAccess);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		bool result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.BeFalse();

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());
	}

	/// <summary>
	/// Test of <see cref="DataExchangeService.ImportDataAsync" />.
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
				.Returns([TestUtils.CreateRandomFileName(10, IFileSystemPicker.XmlExt)]);

			dbAccess
				.BackupDatabase()
				.Returns(AppUtils.CreateRandomFileName(10));

			IXmlSerializerWrapper serializer = Substitute.For<IXmlSerializerWrapper>();

			serializer
				.Deserialize<ExplorerModelBase[]>(Arg.Any<string>())
				.Returns(default(ExplorerModelBase[]));

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(dbAccess);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		bool result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.BeFalse();

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());
	}

	/// <summary>
	/// Test of <see cref="DataExchangeService.ImportDataAsync" />.
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
				.Returns([TestUtils.CreateRandomFileName(10, IFileSystemPicker.JsonExt)]);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.BackupDatabase()
				.Returns(AppUtils.CreateRandomFileName(10));

			dbAccess
				.ClearDatabase()
				.Returns(true);

			IJsonSerializerWrapper serializer = Substitute.For<IJsonSerializerWrapper>();

			serializer
				.Deserialize<ExplorerModelBase[]>(Arg.Any<string>())
				.Returns([]);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(picker);

			builder.RegisterInstance(serializer);
		});

		DataExchangeService sut = mock.Create<DataExchangeService>();

		// Act
		bool result = await sut.ImportDataAsync([]);

		// Assert
		result
			.Should()
			.BeTrue();
	}
	#endregion
}
