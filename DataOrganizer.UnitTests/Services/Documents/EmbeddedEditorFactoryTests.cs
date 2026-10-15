using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Documents;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using Entities.Enums;
using NSubstitute;
using Shared.Common;
using System;

namespace DataOrganizer.UnitTests.Services.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(EmbeddedEditorFactory)}"" type")]
internal class EmbeddedEditorFactoryTests
{
	#region Methods
	/// <summary>
	/// <see cref="EmbeddedEditorFactory.Create" />: a dataset gets the editor of its records.
	/// </summary>
	[Test]
	public void Create_Gives_A_Dataset_The_Editor_Of_Its_Records()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(kind: EntityKind.Dataset);

		using AutoMock viewModelMock = AutoMock.GetLoose();

		DatasetEditorViewModel viewModel = viewModelMock.Create<DatasetEditorViewModel>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<DatasetEditorViewModel>()
				.Returns(viewModel);

			builder.RegisterInstance(viewFactory);
		});

		EmbeddedEditorFactory sut = mock.Create<EmbeddedEditorFactory>();

		// Act
		EmbeddedEditorViewModelBase? result = sut.Create(file);

		// Assert
		result
			.Should()
			.BeSameAs(viewModel);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorFactory.Create" />: a file gets the editor of its text.
	/// </summary>
	[AvaloniaTest]
	public void Create_Gives_A_File_The_Editor_Of_Its_Text()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock viewModelMock = AutoMock.GetLoose();

		EmbeddedFileEditorViewModel viewModel = viewModelMock.Create<EmbeddedFileEditorViewModel>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EmbeddedFileEditorViewModel>()
				.Returns(viewModel);

			builder.RegisterInstance(viewFactory);
		});

		EmbeddedEditorFactory sut = mock.Create<EmbeddedEditorFactory>();

		// Act
		EmbeddedEditorViewModelBase? result = sut.Create(file);

		// Assert
		result
			.Should()
			.BeSameAs(viewModel);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorFactory.Create" />: a folder gets no editor, and none is created for it.
	/// </summary>
	[Test]
	public void Create_Gives_A_Folder_No_Editor()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(kind: EntityKind.Folder);

		IViewFactory viewFactory = Substitute.For<IViewFactory>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(viewFactory));

		EmbeddedEditorFactory sut = mock.Create<EmbeddedEditorFactory>();

		// Act
		EmbeddedEditorViewModelBase? result = sut.Create(file);

		// Assert
		result
			.Should()
			.BeNull();

		viewFactory
			.ReceivedCalls()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EmbeddedEditorFactory.Create" />: the editor of a file that is not decrypted gets no keeper, even under
	/// one, so its contents are read and written as they are stored.
	/// </summary>
	[AvaloniaTest]
	public void Create_Gives_No_Keeper_To_A_File_That_Is_Not_Decrypted(
		[Values(EncryptionStatus.None, EncryptionStatus.Encrypted)] EncryptionStatus status)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: status);

		file.Parent = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		using AutoMock viewModelMock = AutoMock.GetLoose();

		EmbeddedFileEditorViewModel viewModel = viewModelMock.Create<EmbeddedFileEditorViewModel>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EmbeddedFileEditorViewModel>()
				.Returns(viewModel);

			builder.RegisterInstance(viewFactory);
		});

		EmbeddedEditorFactory sut = mock.Create<EmbeddedEditorFactory>();

		// Act
		sut.Create(file);

		// Assert
		viewModel.KeeperId
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EmbeddedEditorFactory.Create" />: the editor gets the file it edits.
	/// </summary>
	[AvaloniaTest]
	public void Create_Gives_The_Editor_The_Id_Of_The_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock viewModelMock = AutoMock.GetLoose();

		EmbeddedFileEditorViewModel viewModel = viewModelMock.Create<EmbeddedFileEditorViewModel>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EmbeddedFileEditorViewModel>()
				.Returns(viewModel);

			builder.RegisterInstance(viewFactory);
		});

		EmbeddedEditorFactory sut = mock.Create<EmbeddedEditorFactory>();

		// Act
		sut.Create(file);

		// Assert
		viewModel.FileId
			.Should()
			.Be(file.Id);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorFactory.Create" />: the editor of a decrypted file gets the keeper whose session key opens
	/// and encrypts its contents.
	/// </summary>
	[AvaloniaTest]
	public void Create_Gives_The_Editor_The_Keeper_Of_A_Decrypted_File()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		file.Parent = keeper;

		using AutoMock viewModelMock = AutoMock.GetLoose();

		EmbeddedFileEditorViewModel viewModel = viewModelMock.Create<EmbeddedFileEditorViewModel>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EmbeddedFileEditorViewModel>()
				.Returns(viewModel);

			builder.RegisterInstance(viewFactory);
		});

		EmbeddedEditorFactory sut = mock.Create<EmbeddedEditorFactory>();

		// Act
		sut.Create(file);

		// Assert
		viewModel.KeeperId
			.Should()
			.Be(keeper.Id);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorFactory.Create" />: the editor of the text of a file gets the name of the file, whose
	/// extension gives the language of the text.
	/// </summary>
	[AvaloniaTest]
	public void Create_Gives_The_Text_Editor_The_Name_Of_The_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock viewModelMock = AutoMock.GetLoose();

		EmbeddedFileEditorViewModel viewModel = viewModelMock.Create<EmbeddedFileEditorViewModel>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EmbeddedFileEditorViewModel>()
				.Returns(viewModel);

			builder.RegisterInstance(viewFactory);
		});

		EmbeddedEditorFactory sut = mock.Create<EmbeddedEditorFactory>();

		// Act
		sut.Create(file);

		// Assert
		viewModel.FileName
			.Should()
			.Be(file.Name);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorFactory.Create" />: the date of change that the editor of a file reports reaches the file.
	/// </summary>
	[AvaloniaTest]
	public void Create_Hands_The_Date_Of_Change_To_The_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		DateTime updatedAt = new(2026, 10, 12, 18, 30, 0);

		using AutoMock viewModelMock = AutoMock.GetLoose();

		EmbeddedFileEditorViewModel viewModel = viewModelMock.Create<EmbeddedFileEditorViewModel>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EmbeddedFileEditorViewModel>()
				.Returns(viewModel);

			builder.RegisterInstance(viewFactory);
		});

		EmbeddedEditorFactory sut = mock.Create<EmbeddedEditorFactory>();

		sut.Create(file);

		// Act
		viewModel.SetUpdatedAtCallback!(updatedAt);

		// Assert
		file.UpdatedAt
			.Should()
			.Be(updatedAt);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorFactory.Create" />: the editor state that the editor of a file reports reaches the file.
	/// </summary>
	[AvaloniaTest]
	public void Create_Hands_The_Editor_State_To_The_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		string state = RandomString.Create(10);

		using AutoMock viewModelMock = AutoMock.GetLoose();

		EmbeddedFileEditorViewModel viewModel = viewModelMock.Create<EmbeddedFileEditorViewModel>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EmbeddedFileEditorViewModel>()
				.Returns(viewModel);

			builder.RegisterInstance(viewFactory);
		});

		EmbeddedEditorFactory sut = mock.Create<EmbeddedEditorFactory>();

		sut.Create(file);

		// Act
		viewModel.SetEditorStateCallback!(state);

		// Assert
		file.EditorState
			.Should()
			.Be(state);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorFactory.Create" />: the editor of a file starts from the editor state that the file keeps
	/// from an earlier editor.
	/// </summary>
	[AvaloniaTest]
	public void Create_Starts_The_Editor_From_The_State_Of_The_File()
	{
		// Arrange
		string state = RandomString.Create(10);

		FileDto file = ItemDtoFactory.CreateFileDto(editorState: state);

		using AutoMock viewModelMock = AutoMock.GetLoose();

		EmbeddedFileEditorViewModel viewModel = viewModelMock.Create<EmbeddedFileEditorViewModel>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EmbeddedFileEditorViewModel>()
				.Returns(viewModel);

			builder.RegisterInstance(viewFactory);
		});

		EmbeddedEditorFactory sut = mock.Create<EmbeddedEditorFactory>();

		// Act
		sut.Create(file);

		// Assert
		viewModel.InitialEditorState
			.Should()
			.Be(state);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorFactory.Create" />: the editor of a file opens in the read-only mode of the editor window.
	/// </summary>
	[AvaloniaTest]
	public void Create_Takes_The_Read_Only_Mode_Of_The_Editor_Window()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		// The window lives as long as the test, as the editor reads the mode from it.
		using AutoMock windowMock = AutoMock.GetLoose();

		using AutoMock viewModelMock = AutoMock.GetLoose(builder =>
		{
			EditorViewModel editor = windowMock.Create<EditorViewModel>();

			editor.IsReadOnly = true;

			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			lifetime
				.Windows
				.Returns([new EditorWindow(editor)]);

			Application app = Substitute.For<Application>();

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app);
		});

		EmbeddedFileEditorViewModel viewModel = viewModelMock.Create<EmbeddedFileEditorViewModel>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EmbeddedFileEditorViewModel>()
				.Returns(viewModel);

			builder.RegisterInstance(viewFactory);
		});

		EmbeddedEditorFactory sut = mock.Create<EmbeddedEditorFactory>();

		// Act
		sut.Create(file);

		// Assert
		viewModel.IsReadOnly
			.Should()
			.BeTrue();
	}
	#endregion
}
