using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Services.Exchange;
using Entities.Models;
using NSubstitute;
using Repository.Dto;
using Repository.Enums;
using Repository.Interfaces.Database;
using Shared.Interfaces;
using SharpHook.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TestSupport.Common;
using TestSupport.Models;

namespace DataOrganizer.UnitTests.Services.Exchange;

[TestFixture(Description = $@"Tests of ""{nameof(XmlExchangeFormat)}"" type")]
internal class XmlExchangeFormatTests
{
	#region Data
	/// <summary>
	/// The path of the exchange file.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine("exchange", "organizer.xml");
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

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.CreateSequentialWrite(FilePath)
				.Returns(stream);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(serializer);
		});

		XmlExchangeFormat sut = mock.Create<XmlExchangeFormat>();

		// Act
		await sut.ExportAsync(FilePath);

		// Assert
		serializer
			.Received(1)
			.Serialize(stream, Arg.Is<ExplorerItemBase[]>(x => x.SequenceEqual(entities)));
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

			IXmlSerializer serializer = Substitute.For<IXmlSerializer>();

			serializer
				.LoadDocumentAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
				.Returns(new XDocument(new XElement("ArrayOfEntry")));

			// The file holds no array of objects.
			serializer
				.Deserialize<ExplorerItemBase[]>(Arg.Any<XDocument>())
				.Returns(default(ExplorerItemBase[]));

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(serializer);
		});

		XmlExchangeFormat sut = mock.Create<XmlExchangeFormat>();

		// Act
		LoadedEntities? result = await sut.ReadAsync(FilePath);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SerializedExchangeFormatBase.ReadAsync" />: a hotkey name the library no longer knows is replaced before
	/// the document is read, so the file is not rejected because of it.
	/// </summary>
	[Test]
	public async Task ReadAsync_Replaces_Unknown_Hotkey_Names()
	{
		// Arrange
		string? code = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.OpenSequentialRead(FilePath)
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

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(serializer);
		});

		XmlExchangeFormat sut = mock.Create<XmlExchangeFormat>();

		// Act
		await sut.ReadAsync(FilePath);

		// Assert
		code
			.Should()
			.Be(nameof(KeyCode.VcUndefined));
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

			XDocument document = new(new XElement("ArrayOfEntry"));

			IXmlSerializer serializer = Substitute.For<IXmlSerializer>();

			serializer
				.LoadDocumentAsync(stream, Arg.Any<CancellationToken>())
				.Returns(document);

			serializer
				.Deserialize<ExplorerItemBase[]>(document)
				.Returns([files[0], folders[0], files[1], folders[1]]);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(serializer);
		});

		XmlExchangeFormat sut = mock.Create<XmlExchangeFormat>();

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
