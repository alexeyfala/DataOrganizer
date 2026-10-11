using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Documents;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Common;
using Shared.Interfaces;
using Shared.Services;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(CopyContentViewModelBase)}"" type")]
internal class CopyContentViewModelBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="CopyContentViewModelBase.CopyContentAsync" />: a file without text puts nothing on the clipboard.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyContentAsync_Copies_Nothing_From_An_Empty_File()
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

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = [],
					IsValid = true
				});

			// Empty contents come back untouched, as the real cipher hands them back.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(file, Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(x => x.ArgAt<byte[]>(1));

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

			builder.RegisterInstance(contentCipher);

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		TestCopyContentViewModel sut = mock.Create<TestCopyContentViewModel>();

		// Act
		await sut.InvokeCopyContentAsync(file, new ItemsControl());

		// Assert
		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="CopyContentViewModelBase.CopyContentAsync" />: contents that fail their check are never decrypted.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyContentAsync_Copies_Nothing_From_Damaged_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = Encoding.UTF8.GetBytes(RandomString.Create(20)),
					IsValid = false
				});

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(contentCipher);

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		TestCopyContentViewModel sut = mock.Create<TestCopyContentViewModel>();

		// Act
		await sut.InvokeCopyContentAsync(file, new ItemsControl());

		// Assert
		await contentCipher
			.DidNotReceive()
			.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="CopyContentViewModelBase.CopyContentAsync" />: contents that are not text never reach the clipboard.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyContentAsync_Copies_Nothing_That_Is_Not_Text()
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

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(8),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			// The start of a PNG image
			byte[] contents = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

			contentCipher
				.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(contents);

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

			builder.RegisterInstance(contentCipher);

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		TestCopyContentViewModel sut = mock.Create<TestCopyContentViewModel>();

		// Act
		await sut.InvokeCopyContentAsync(file, new ItemsControl());

		// Assert
		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>());

		await clipboard
			.DidNotReceive()
			.SetDataAsync(Arg.Any<DataTransfer>());
	}

	/// <summary>
	/// <see cref="CopyContentViewModelBase.CopyContentAsync" />: copies the text in the encoding of the file, without its byte
	/// order mark.
	/// </summary>
	[AvaloniaTest]
	[TestCase(new byte[] { 0xEF, 0xBB, 0xBF, 0x48, 0x69 })]
	[TestCase(new byte[] { 0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00 })]
	public async Task CopyContentAsync_Copies_The_Text_In_Its_Encoding(byte[] contents)
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

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(8),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(contents);

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

			builder.RegisterInstance(contentCipher);

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		TestCopyContentViewModel sut = mock.Create<TestCopyContentViewModel>();

		// Act
		await sut.InvokeCopyContentAsync(file, new ItemsControl());

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync("Hi");
	}

	/// <summary>
	/// <see cref="CopyContentViewModelBase.CopyContentAsync" />: copies the text in the encoding chosen for the file rather
	/// than in the one found from its contents.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyContentAsync_Copies_The_Text_In_The_Chosen_Encoding()
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

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(8),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(text));

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

			builder.RegisterInstance(contentCipher);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		TestCopyContentViewModel sut = mock.Create<TestCopyContentViewModel>();

		// Act
		await sut.InvokeCopyContentAsync(file, new ItemsControl());

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync(text);
	}

	/// <summary>
	/// <see cref="CopyContentViewModelBase.CopyContentAsync" />: encrypted content is flagged sensitive (written via <see cref="IClipboardAccessor.SetDataAsync" />), plaintext content uses <see cref="IClipboardAccessor.SetTextAsync" />.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyContentAsync_Flags_Sensitive_When_Encrypted([Values] bool isEncrypted)
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

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(8),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(Encoding.UTF8.GetBytes(content));

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

			builder.RegisterInstance(contentCipher);

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		TestCopyContentViewModel sut = mock.Create<TestCopyContentViewModel>();

		// Act
		await sut.InvokeCopyContentAsync(file, new ItemsControl());

		// Assert
		if (isEncrypted)
		{
			await clipboard
				.Received(1)
				.SetDataAsync(Arg.Any<DataTransfer>());

			await clipboard
				.DidNotReceive()
				.SetTextAsync(Arg.Any<string>());
		}
		else
		{
			await clipboard
				.Received(1)
				.SetTextAsync(content);

			await clipboard
				.DidNotReceive()
				.SetDataAsync(Arg.Any<DataTransfer>());
		}
	}

	/// <summary>
	/// <see cref="CopyContentViewModelBase.CopyContentAsync" />: a file whose text is copied goes into the copy history.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyContentAsync_Inserts_The_File_Into_The_Copy_History()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

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

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		TestCopyContentViewModel sut = mock.Create<TestCopyContentViewModel>();

		// Act
		await sut.InvokeCopyContentAsync(file, new ItemsControl());

		// Assert
		sut.CopyHistory
			.Should()
			.Equal(file);
	}

	/// <summary>
	/// <see cref="CopyContentViewModelBase.CopyContentAsync" />: a file whose text is not copied stays out of the copy
	/// history.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyContentAsync_Keeps_The_Copy_History_When_Nothing_Is_Copied(
		[Values(FileTextStatus.Empty, FileTextStatus.NotText, FileTextStatus.Damaged, FileTextStatus.Closed)] FileTextStatus status)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

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

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		TestCopyContentViewModel sut = mock.Create<TestCopyContentViewModel>();

		// Act
		await sut.InvokeCopyContentAsync(file, new ItemsControl());

		// Assert
		sut.CopyHistory
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="CopyContentViewModelBase.CopyContentAsync" />: the decrypted contents are wiped once their text is read.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyContentAsync_Zeroes_The_Decrypted_Contents(
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

			dbAccess
				.GetFileContentsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(8),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(file, Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(decrypted);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(contentCipher);

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		TestCopyContentViewModel sut = mock.Create<TestCopyContentViewModel>();

		// Act
		await sut.InvokeCopyContentAsync(file, new ItemsControl());

		// Assert
		decrypted
			.Should()
			.OnlyContain(static x => x == 0);
	}
	#endregion

	#region Nested Types
	/// <summary>
	/// Minimal concrete <see cref="CopyContentViewModelBase" /> exposing the protected copy operation.
	/// </summary>
	private sealed class TestCopyContentViewModel : CopyContentViewModelBase
	{
		#region Properties
		/// <summary>
		/// Files inserted into the copy history, in order.
		/// </summary>
		public List<FileDto> CopyHistory { get; } = [];
		#endregion

		#region Constructors
		public TestCopyContentViewModel(
			Application app,
			IDbAccess dbAccess,
			IDialogService dialogService,
			IFileTextCopier fileTextCopier,
			ILogger logger,
			IMessenger messenger,
			INotificationService notification,
			ITaskExceptionHandler exceptionHandler) : base(
				app,
				dbAccess,
				dialogService,
				fileTextCopier,
				logger,
				messenger,
				notification,
				exceptionHandler)
		{
		}
		#endregion

		#region Methods
		/// <inheritdoc />
		public override void InsertIntoCopyHistory(FileDto file, bool updateView) => CopyHistory.Add(file);

		public Task InvokeCopyContentAsync(FileDto file, ItemsControl container)
		{
			return CopyContentAsync(file, container, updateView: false);
		}
		#endregion
	}
	#endregion
}
