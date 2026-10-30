using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Entities.Models;
using HierarchyExchange.Services;
using NSubstitute;
using Repository.Dto;
using Repository.Enums;
using Repository.Interfaces.Database;
using Shared.Common;
using Shared.Interfaces;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Models;

namespace HierarchyExchange.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(JsonExchangeFormat)}"" type")]
internal class JsonExchangeFormatTests
{
	#region Data
	/// <summary>
	/// The path of the exchange file.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine("exchange", "organizer.json");
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

		using MemoryStream stream = new();

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

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.CreateSequentialWrite(FilePath)
				.Returns(stream);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(serializer);
		});

		JsonExchangeFormat sut = mock.Create<JsonExchangeFormat>();

		// Act
		await sut.ExportAsync(FilePath);

		// Assert
		await serializer.Received(1).SerializeAsync(
			stream,
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
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(FilePath)
				.Returns(new MemoryStream());

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			// The file holds no array of objects.
			serializer
				.DeserializeAsync<ExplorerItemBase[]>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(default(ExplorerItemBase[]));

			builder.RegisterInstance(fileSystem);

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
			MemoryStream stream = new();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(FilePath)
				.Returns(stream);

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeAsync<ExplorerItemBase[]>(stream, Arg.Any<CancellationToken>())
				.Returns([files[0], folders[0], files[1], folders[1]]);

			builder.RegisterInstance(fileSystem);

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
