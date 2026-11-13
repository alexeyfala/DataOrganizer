using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Database.Entities.Models;
using DataOrganizer.Database.Repository.Dto;
using DataOrganizer.Database.Repository.Enums;
using DataOrganizer.Database.Repository.Interfaces.Database;
using DataOrganizer.Shared.Common;
using DataOrganizer.Shared.Interfaces;
using DataOrganizer.TestSupport.Common;
using DataOrganizer.TestSupport.Models;
using DataOrganizer.Workflows.HierarchyExchange.Services;
using NSubstitute;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Testably.Abstractions.Testing;

namespace DataOrganizer.Workflows.UnitTests.HierarchyExchange.Services;

[TestFixture(Description = $@"Tests of ""{nameof(JsonExchangeFormat)}"" type")]
internal class JsonExchangeFormatTests
{
	#region Data
	/// <summary>
	/// The path of the exchange file.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine("exchange", "organizer.json");

	/// <summary>
	/// Folder the exchange file lives in.
	/// </summary>
	private static readonly string Folder = TestPath.Combine("exchange");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SerializedExchangeFormatBase.ExportAsync" />: every folder and file of the database, with contents and
	/// editor state, is serialized into the file.
	/// </summary>
	[Test]
	public async Task ExportAsync_Writes_Every_Object_Of_The_Database()
	{
		// Arrange
		FolderEntity[] folders = [.. EntityFactory.CreateFolders(2)];

		FileEntity[] files = [.. EntityFactory.CreateFiles(2)];

		ExplorerItemBase[] entities = [.. folders, .. files];

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

			MockFileSystem fileSystem = new();

			fileSystem
				.Directory
				.CreateDirectory(Folder);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance<IFileSystem>(fileSystem);

			builder.RegisterInstance(serializer);
		});

		JsonExchangeFormat sut = mock.Create<JsonExchangeFormat>();

		// Act
		await sut.ExportAsync(FilePath);

		// Assert
		await serializer.Received(1).SerializeAsync(
			Arg.Is<Stream>(x => ((FileSystemStream)x).Name == FilePath),
			Arg.Is<ExplorerItemBase[]>(x => x.SequenceEqual(entities)),
			JsonDefaults.Options,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="SerializedExchangeFormatBase.ReadAsync" />: a file that holds no objects reads as <c>null</c>.
	/// </summary>
	[Test]
	public async Task ReadAsync_Gives_Null_For_A_File_Without_Objects()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem fileSystem = new();

			fileSystem
				.Directory
				.CreateDirectory(Folder);

			fileSystem
				.File
				.WriteAllBytes(FilePath, []);

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			// The file holds no array of objects.
			serializer
				.DeserializeAsync<ExplorerItemBase[]>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(default(ExplorerItemBase[]));

			builder.RegisterInstance<IFileSystem>(fileSystem);

			builder.RegisterInstance(serializer);
		});

		JsonExchangeFormat sut = mock.Create<JsonExchangeFormat>();

		// Act
		LoadedEntities? result = await sut.ReadAsync(FilePath);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SerializedExchangeFormatBase.ReadAsync" />: the objects of the file come back as folders and files apart,
	/// each in the order of the file.
	/// </summary>
	[Test]
	public async Task ReadAsync_Splits_The_Objects_Into_Folders_And_Files()
	{
		// Arrange
		FolderEntity[] folders = [.. EntityFactory.CreateFolders(2)];

		FileEntity[] files = [.. EntityFactory.CreateFiles(2)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem fileSystem = new();

			fileSystem
				.Directory
				.CreateDirectory(Folder);

			fileSystem
				.File
				.WriteAllBytes(FilePath, []);

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeAsync<ExplorerItemBase[]>(Arg.Is<Stream>(x => ((FileSystemStream)x).Name == FilePath), Arg.Any<CancellationToken>())
				.Returns([files[0], folders[0], files[1], folders[1]]);

			builder.RegisterInstance<IFileSystem>(fileSystem);

			builder.RegisterInstance(serializer);
		});

		JsonExchangeFormat sut = mock.Create<JsonExchangeFormat>();

		// Act
		LoadedEntities? result = await sut.ReadAsync(FilePath);

		// Assert
		result!.Folders
			.Should()
			.Equal(folders);

		result.Files
			.Should()
			.Equal(files);
	}
	#endregion
}
