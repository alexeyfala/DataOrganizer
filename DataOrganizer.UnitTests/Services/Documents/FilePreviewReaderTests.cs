using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Documents;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Services.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(FilePreviewReader)}"" type")]
internal class FilePreviewReaderTests
{
	#region Methods
	/// <summary>
	/// <see cref="FilePreviewReader.ReadPreviewAsync" />: a file without text gives no preview.
	/// </summary>
	[Test]
	public async Task ReadPreviewAsync_Gives_No_Preview_For_An_Empty_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id)
				.Returns(new ValidatedContents
				{
					Contents = [],
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();
		});

		FilePreviewReader sut = mock.Create<FilePreviewReader>();

		// Act
		string? preview = await sut.ReadPreviewAsync(file);

		// Assert
		preview
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FilePreviewReader.ReadPreviewAsync" />: contents that are not text give no preview.
	/// </summary>
	[Test]
	public async Task ReadPreviewAsync_Gives_No_Preview_For_Contents_That_Are_Not_Text()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id)
				.Returns(new ValidatedContents
				{
					// The start of a PNG image
					Contents = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D],
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();
		});

		FilePreviewReader sut = mock.Create<FilePreviewReader>();

		// Act
		string? preview = await sut.ReadPreviewAsync(file);

		// Assert
		preview
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FilePreviewReader.ReadPreviewAsync" />: contents that fail their check give no preview.
	/// </summary>
	[Test]
	public async Task ReadPreviewAsync_Gives_No_Preview_For_Damaged_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id)
				.Returns(new ValidatedContents
				{
					Contents = Encoding.UTF8.GetBytes("Hi"),
					IsValid = false
				});

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();
		});

		FilePreviewReader sut = mock.Create<FilePreviewReader>();

		// Act
		string? preview = await sut.ReadPreviewAsync(file);

		// Assert
		preview
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FilePreviewReader.ReadPreviewAsync" />: contents the session key cannot open give no preview.
	/// </summary>
	[Test]
	[TestCaseSource(nameof(SessionCipherFailures))]
	public async Task ReadPreviewAsync_Gives_No_Preview_When_The_Decryption_Fails(Exception failure)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id)
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(8),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.Decrypt(file, Arg.Any<byte[]>())
				.Throws(failure);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder.RegisterInstance(contentCipher);
		});

		FilePreviewReader sut = mock.Create<FilePreviewReader>();

		// Act
		string? preview = await sut.ReadPreviewAsync(file);

		// Assert
		preview
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FilePreviewReader.ReadPreviewAsync" />: a read that fails gives no preview.
	/// </summary>
	[Test]
	public async Task ReadPreviewAsync_Gives_No_Preview_When_The_Read_Fails()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileTextReader fileTextReader = Substitute.For<IFileTextReader>();

			fileTextReader
				.ReadUnlockedAsync(file, Arg.Any<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(fileTextReader);
		});

		FilePreviewReader sut = mock.Create<FilePreviewReader>();

		// Act
		string? preview = await sut.ReadPreviewAsync(file);

		// Assert
		preview
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FilePreviewReader.ReadPreviewAsync" />: the preview of a file under an unlocked keeper is its
	/// decrypted text.
	/// </summary>
	[Test]
	public async Task ReadPreviewAsync_Reads_The_Decrypted_Text()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] stored = RandomValues.CreateBytes(8);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id)
				.Returns(new ValidatedContents
				{
					Contents = stored,
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.Decrypt(file, stored)
				.Returns(Encoding.UTF8.GetBytes("Hi"));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder.RegisterInstance(contentCipher);
		});

		FilePreviewReader sut = mock.Create<FilePreviewReader>();

		// Act
		string? preview = await sut.ReadPreviewAsync(file);

		// Assert
		preview
			.Should()
			.Be("Hi");
	}

	/// <summary>
	/// <see cref="FilePreviewReader.ReadPreviewAsync" />: the preview of a long text is its start with a mark
	/// of the cut.
	/// </summary>
	[Test]
	public async Task ReadPreviewAsync_Reads_The_Start_Of_A_Long_Text()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id)
				.Returns(new ValidatedContents
				{
					Contents = Encoding.UTF8.GetBytes(new string('a', 300)),
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();
		});

		FilePreviewReader sut = mock.Create<FilePreviewReader>();

		// Act
		string? preview = await sut.ReadPreviewAsync(file);

		// Assert
		preview
			.Should()
			.Be($"{new string('a', 200)}...");
	}

	/// <summary>
	/// <see cref="FilePreviewReader.ReadPreviewAsync" />: the preview is the text in the encoding of the file,
	/// without its byte order mark.
	/// </summary>
	[Test]
	[TestCase(new byte[] { 0xEF, 0xBB, 0xBF, 0x48, 0x69 })]
	[TestCase(new byte[] { 0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00 })]
	public async Task ReadPreviewAsync_Reads_The_Text_In_Its_Encoding(byte[] contents)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id)
				.Returns(new ValidatedContents
				{
					Contents = contents,
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();
		});

		FilePreviewReader sut = mock.Create<FilePreviewReader>();

		// Act
		string? preview = await sut.ReadPreviewAsync(file);

		// Assert
		preview
			.Should()
			.Be("Hi");
	}

	/// <summary>
	/// <see cref="FilePreviewReader.ReadPreviewAsync" />: the preview is the text in the encoding chosen for
	/// the file rather than in the one found from its contents.
	/// </summary>
	[Test]
	public async Task ReadPreviewAsync_Reads_The_Text_In_The_Chosen_Encoding()
	{
		// Arrange
		const string text = "Привет, мир";

		FileDto file = ItemDtoFactory.CreateFileDto(editorState: new SystemTextJsonSerializer().Serialize(new FileEditorState
		{
			Encoding = "cp866"
		}));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id)
				.Returns(new ValidatedContents
				{
					Contents = CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(text),
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		FilePreviewReader sut = mock.Create<FilePreviewReader>();

		// Act
		string? preview = await sut.ReadPreviewAsync(file);

		// Assert
		preview
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="FilePreviewReader.ReadPreviewAsync" />: the decrypted contents are wiped once their text
	/// is read.
	/// </summary>
	[Test]
	public async Task ReadPreviewAsync_Zeroes_The_Decrypted_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		byte[] decrypted = Encoding.UTF8.GetBytes("Hi");

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id)
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(8),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.Decrypt(file, Arg.Any<byte[]>())
				.Returns(decrypted);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder.RegisterInstance(contentCipher);
		});

		FilePreviewReader sut = mock.Create<FilePreviewReader>();

		// Act
		string? preview = await sut.ReadPreviewAsync(file);

		// Assert
		decrypted
			.Should()
			.OnlyContain(static x => x == 0);
	}

	#endregion

	#region Helpers
	/// <summary>
	/// Failures an operation on the key of a session can end with.
	/// </summary>
	private static Exception[] SessionCipherFailures() =>
	[
		new AuthenticationTagMismatchException(),
		new InvalidOperationException()
	];
	#endregion
}
