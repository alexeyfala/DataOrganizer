using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Documents;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using Material.Icons.Avalonia;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(FileListViewModelBase)}"" type")]
internal class FileListViewModelBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileListViewModelBase.PreviewPointerEnteredCommand" />: a file without text shows no tip.
	/// </summary>
	[AvaloniaTest]
	public async Task PreviewPointerEnteredCommand_Shows_No_Tip_For_An_Empty_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		MaterialIcon icon = new()
		{
			DataContext = file
		};

		Window window = new()
		{
			Content = icon
		};

		window.Show();

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

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Act
		await sut.PreviewPointerEnteredCommand.ExecuteAsync(icon);

		// Assert
		ToolTip.GetTip(icon)
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.PreviewPointerEnteredCommand" />: contents that are not text show no tip.
	/// </summary>
	[AvaloniaTest]
	public async Task PreviewPointerEnteredCommand_Shows_No_Tip_For_Contents_That_Are_Not_Text()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		MaterialIcon icon = new()
		{
			DataContext = file
		};

		Window window = new()
		{
			Content = icon
		};

		window.Show();

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

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Act
		await sut.PreviewPointerEnteredCommand.ExecuteAsync(icon);

		// Assert
		ToolTip.GetTip(icon)
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.PreviewPointerEnteredCommand" />: contents that fail their check show no tip.
	/// </summary>
	[AvaloniaTest]
	public async Task PreviewPointerEnteredCommand_Shows_No_Tip_For_Damaged_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		MaterialIcon icon = new()
		{
			DataContext = file
		};

		Window window = new()
		{
			Content = icon
		};

		window.Show();

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

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Act
		await sut.PreviewPointerEnteredCommand.ExecuteAsync(icon);

		// Assert
		ToolTip.GetTip(icon)
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.PreviewPointerEnteredCommand" />: contents the session key cannot open show no tip.
	/// </summary>
	[AvaloniaTest]
	[TestCaseSource(nameof(SessionCipherFailures))]
	public async Task PreviewPointerEnteredCommand_Shows_No_Tip_When_The_Decryption_Fails(Exception failure)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		MaterialIcon icon = new()
		{
			DataContext = file
		};

		Window window = new()
		{
			Content = icon
		};

		window.Show();

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

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Act
		await sut.PreviewPointerEnteredCommand.ExecuteAsync(icon);

		// Assert
		ToolTip.GetTip(icon)
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.PreviewPointerEnteredCommand" />: the tip of a file under an unlocked keeper shows its
	/// decrypted text.
	/// </summary>
	[AvaloniaTest]
	public async Task PreviewPointerEnteredCommand_Shows_The_Decrypted_Text()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		MaterialIcon icon = new()
		{
			DataContext = file
		};

		Window window = new()
		{
			Content = icon
		};

		window.Show();

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

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Act
		await sut.PreviewPointerEnteredCommand.ExecuteAsync(icon);

		// Assert
		ToolTip.GetTip(icon)
			.Should()
			.Be("Hi");
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.PreviewPointerEnteredCommand" />: the tip shows the text in the encoding of the file,
	/// without its byte order mark.
	/// </summary>
	[AvaloniaTest]
	[TestCase(new byte[] { 0xEF, 0xBB, 0xBF, 0x48, 0x69 })]
	[TestCase(new byte[] { 0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00 })]
	public async Task PreviewPointerEnteredCommand_Shows_The_Text_In_Its_Encoding(byte[] contents)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		MaterialIcon icon = new()
		{
			DataContext = file
		};

		Window window = new()
		{
			Content = icon
		};

		window.Show();

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

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Act
		await sut.PreviewPointerEnteredCommand.ExecuteAsync(icon);

		// Assert
		ToolTip.GetTip(icon)
			.Should()
			.Be("Hi");
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.PreviewPointerEnteredCommand" />: the tip shows the text in the encoding chosen for
	/// the file rather than in the one found from its contents.
	/// </summary>
	[AvaloniaTest]
	public async Task PreviewPointerEnteredCommand_Shows_The_Text_In_The_Chosen_Encoding()
	{
		// Arrange
		const string text = "Привет, мир";

		FileDto file = ItemDtoFactory.CreateFileDto(editorState: new SystemTextJsonSerializer().Serialize(new FileEditorState
		{
			Encoding = "cp866"
		}));

		MaterialIcon icon = new()
		{
			DataContext = file
		};

		Window window = new()
		{
			Content = icon
		};

		window.Show();

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

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Act
		await sut.PreviewPointerEnteredCommand.ExecuteAsync(icon);

		// Assert
		ToolTip.GetTip(icon)
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.PreviewPointerEnteredCommand" />: the decrypted contents are wiped once their text
	/// is read.
	/// </summary>
	[AvaloniaTest]
	public async Task PreviewPointerEnteredCommand_Zeroes_The_Decrypted_Contents()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		MaterialIcon icon = new()
		{
			DataContext = file
		};

		Window window = new()
		{
			Content = icon
		};

		window.Show();

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

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Act
		await sut.PreviewPointerEnteredCommand.ExecuteAsync(icon);

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
