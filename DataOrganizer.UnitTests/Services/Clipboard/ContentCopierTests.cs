using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Documents;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using Repository.Interfaces.Database;
using Shared.Common;
using Shared.Interfaces;
using Shared.Services;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ContentCopier)}"" type")]
internal class ContentCopierTests
{
	#region Methods
	/// <summary>
	/// <see cref="ContentCopier.CopyAsync" />: a file that is no longer in the database is neither read nor put into the
	/// copy history.
	/// </summary>
	[Test]
	public async Task CopyAsync_Copies_Nothing_From_A_File_That_No_Longer_Exists()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		ICopyHistoryRecorder copyHistory = Substitute.For<ICopyHistoryRecorder>();

		IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			// The file left the database.
			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(copyHistory);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);
		});

		ContentCopier sut = mock.Create<ContentCopier>();

		// Act
		bool result = await sut.CopyAsync(file, showAtOnce: false);

		// Assert
		result
			.Should()
			.BeFalse();

		await fileTextCopier
			.DidNotReceive()
			.CopyAsync(Arg.Any<FileDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

		copyHistory
			.DidNotReceive()
			.Add(Arg.Any<FileDto>(), Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ContentCopier.CopyAsync" />: a file without text puts nothing on the clipboard.
	/// </summary>
	[Test]
	public async Task CopyAsync_Copies_Nothing_From_An_Empty_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(file, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(new FileContentsReading
				{
					Contents = [],
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(fileContentsReader);
		});

		ContentCopier sut = mock.Create<ContentCopier>();

		// Act
		await sut.CopyAsync(file, showAtOnce: false);

		// Assert
		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ContentCopier.CopyAsync" />: contents that fail their check never reach the clipboard.
	/// </summary>
	[Test]
	public async Task CopyAsync_Copies_Nothing_From_Damaged_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(file, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(new FileContentsReading
				{
					Status = FileContentsStatus.Damaged
				});

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(fileContentsReader);
		});

		ContentCopier sut = mock.Create<ContentCopier>();

		// Act
		await sut.CopyAsync(file, showAtOnce: false);

		// Assert
		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ContentCopier.CopyAsync" />: contents that are not text never reach the clipboard.
	/// </summary>
	[Test]
	public async Task CopyAsync_Copies_Nothing_That_Is_Not_Text()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			// The start of a PNG image
			fileContentsReader
				.ReadAsync(Arg.Any<FileDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(new FileContentsReading
				{
					Contents = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D],
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(fileContentsReader);
		});

		ContentCopier sut = mock.Create<ContentCopier>();

		// Act
		await sut.CopyAsync(file, showAtOnce: false);

		// Assert
		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ContentCopier.CopyAsync" />: copies the text in the encoding of the file, without its byte
	/// order mark.
	/// </summary>
	[Test]
	[TestCase(new byte[] { 0xEF, 0xBB, 0xBF, 0x48, 0x69 })]
	[TestCase(new byte[] { 0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00 })]
	public async Task CopyAsync_Copies_The_Text_In_Its_Encoding(byte[] contents)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(Arg.Any<FileDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(new FileContentsReading
				{
					Contents = contents,
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(fileContentsReader);
		});

		ContentCopier sut = mock.Create<ContentCopier>();

		// Act
		await sut.CopyAsync(file, showAtOnce: false);

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync("Hi", html: null, rtf: null, isSensitive: false);
	}

	/// <summary>
	/// <see cref="ContentCopier.CopyAsync" />: copies the text in the encoding chosen for the file rather
	/// than in the one found from its contents.
	/// </summary>
	[Test]
	public async Task CopyAsync_Copies_The_Text_In_The_Chosen_Encoding()
	{
		// Arrange
		const string text = "Привет, мир";

		FileDto file = ItemDtoFactory.CreateFileDto(editorState: new SystemTextJsonSerializer().Serialize(new FileEditorState
		{
			Encoding = "cp866"
		}));

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(Arg.Any<FileDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(new FileContentsReading
				{
					Contents = CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(text),
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		ContentCopier sut = mock.Create<ContentCopier>();

		// Act
		await sut.CopyAsync(file, showAtOnce: false);

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync(text, html: null, rtf: null, isSensitive: false);
	}

	/// <summary>
	/// <see cref="ContentCopier.CopyAsync" />: encrypted content is flagged sensitive, plaintext content is not.
	/// </summary>
	[Test]
	public async Task CopyAsync_Flags_Sensitive_When_Encrypted([Values] bool isEncrypted)
	{
		// Arrange
		string content = RandomString.Create(20);

		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: isEncrypted
			? EncryptionStatus.Encrypted
			: EncryptionStatus.None);

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(Arg.Any<FileDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(new FileContentsReading
				{
					Contents = Encoding.UTF8.GetBytes(content),
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(fileContentsReader);
		});

		ContentCopier sut = mock.Create<ContentCopier>();

		// Act
		await sut.CopyAsync(file, showAtOnce: false);

		// Assert
		if (isEncrypted)
		{
			await clipboard
				.Received(1)
				.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), isSensitive: true);

			await clipboard
				.DidNotReceive()
				.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), isSensitive: false);
		}
		else
		{
			await clipboard
				.Received(1)
				.SetTextAsync(content, html: null, rtf: null, isSensitive: false);

			await clipboard
				.DidNotReceive()
				.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), isSensitive: true);
		}
	}

	/// <summary>
	/// <see cref="ContentCopier.CopyAsync" />: a file whose text is copied goes into the copy history, which hands on
	/// whether a shown list moves it at once.
	/// </summary>
	[Test]
	public async Task CopyAsync_Inserts_The_File_Into_The_Copy_History([Values] bool showAtOnce)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		ICopyHistoryRecorder copyHistory = Substitute.For<ICopyHistoryRecorder>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

			fileTextCopier
				.CopyAsync(file, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(FileTextStatus.Text);

			builder.RegisterInstance(copyHistory);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);
		});

		ContentCopier sut = mock.Create<ContentCopier>();

		// Act
		bool result = await sut.CopyAsync(file, showAtOnce);

		// Assert
		result
			.Should()
			.BeTrue();

		copyHistory
			.Received(1)
			.Add(file, showAtOnce);
	}

	/// <summary>
	/// <see cref="ContentCopier.CopyAsync" />: a file whose text is not copied stays out of the copy history.
	/// </summary>
	[Test]
	public async Task CopyAsync_Keeps_The_Copy_History_When_Nothing_Is_Copied(
		[Values(FileTextStatus.Empty, FileTextStatus.NotText, FileTextStatus.Damaged, FileTextStatus.Closed)] FileTextStatus status)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		ICopyHistoryRecorder copyHistory = Substitute.For<ICopyHistoryRecorder>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

			fileTextCopier
				.CopyAsync(file, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(status);

			builder.RegisterInstance(copyHistory);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);
		});

		ContentCopier sut = mock.Create<ContentCopier>();

		// Act
		bool result = await sut.CopyAsync(file, showAtOnce: false);

		// Assert
		result
			.Should()
			.BeFalse();

		copyHistory
			.DidNotReceive()
			.Add(Arg.Any<FileDto>(), Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="ContentCopier.CopyAsync" />: the decrypted contents are wiped once their text is read.
	/// </summary>
	[Test]
	public async Task CopyAsync_Zeroes_The_Decrypted_Contents(
		[Values(EncryptionStatus.Encrypted, EncryptionStatus.Decrypted)] EncryptionStatus encryptionStatus)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: encryptionStatus);

		byte[] decrypted = Encoding.UTF8.GetBytes(RandomString.Create(20));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(file, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(new FileContentsReading
				{
					Contents = decrypted,
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(fileContentsReader);
		});

		ContentCopier sut = mock.Create<ContentCopier>();

		// Act
		await sut.CopyAsync(file, showAtOnce: false);

		// Assert
		decrypted
			.Should()
			.OnlyContain(static x => x == 0);
	}
	#endregion

}
