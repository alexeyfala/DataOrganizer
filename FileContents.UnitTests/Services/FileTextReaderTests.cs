using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using EditorDocuments.Dto;
using EditorDocuments.Interfaces;
using FileContents.Dto;
using FileContents.Enums;
using FileContents.Interfaces;
using FileContents.Services;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using Shared.Common;
using Shared.Interfaces;
using Shared.Services;
using System.Text;
using System.Threading.Tasks;

namespace FileContents.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(FileTextReader)}"" type")]
internal class FileTextReaderTests
{
	#region Data
	/// <summary>
	/// Header of the password prompt.
	/// </summary>
	private const string Header = "Copy content";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="FileTextReader.ReadAsync" />: the text is read in the encoding of the contents, without the byte order
	/// mark, when the file has no choice or its editor state cannot be read.
	/// </summary>
	[TestCase(null)]
	[TestCase(@"{ ""FontSize"": 14 }")]
	[TestCase(@"{ ""Encoding"": ")]
	public async Task ReadAsync_Reads_The_Text_In_Its_Encoding(string? editorState)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(editorState: editorState);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(file, Header)
				.Returns(new FileContentsReading
				{
					Contents = [0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00],
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadAsync(file, Header);

		// Assert
		result.Text
			.Should()
			.Be("Hi");
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadAsync" />: the text is read in the encoding chosen for the file rather than in the one
	/// found from its contents, and the state the file carries spares a read of the stored one.
	/// </summary>
	[Test]
	public async Task ReadAsync_Reads_The_Text_In_The_Chosen_Encoding()
	{
		// Arrange
		const string text = "Привет, мир";

		FileDto file = ItemDtoFactory.CreateFileDto(editorState: new SystemTextJsonSerializer().Serialize(new FileEditorState
		{
			Encoding = "cp866"
		}));

		IEditorStateStore editorStateStore = Substitute.For<IEditorStateStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(file, Header)
				.Returns(new FileContentsReading
				{
					Contents = CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(text),
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(editorStateStore);

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadAsync(file, Header);

		// Assert
		result.Text
			.Should()
			.Be(text);

		await editorStateStore
			.DidNotReceiveWithAnyArgs()
			.ReadAsync(default);
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadAsync" />: the text of a file not opened in the session, which carries no editor state,
	/// is read in the encoding stored for it in the database.
	/// </summary>
	[Test]
	public async Task ReadAsync_Reads_The_Text_In_The_Stored_Encoding_Of_A_File_Not_Opened()
	{
		// Arrange
		const string text = "Привет, мир";

		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEditorStateStore editorStateStore = Substitute.For<IEditorStateStore>();

			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			editorStateStore
				.ReadAsync(file.Id)
				.Returns(new SystemTextJsonSerializer().Serialize(new FileEditorState
				{
					Encoding = "cp866"
				}));

			fileContentsReader
				.ReadAsync(file, Header)
				.Returns(new FileContentsReading
				{
					Contents = CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(text),
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(editorStateStore);

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadAsync(file, Header);

		// Assert
		result.Text
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadAsync" />: the contents opened under the header of the prompt are read as text.
	/// </summary>
	[Test]
	public async Task ReadAsync_Reads_The_Text_Opened_Under_The_Header()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(file, Header)
				.Returns(new FileContentsReading
				{
					Contents = Encoding.UTF8.GetBytes("Hi"),
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadAsync(file, Header);

		// Assert
		result
			.Should()
			.Be(new FileTextReading
			{
				Status = FileTextStatus.Text,
				Text = "Hi"
			});
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadAsync" />: a file without text is told apart from a text.
	/// </summary>
	[Test]
	public async Task ReadAsync_Reports_An_Empty_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(file, Header)
				.Returns(new FileContentsReading
				{
					Contents = [],
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadAsync(file, Header);

		// Assert
		result.Status
			.Should()
			.Be(FileTextStatus.Empty);
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadAsync" />: contents that stay closed give no text.
	/// </summary>
	[Test]
	public async Task ReadAsync_Reports_Closed_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(file, Header)
				.Returns(new FileContentsReading
				{
					Status = FileContentsStatus.Closed
				});

			builder.RegisterInstance(fileContentsReader);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadAsync(file, Header);

		// Assert
		result
			.Should()
			.Be(new FileTextReading
			{
				Status = FileTextStatus.Closed
			});
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadAsync" />: contents that are not text give no text.
	/// </summary>
	[Test]
	public async Task ReadAsync_Reports_Contents_That_Are_Not_Text()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			// The start of a PNG image
			fileContentsReader
				.ReadAsync(file, Header)
				.Returns(new FileContentsReading
				{
					Contents = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D],
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadAsync(file, Header);

		// Assert
		result
			.Should()
			.Be(new FileTextReading
			{
				Status = FileTextStatus.NotText
			});
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadAsync" />: contents that fail their check give no text.
	/// </summary>
	[Test]
	public async Task ReadAsync_Reports_Damaged_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(file, Header)
				.Returns(new FileContentsReading
				{
					Status = FileContentsStatus.Damaged
				});

			builder.RegisterInstance(fileContentsReader);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadAsync(file, Header);

		// Assert
		result
			.Should()
			.Be(new FileTextReading
			{
				Status = FileTextStatus.Damaged
			});
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadAsync" />: the decrypted contents are wiped once their text is read.
	/// </summary>
	[Test]
	public async Task ReadAsync_Zeroes_The_Decrypted_Contents(
		[Values(EncryptionStatus.Encrypted, EncryptionStatus.Decrypted)] EncryptionStatus encryptionStatus)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: encryptionStatus);

		byte[] decrypted = Encoding.UTF8.GetBytes(RandomString.Create(20));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(file, Header)
				.Returns(new FileContentsReading
				{
					Contents = decrypted,
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		await sut.ReadAsync(file, Header);

		// Assert
		decrypted
			.Should()
			.OnlyContain(static x => x == 0);
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadUnlockedAsync" />: the opened contents are read as text.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Reads_The_Text_Of_The_Opened_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadUnlockedAsync(file)
				.Returns(new FileContentsReading
				{
					Contents = Encoding.UTF8.GetBytes("Hi"),
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadUnlockedAsync(file);

		// Assert
		result
			.Should()
			.Be(new FileTextReading
			{
				Status = FileTextStatus.Text,
				Text = "Hi"
			});
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadUnlockedAsync" />: contents that stay closed give no text.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Reports_Closed_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadUnlockedAsync(file)
				.Returns(new FileContentsReading
				{
					Status = FileContentsStatus.Closed
				});

			builder.RegisterInstance(fileContentsReader);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadUnlockedAsync(file);

		// Assert
		result
			.Should()
			.Be(new FileTextReading
			{
				Status = FileTextStatus.Closed
			});
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadUnlockedAsync" />: contents that fail their check give no text.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Reports_Damaged_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadUnlockedAsync(file)
				.Returns(new FileContentsReading
				{
					Status = FileContentsStatus.Damaged
				});

			builder.RegisterInstance(fileContentsReader);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadUnlockedAsync(file);

		// Assert
		result
			.Should()
			.Be(new FileTextReading
			{
				Status = FileTextStatus.Damaged
			});
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadUnlockedAsync" />: the decrypted contents are wiped once their text is read.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Zeroes_The_Decrypted_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		byte[] decrypted = Encoding.UTF8.GetBytes(RandomString.Create(20));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadUnlockedAsync(file)
				.Returns(new FileContentsReading
				{
					Contents = decrypted,
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		await sut.ReadUnlockedAsync(file);

		// Assert
		decrypted
			.Should()
			.OnlyContain(static x => x == 0);
	}
	#endregion
}
