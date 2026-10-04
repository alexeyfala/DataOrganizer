using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using Material.Icons.Avalonia;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Interfaces;
using Shared.Services;
using System.Text;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(FileListViewModelBase)}"" type")]
internal class FileListViewModelBaseTests
{
	#region Methods
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
					// UTF-16 without a byte order mark
					Contents = [0x48, 0x00, 0x69, 0x00],
					IsValid = true
				});

			builder.RegisterInstance(dbAccess);

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
	#endregion
}
