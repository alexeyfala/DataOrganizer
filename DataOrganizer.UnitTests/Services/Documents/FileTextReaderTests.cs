using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Documents;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Common;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Services.Documents;

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
	/// <see cref="FileTextReader.ReadAsync" />: the contents are decrypted under the header of the prompt, then read as text.
	/// </summary>
	[Test]
	public async Task ReadAsync_Decrypts_The_Contents_Under_The_Header()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

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
				.TryDecryptContentsAsync(file, stored, Header)
				.Returns(Encoding.UTF8.GetBytes("Hi"));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
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
				.TryDecryptContentsAsync(file, Arg.Any<byte[]>(), Header)
				.Returns([0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00]);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);

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
	/// found from its contents.
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
				.TryDecryptContentsAsync(file, Arg.Any<byte[]>(), Header)
				.Returns(CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(text));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);

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
	/// <see cref="FileTextReader.ReadAsync" />: a file without text is told apart from a text.
	/// </summary>
	[Test]
	public async Task ReadAsync_Reports_An_Empty_File()
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

			// Empty contents come back untouched, as the real cipher hands them back.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(file, Arg.Any<byte[]>(), Header)
				.Returns(x => x.ArgAt<byte[]>(1));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
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
	/// <see cref="FileTextReader.ReadAsync" />: contents the cipher does not open stay closed.
	/// </summary>
	[Test]
	public async Task ReadAsync_Reports_Closed_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

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

			// The password is not given.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(file, Arg.Any<byte[]>(), Header)
				.Returns((byte[]?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadAsync(file, Header);

		// Assert
		result.Status
			.Should()
			.Be(FileTextStatus.Closed);
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
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id)
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(8),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			// The start of a PNG image
			contentCipher
				.TryDecryptContentsAsync(file, Arg.Any<byte[]>(), Header)
				.Returns([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D]);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
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
	/// <see cref="FileTextReader.ReadAsync" />: contents that fail their check are never decrypted.
	/// </summary>
	[Test]
	public async Task ReadAsync_Reports_Damaged_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

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

			builder.RegisterInstance(contentCipher);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadAsync(file, Header);

		// Assert
		result.Status
			.Should()
			.Be(FileTextStatus.Damaged);

		await contentCipher
			.DidNotReceive()
			.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
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
				.TryDecryptContentsAsync(file, Arg.Any<byte[]>(), Header)
				.Returns(decrypted);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
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
	/// <see cref="FileTextReader.ReadUnlockedAsync" />: the contents under an unlocked keeper are decrypted with the key of
	/// the session, then read as text.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Decrypts_The_Contents_Under_An_Unlocked_Keeper()
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

			builder.RegisterInstance(contentCipher);
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
	/// <see cref="FileTextReader.ReadUnlockedAsync" />: the contents under a locked keeper stay closed, and no password is
	/// asked for.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Keeps_A_Locked_File_Closed()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id)
				.Returns(new ValidatedContents
				{
					Contents = Encoding.UTF8.GetBytes("Hi"),
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadUnlockedAsync(file);

		// Assert
		result.Status
			.Should()
			.Be(FileTextStatus.Closed);

		await contentCipher
			.DidNotReceive()
			.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadUnlockedAsync" />: contents the key of the session cannot open stay closed instead of
	/// throwing.
	/// </summary>
	[Test]
	[TestCaseSource(nameof(SessionCipherFailures))]
	public async Task ReadUnlockedAsync_Keeps_The_File_Closed_When_The_Decryption_Fails(Exception failure)
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

			builder.RegisterInstance(contentCipher);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadUnlockedAsync(file);

		// Assert
		result.Status
			.Should()
			.Be(FileTextStatus.Closed);
	}

	/// <summary>
	/// <see cref="FileTextReader.ReadUnlockedAsync" />: the contents of a plain file are read as they are stored.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Reads_The_Text_Of_A_Plain_File()
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
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);
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
	/// <see cref="FileTextReader.ReadUnlockedAsync" />: contents that fail their check are never decrypted.
	/// </summary>
	[Test]
	public async Task ReadUnlockedAsync_Reports_Damaged_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(file.Id)
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(8),
					IsValid = false
				});

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
		});

		FileTextReader sut = mock.Create<FileTextReader>();

		// Act
		FileTextReading result = await sut.ReadUnlockedAsync(file);

		// Assert
		result.Status
			.Should()
			.Be(FileTextStatus.Damaged);

		contentCipher
			.DidNotReceive()
			.Decrypt(Arg.Any<FileDto>(), Arg.Any<byte[]>());
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

			builder.RegisterInstance(contentCipher);
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
