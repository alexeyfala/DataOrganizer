using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Editor;
using DataOrganizer.Messages.Encryption;
using DataOrganizer.Messages.Execution;
using DataOrganizer.Services.Documents;
using DataOrganizer.Services.Encryption;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using DynamicData.Binding;
using Entities.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.ViewModels.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(EditorViewModel)}"" type")]
internal class EditorViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="EditorViewModel.AddAsync" />: delegates to the hierarchy editor and, on success, refreshes the object count.
	/// </summary>
	[Test]
	public async Task AddAsync_Delegates_To_Hierarchy_Editor(
		[Values] EntityKind type,
		[Values] bool hasParent)
	{
		// Arrange
		IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

		ExplorerItemDtoBase created = Substitute.For<ExplorerItemDtoBase>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			hierarchyEditor
				.AddAsync(
					Arg.Any<string>(),
					Arg.Any<EntityKind>(),
					Arg.Any<FolderDto>(),
					Arg.Any<Collection<ExplorerItemDtoBase>>(),
					Arg.Any<CancellationToken>())
				.Returns(created);

			builder.RegisterInstance(hierarchyEditor);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		FolderDto? parent = hasParent ? ItemDtoFactory.CreateFolderDto() : null;

		string name = RandomString.Create(10);

		// Act
		ExplorerItemDtoBase? entity = await sut.AddAsync(name, type, parent);

		// Assert
		entity
			.Should()
			.BeSameAs(created);

		await hierarchyEditor.Received(1).AddAsync(
			name,
			type,
			parent,
			sut.Hierarchy,
			Arg.Any<CancellationToken>());

		sut.HierarchySummary
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="EditorViewModel.AddHierarchy" />: the supplied objects are added to the Hierarchy property.
	/// </summary>
	[Test]
	public void AddHierarchy_Adds_Objects_To_Hierarchy_Property()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		ExplorerItemDtoBase[] hierarchy = [.. ItemDtoFactory.CreateFolderDtos(5).Concat<ExplorerItemDtoBase>(ItemDtoFactory.CreateFileDtos(5))];

		// Act
		sut.AddHierarchy(hierarchy);

		// Assert
		sut.Hierarchy
			.Should()
			.Contain(hierarchy);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ChangePassword" />: open files are closed and the folder password is changed.
	/// </summary>
	[Test]
	public async Task ChangePassword_Closes_Open_Files_And_Changes_The_Password()
	{
		// Arrange
		FileDto[] editingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isEditing: true)];

		FileDto[] executingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isExecuting: true)];

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IFolderProtection folderProtection = Substitute.For<IFolderProtection>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(folderProtection);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ChangePassword(folder);

		// Assert
		editingFiles
			.Should()
			.OnlyContain(x => !x.IsEditing);

		executingFiles
			.Should()
			.OnlyContain(x => !x.IsExecuting);

		await folderProtection
			.Received(1)
			.ChangePasswordAsync(Arg.Any<FolderDto>());
	}

	/// <summary>
	/// <see cref="ViewModelBase.CloseExecutingFile" />: the file is removed from executing files, unmarked, and closed in the engine.
	/// </summary>
	[Test]
	public void CloseExecutingFile_Closes_File()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		dto.IsExecuting = true;

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(engine);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.ExecutingFiles
			.Add(dto);

		// Act
		sut.CloseExecutingFile(dto);

		// Assert
		sut.ExecutingFiles
			.Should()
			.NotContain(dto);

		dto.IsExecuting
			.Should()
			.BeFalse();

		engine
			.Received(1)
			.CloseAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="ViewModelBase.CloseFiles" />: both editing and executing files are unmarked.
	/// </summary>
	[Test]
	public void CloseFiles_Closes_Editing_And_Executing_Files()
	{
		// Arrange
		FileDto[] editingFiles = [.. ItemDtoFactory.CreateFileDtos(2)];

		FileDto[] executingFiles = [.. ItemDtoFactory.CreateFileDtos(2)];

		editingFiles.ForEach(x => x.IsEditing = true);

		executingFiles.ForEach(x => x.IsExecuting = true);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<InlineDispatcherAccessor>()
			.As<IDispatcherAccessor>());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.CloseFiles(editingFiles, executingFiles);

		// Assert
		editingFiles
			.Should()
			.OnlyContain(x => !x.IsEditing);

		executingFiles
			.Should()
			.OnlyContain(x => !x.IsExecuting);
	}

	/// <summary>
	/// <see cref="EditorViewModel.DecryptFolder" />: open files are closed and the folder is decrypted.
	/// </summary>
	[Test]
	public async Task DecryptFolder_Closes_Open_Files_And_Decrypts_The_Folder()
	{
		// Arrange
		FileDto[] editingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isEditing: true)];

		FileDto[] executingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isExecuting: true)];

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IFolderProtection folderProtection = Substitute.For<IFolderProtection>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(folderProtection);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.DecryptFolder(folder);

		// Assert
		editingFiles
			.Should()
			.OnlyContain(x => !x.IsEditing);

		executingFiles
			.Should()
			.OnlyContain(x => !x.IsExecuting);

		await folderProtection
			.Received(1)
			.DecryptFolderAsync(Arg.Any<FolderDto>(), Arg.Any<FileDto[]>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.DecryptFolder" />: nothing happens when the folder has no files to close.
	/// </summary>
	[Test]
	public async Task DecryptFolder_Does_Nothing_If_Missing_Files()
	{
		// Arrange
		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(TypedParameter.From(dialogService));

		// Act
		await sut.DecryptFolder(ItemDtoFactory.CreateFolderDto());

		// Assert
		await dialogService
			.DidNotReceive()
			.RequestCloseFilesAsync();
	}

	/// <summary>
	/// <see cref="EditorViewModel.DeleteAsync" />: on success the deleted file is closed and removed from executing files.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Closes_File_On_Success()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(isExecuting: true);

		IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			hierarchyEditor
				.DeleteAsync(
					Arg.Any<ExplorerItemDtoBase>(),
					Arg.Any<Collection<ExplorerItemDtoBase>>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(hierarchyEditor);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.ExecutingFiles
			.Add(file);

		// Act
		bool result = await sut.DeleteAsync(file);

		// Assert
		result
			.Should()
			.BeTrue();

		sut.ExecutingFiles
			.Should()
			.NotContain(file);

		file.IsExecuting
			.Should()
			.BeFalse();

		await hierarchyEditor.Received(1).DeleteAsync(
			file,
			sut.Hierarchy,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.DeleteAsync" />: every file of a deleted folder's subtree is closed in the editor and in the
	/// operating system.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Closes_The_Files_Of_A_Deleted_Folder()
	{
		// Arrange
		FileDto editingFile = ItemDtoFactory.CreateFileDto(isEditing: true);

		FileDto executingFile = ItemDtoFactory.CreateFileDto(isExecuting: true);

		FolderDto subfolder = ItemDtoFactory.CreateFolderDto();

		subfolder
			.Children
			.Add(editingFile);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder
			.Children
			.Add(executingFile);

		folder
			.Children
			.Add(subfolder);

		IExecutionEngine executionEngine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(
					Arg.Any<ExplorerItemDtoBase>(),
					Arg.Any<Collection<ExplorerItemDtoBase>>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(hierarchyEditor);

			builder.RegisterInstance(executionEngine);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.ExecutingFiles
			.Add(executingFile);

		// Act
		bool result = await sut.DeleteAsync(folder);

		// Assert
		result
			.Should()
			.BeTrue();

		editingFile.IsEditing
			.Should()
			.BeFalse();

		executingFile.IsExecuting
			.Should()
			.BeFalse();

		sut.ExecutingFiles
			.Should()
			.NotContain(executingFile);

		await executionEngine
			.Received(1)
			.CloseAsync(executingFile.Id, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.DeleteAsync" />: the key of a deleted folder is dropped with the folder.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Drops_The_Key_Of_A_Deleted_Folder()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(
					Arg.Any<ExplorerItemDtoBase>(),
					Arg.Any<Collection<ExplorerItemDtoBase>>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(hierarchyEditor);

			builder.RegisterInstance(contentVisibility);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		bool result = await sut.DeleteAsync(folder);

		// Assert
		result
			.Should()
			.BeTrue();

		contentVisibility
			.Received(1)
			.DiscardKeys(folder);
	}

	/// <summary>
	/// <see cref="EditorViewModel.DeleteAsync" />: when the editor reports failure the file is left open.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Keeps_File_When_Editor_Fails()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(isExecuting: true);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(
					Arg.Any<ExplorerItemDtoBase>(),
					Arg.Any<Collection<ExplorerItemDtoBase>>(),
					Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(hierarchyEditor);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.ExecutingFiles
			.Add(file);

		// Act
		bool result = await sut.DeleteAsync(file);

		// Assert
		result
			.Should()
			.BeFalse();

		sut.ExecutingFiles
			.Should()
			.Contain(file);

		file.IsExecuting
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorViewModel.DeleteAsync" />: a folder that was not deleted keeps its key.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Keeps_The_Key_When_Editor_Fails()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(
					Arg.Any<ExplorerItemDtoBase>(),
					Arg.Any<Collection<ExplorerItemDtoBase>>(),
					Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(hierarchyEditor);

			builder.RegisterInstance(contentVisibility);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		bool result = await sut.DeleteAsync(folder);

		// Assert
		result
			.Should()
			.BeFalse();

		contentVisibility
			.DidNotReceiveWithAnyArgs()
			.DiscardKeys(default!);
	}

	/// <summary>
	/// <see cref="EditorViewModel.DeleteAsync" />: a file keeps no key of its own, so its deletion leaves the keys alone.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Leaves_The_Keys_Alone_For_A_Deleted_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(
					Arg.Any<ExplorerItemDtoBase>(),
					Arg.Any<Collection<ExplorerItemDtoBase>>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(hierarchyEditor);

			builder.RegisterInstance(contentVisibility);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		bool result = await sut.DeleteAsync(file);

		// Assert
		result
			.Should()
			.BeTrue();

		contentVisibility
			.DidNotReceiveWithAnyArgs()
			.DiscardKeys(default!);
	}

	/// <summary>
	/// <see cref="EditorViewModel.DeleteAsync" />: every file of a deleted folder's subtree leaves the copy history.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Removes_The_Files_Of_A_Deleted_Folder_From_The_Copy_History()
	{
		// Arrange
		FileDto nestedFile = ItemDtoFactory.CreateFileDto();

		FileDto keptFile = ItemDtoFactory.CreateFileDto();

		FolderDto subfolder = ItemDtoFactory.CreateFolderDto();

		subfolder
			.Children
			.Add(nestedFile);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder
			.Children
			.Add(subfolder);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(
					Arg.Any<ExplorerItemDtoBase>(),
					Arg.Any<Collection<ExplorerItemDtoBase>>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(hierarchyEditor);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.CopyHistorySettings
			.ItemIds
			.Add(nestedFile.Id);

		sut
			.CopyHistorySettings
			.ItemIds
			.Add(keptFile.Id);

		// Act
		bool result = await sut.DeleteAsync(folder);

		// Assert
		result
			.Should()
			.BeTrue();

		sut.CopyHistorySettings.ItemIds
			.Should()
			.Equal(keptFile.Id);
	}

	/// <summary>
	/// <see cref="EditorViewModel.EditingFilesViewLoadedCommand" />: the tabs of the editor open with the selected one
	/// and the previous one.
	/// </summary>
	[Test]
	public void EditingFilesViewLoadedCommand_Restores_The_Editor_Tabs()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(count: 3)];

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.EditorTabs = new()
		{
			Files = files,
			PreviousFile = files[2],
			SelectedFile = files[1]
		};

		EditingFilesViewModel editingFiles = mock.Create<EditingFilesViewModel>();

		// Act
		sut
			.EditingFilesViewLoadedCommand
			.Execute(editingFiles);

		// Assert
		editingFiles.Items
			.Should()
			.Equal(files);

		editingFiles.SelectedIndex
			.Should()
			.Be(1);

		editingFiles.PreviousFile
			.Should()
			.BeSameAs(files[2]);
	}

	/// <summary>
	/// <see cref="EditorViewModel.EditNote" />: the note of a protected object is declared sensitive to the dialog.
	/// </summary>
	[Test]
	public async Task EditNote_Declares_The_Note_Of_A_Protected_Object_Sensitive([Values] EncryptionStatus encryptionStatus)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: encryptionStatus);

		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dialogService
				.RequestMultilineTextAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<bool>(),
					Arg.Any<CancellationToken>())
				.Returns(new TextInputResult());

			builder.RegisterInstance(dialogService);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.EditNote(file);

		// Assert
		await dialogService.Received(1).RequestMultilineTextAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			encryptionStatus != EncryptionStatus.None,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.EditNote" />: the note of the dialog is stored through the note editor.
	/// </summary>
	[Test]
	public async Task EditNote_Delegates_To_Note_Editor()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		string storedNote = RandomString.Create(20);

		string editedNote = RandomString.Create(20);

		IDialogService dialogService = Substitute.For<IDialogService>();

		INoteEditor noteEditor = Substitute.For<INoteEditor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dialogService
				.RequestMultilineTextAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<bool>(),
					Arg.Any<CancellationToken>())
				.Returns(new TextInputResult(true, editedNote));

			INoteReader noteReader = Substitute.For<INoteReader>();

			noteReader
				.ReadNote(file)
				.Returns(storedNote);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(noteEditor);

			builder.RegisterInstance(noteReader);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.EditNote(file);

		// Assert
		await dialogService.Received(1).RequestMultilineTextAsync(
			storedNote,
			file.Name,
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());

		await noteEditor.Received(1).EditAsync(
			file,
			editedNote,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.EditNote" />: a cancelled dialog leaves the note untouched.
	/// </summary>
	[Test]
	public async Task EditNote_Keeps_Note_When_Dialog_Is_Cancelled()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		INoteEditor noteEditor = Substitute.For<INoteEditor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestMultilineTextAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<bool>(),
					Arg.Any<CancellationToken>())
				.Returns(new TextInputResult());

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(noteEditor);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.EditNote(file);

		// Assert
		await noteEditor.DidNotReceive().EditAsync(
			Arg.Any<ExplorerItemDtoBase>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.EditNoteCommand" /> CanExecute.
	/// </summary>
	[Test]
	public void EditNoteCommand_CanExecute_Is_Denied_In_Read_Only_Mode_And_For_An_Encrypted_Object(
		[Values] bool isReadOnly,
		[Values] EncryptionStatus encryptionStatus)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.IsReadOnly = isReadOnly;

		// Act
		bool canExecute = sut
			.EditNoteCommand
			.CanExecute(ItemDtoFactory.CreateFileDto(encryptionStatus: encryptionStatus));

		// Assert
		canExecute
			.Should()
			.Be(!isReadOnly && encryptionStatus != EncryptionStatus.Encrypted);
	}

	/// <summary>
	/// <see cref="EditorViewModel.EditNoteCommand" /> CanExecute.
	/// </summary>
	[Test]
	public void EditNoteCommand_CanExecute_Returns_False_Without_An_Object()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		bool canExecute = sut
			.EditNoteCommand
			.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EditorViewModel.EncryptFolder" />: open files are closed and the folder is encrypted.
	/// </summary>
	[Test]
	public async Task EncryptFolder_Closes_Open_Files_And_Encrypts_The_Folder()
	{
		// Arrange
		FileDto[] editingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isEditing: true)];

		FileDto[] executingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isExecuting: true)];

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IFolderProtection folderProtection = Substitute.For<IFolderProtection>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(folderProtection);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.EncryptFolder(folder);

		// Assert
		editingFiles
			.Should()
			.OnlyContain(x => !x.IsEditing);

		executingFiles
			.Should()
			.OnlyContain(x => !x.IsExecuting);

		await folderProtection
			.Received(1)
			.EncryptFolderAsync(Arg.Any<FolderDto>(), Arg.Any<FileDto[]>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.EncryptFolder" />: nothing happens when the folder has no files to close.
	/// </summary>
	[Test]
	public async Task EncryptFolder_Does_Nothing_If_Missing_Files()
	{
		// Arrange
		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(TypedParameter.From(dialogService));

		// Act
		await sut.EncryptFolder(ItemDtoFactory.CreateFolderDto());

		// Assert
		await dialogService
			.DidNotReceive()
			.RequestCloseFilesAsync();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ExecuteFile" />: contents are not loaded when the file is already executing.
	/// </summary>
	[Test]
	public async Task ExecuteFile_Does_Not_Load_Contents_Of_An_Already_Executing_File()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExecutionEngine engine = Substitute.For<IExecutionEngine>();

			engine
				.IsExecuting(Arg.Any<Guid>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(engine);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ExecuteFile(ItemDtoFactory.CreateFileDto());

		// Assert
		await dbAccess
			.DidNotReceive()
			.GetFileContentsAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ExecuteFile" />: contents that fail their check are not run.
	/// </summary>
	[Test]
	public async Task ExecuteFile_Does_Not_Run_Damaged_Contents()
	{
		// Arrange
		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents contents = new()
			{
				Contents = RandomValues.CreateBytes(10),
				IsValid = false
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(engine);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ExecuteFile(ItemDtoFactory.CreateFileDto());

		// Assert
		await engine
			.DidNotReceive()
			.ExecuteAsync(Arg.Any<ExecuteFileParameters>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ExecuteFile" />: contents the session key does not open are not run.
	/// </summary>
	[Test]
	public async Task ExecuteFile_Does_Not_Run_When_The_Decryption_Fails()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateFolderDto();

		keeper.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto dto = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper
			.Children
			.Add(dto);

		dto.Parent = keeper;

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents contents = new()
			{
				Contents = RandomValues.CreateBytes(10),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Decrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Throws(new AuthenticationTagMismatchException());

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(engine);

			builder.RegisterInstance(sessionKeyStore);

			// The real cipher, so the contents reach the session key whichever call opens them.
			builder
				.RegisterType<ContentCipher>()
				.As<IContentCipher>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ExecuteFile(dto);

		// Assert
		await engine
			.DidNotReceive()
			.ExecuteAsync(Arg.Any<ExecuteFileParameters>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ExecuteFile" />: a read the database could not answer is not run.
	/// </summary>
	[Test]
	public async Task ExecuteFile_Does_Not_Run_When_The_Read_Fails()
	{
		// Arrange
		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(engine);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ExecuteFile(ItemDtoFactory.CreateFileDto());

		// Assert
		await engine
			.DidNotReceive()
			.ExecuteAsync(Arg.Any<ExecuteFileParameters>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ExecuteFile" />: the file is marked executing, added to executing files, and executed by the engine.
	/// </summary>
	[Test]
	public async Task ExecuteFile_Marks_The_File_Executing_And_Runs_It()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			engine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>())
				.Returns(true);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents contents = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(engine);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ExecuteFile(dto);

		// Assert
		dto.IsExecuting
			.Should()
			.BeTrue();

		sut.ExecutingFiles
			.Should()
			.Contain(dto);

		await engine
			.Received(1)
			.ExecuteAsync(Arg.Any<ExecuteFileParameters>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ExecuteFile" />: a protected file runs with its contents opened by the session key of its
	/// keeper, and the run keeps the keeper for the changes.
	/// </summary>
	[Test]
	public async Task ExecuteFile_Runs_A_Decrypted_File_With_Its_Opened_Contents()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateFolderDto();

		keeper.EncryptedDek = RandomValues.CreateBytes(10);

		FileDto dto = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper
			.Children
			.Add(dto);

		dto.Parent = keeper;

		byte[] opened = RandomValues.CreateBytes(10);

		ExecuteFileParameters? parameters = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			byte[] stored = RandomValues.CreateBytes(16);

			ValidatedContents contents = new()
			{
				Contents = stored,
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(dto.Id)
				.Returns(contents);

			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Decrypt(keeper.Id, ContentIdentity.Contents, stored)
				.Returns(opened);

			IExecutionEngine engine = Substitute.For<IExecutionEngine>();

			engine
				.ExecuteAsync(Arg.Do<ExecuteFileParameters>(x => parameters = x))
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(engine);

			builder.RegisterInstance(sessionKeyStore);

			// The real cipher, so the contents reach the session key whichever call opens them.
			builder
				.RegisterType<ContentCipher>()
				.As<IContentCipher>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ExecuteFile(dto);

		// Assert
		parameters!.Contents
			.Should()
			.Equal(opened);

		parameters.KeeperId
			.Should()
			.Be(keeper.Id);
	}

	/// <summary>
	/// <see cref="EditorViewModel.Exit" />: the application is shut down.
	/// </summary>
	[Test]
	public void Exit_Shutdowns_The_Application()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.Exit(null);

		// Assert
		sut.IsShutdown
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ExpandCollapseAllFoldersAsync" />: all folders are expanded or collapsed, and the selection is kept on expand and reset on collapse.
	/// </summary>
	[Test]
	public async Task ExpandCollapseAllFoldersAsync_Applies_To_All_Folders_And_Resets_Selection_On_Collapse([Values] bool isExpandAll)
	{
		// Arrange
		FolderDto selectedFolder = ItemDtoFactory.CreateFolderDto();

		selectedFolder.IsSelected = true;

		FolderDto[] folders = [.. ItemDtoFactory.CreateFolderDtos(5)];

		folders = [.. folders, .. selectedFolder.ToEnumerable()];

		folders
			.ForEach(x => x.Children.AddRange(ItemDtoFactory.CreateFolderDtos(5)))
			.GetFolders()
			.ForEach(x => x.IsExpanded = !isExpandAll);

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.SelectedObject = selectedFolder;

		sut
			.Hierarchy
			.AddRange(folders);

		// Act
		await sut.ExpandCollapseAllFoldersAsync(isExpandAll);

		// Assert
		folders.GetFolders()
			.Should()
			.OnlyContain(x => x.IsExpanded == isExpandAll);

		if (isExpandAll)
		{
			sut.SelectedObject
				.Should()
				.NotBeNull();

			selectedFolder.IsSelected
				.Should()
				.BeTrue();
		}
		else
		{
			sut.SelectedObject
				.Should()
				.BeNull();

			selectedFolder.IsSelected
				.Should()
				.BeFalse();
		}
	}

	/// <summary>
	/// <see cref="EditorViewModel.HandleSettingsChangedAsync" />: a changed auto-lock delay restarts the countdown at once.
	/// </summary>
	[Test]
	public async Task HandleSettingsChangedAsync_Applies_A_Changed_Auto_Lock_Delay()
	{
		// Arrange
		IAutoLockService autoLock = Substitute.For<IAutoLockService>();

		AppSettings saved = SettingsFactory.CreateSettings();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(saved);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(autoLock);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.AddHierarchy([ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)]);

		AppSettings changed = SettingsFactory.CreateSettings();

		changed.AutoLockMinutes = 5;

		// Act
		await sut.HandleSettingsChangedAsync(true, changed);

		// Assert
		autoLock
			.Received(1)
			.Arm();
	}

	/// <summary>
	/// <see cref="EditorViewModel.HandleSettingsChangedAsync" />: on save hotkeys are restarted and settings persisted, otherwise the material theme is reapplied.
	/// </summary>
	[Test]
	public async Task HandleSettingsChangedAsync_Handles_Bussiness_Logic_After_Settings_Changing([Values] bool isSave)
	{
		// Arrange
		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		IAppThemeService themeService = Substitute.For<IAppThemeService>();

		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		AppSettings settings = SettingsFactory.CreateSettings(trackHotkeys: true);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			hook
				.IsRunning
				.Returns(true);

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(themeService);

			builder.RegisterInstance(hook);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Tracking is started while the view model is built, and that belongs to the arrangement.
		hook.ClearReceivedCalls();

		// Act
		await sut.HandleSettingsChangedAsync(isSave, settings);

		// Assert
		if (isSave)
		{
			await hook
				.Received(1)
				.StopTrackingAsync();

			await hook
				.Received(1)
				.StartTrackingAsync(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());

			settingsStore
				.Received(1)
				.Overwrite(Arg.Any<AppSettings>());

			settingsStore
				.Received(1)
				.Save();
		}
		else
		{
			themeService
				.Received()
				.ApplyFromSettings();

		}
	}

	/// <summary>
	/// <see cref="EditorViewModel.HandleSettingsChangedAsync" />: an unchanged auto-lock delay leaves the countdown running.
	/// </summary>
	[Test]
	public async Task HandleSettingsChangedAsync_Keeps_An_Unchanged_Auto_Lock_Delay()
	{
		// Arrange
		IAutoLockService autoLock = Substitute.For<IAutoLockService>();

		AppSettings settings = SettingsFactory.CreateSettings();

		settings.AutoLockMinutes = 5;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(autoLock);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.AddHierarchy([ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)]);

		// Act
		await sut.HandleSettingsChangedAsync(true, settings);

		// Assert
		autoLock
			.DidNotReceive()
			.Arm();

		autoLock
			.DidNotReceive()
			.Stop();
	}

	/// <summary>
	/// <see cref="EditorViewModel.HideAllFileContents" />: all open files are closed and their contents re-encrypted.
	/// </summary>
	[Test]
	public async Task HideAllFileContents_Closes_Open_Files_And_Hides_All_Contents()
	{
		// Arrange
		FileDto[] editingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted)];

		FileDto[] executingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isExecuting: true,
			encryptionStatus: EncryptionStatus.Decrypted)];

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(contentVisibility);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.AddHierarchy(editingFiles.Concat(executingFiles));

		// Act
		await sut.HideAllFileContents();

		// Assert
		editingFiles
			.Should()
			.OnlyContain(x => !x.IsEditing);

		executingFiles
			.Should()
			.OnlyContain(x => !x.IsExecuting);

		// Marking the objects and dropping the keys belongs to the encryption service.
		contentVisibility
			.Received(1)
			.HideAllContents(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.HideAllFileContents" />: nothing is hidden and no file is closed while an editor
	/// cannot persist its changes, so hiding never discards them.
	/// </summary>
	[Test]
	public async Task HideAllFileContents_Keeps_Contents_When_An_Editor_Cannot_Save()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		IMessenger messenger = new StrongReferenceMessenger();

		object recipient = new();

		messenger.Register<FlushEditorsMessage>(recipient, static (_, message) => message.Reply(false));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.AddHierarchy([file]);

		// Act
		await sut.HideAllFileContents();

		// Assert
		file.IsEditing
			.Should()
			.BeTrue();

		contentVisibility
			.DidNotReceive()
			.HideAllContents(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.HideFileContents" />: the file is closed and its contents marked encrypted.
	/// </summary>
	[Test]
	public async Task HideFileContents_Closes_The_File_And_Hides_Its_Contents([Values] bool isEditing)
	{
		// Arrange
		FileDto file = isEditing
			? ItemDtoFactory.CreateFileDto(isEditing: true)
			: ItemDtoFactory.CreateFileDto(isExecuting: true);

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(contentVisibility);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.HideFileContents(file);

		// Assert
		// Marking the object and dropping the key belongs to the encryption service.
		contentVisibility
			.Received(1)
			.HideFileContents(file);

		file.IsEditing
			.Should()
			.BeFalse();

		file.IsExecuting
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EditorViewModel.HideFileContents" />: a single file is not hidden while an editor cannot persist its changes.
	/// </summary>
	[Test]
	public async Task HideFileContents_Keeps_Contents_When_An_Editor_Cannot_Save()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		IDialogService dialogService = Substitute.For<IDialogService>();

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		IMessenger messenger = new StrongReferenceMessenger();

		object recipient = new();

		messenger.Register<FlushEditorsMessage>(recipient, static (_, message) => message.Reply(false));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.HideFileContents(file);

		// Assert
		await dialogService
			.DidNotReceive()
			.RequestCloseFilesAsync();

		contentVisibility
			.DidNotReceive()
			.HideFileContents(Arg.Any<FileDto>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.HideFolderContents" />: the folder's open files are closed and its contents hidden.
	/// </summary>
	[Test]
	public async Task HideFolderContents_Closes_Open_Files_And_Hides_The_Contents()
	{
		// Arrange
		FileDto[] editingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isEditing: true)];

		FileDto[] executingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isExecuting: true)];

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(contentVisibility);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.HideFolderContents(folder);

		// Assert
		editingFiles
			.Should()
			.OnlyContain(x => !x.IsEditing);

		executingFiles
			.Should()
			.OnlyContain(x => !x.IsExecuting);

		contentVisibility
			.Received(1)
			.HideFolderContents(Arg.Any<FolderDto>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.HideFolderContents" />: a folder is not hidden while an editor cannot persist its changes.
	/// </summary>
	[Test]
	public async Task HideFolderContents_Keeps_Contents_When_An_Editor_Cannot_Save()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		FileDto file = ItemDtoFactory.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		folder
			.Children
			.Add(file);

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		IMessenger messenger = new StrongReferenceMessenger();

		object recipient = new();

		messenger.Register<FlushEditorsMessage>(recipient, static (_, message) => message.Reply(false));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.HideFolderContents(folder);

		// Assert
		file.IsEditing
			.Should()
			.BeTrue();

		contentVisibility
			.DidNotReceive()
			.HideFolderContents(Arg.Any<FolderDto>());
	}

	/// <summary>
	/// <see cref="ViewModelBase.Hierarchy" />: in read-only mode an expanded folder is not saved.
	/// </summary>
	[Test]
	public async Task Hierarchy_In_Read_Only_Mode_Saves_No_Expanded_Folder()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(propertyWriter));

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(folder);

		sut.IsReadOnly = true;

		// Act
		folder.IsExpanded = true;

		// Assert
		await propertyWriter.DidNotReceive().UpdateIsExpandedAsync(
			folder.Id,
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ViewModelBase.Hierarchy" />: a folder added to the tree after it was shown is saved when it is expanded.
	/// </summary>
	[Test]
	public async Task Hierarchy_Saves_A_Folder_Added_Later_When_It_Is_Expanded()
	{
		// Arrange
		FolderDto parent = ItemDtoFactory.CreateFolderDto();

		FolderDto child = ItemDtoFactory.CreateFolderDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(propertyWriter));

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(parent);

		child.Parent = parent;

		parent.Children.Add(child);

		// Act
		child.IsExpanded = true;

		// Assert
		await propertyWriter.Received(1).UpdateIsExpandedAsync(
			child.Id,
			true,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ViewModelBase.Hierarchy" />: a folder of the tree is saved in the state it is turned to, expanded or
	/// collapsed.
	/// </summary>
	[Test]
	public async Task Hierarchy_Saves_A_Folder_Expanded_Or_Collapsed([Values] bool isExpanded)
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.IsExpanded = !isExpanded;

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(propertyWriter));

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(folder);

		// Act
		folder.IsExpanded = isExpanded;

		// Assert
		await propertyWriter.Received(1).UpdateIsExpandedAsync(
			folder.Id,
			isExpanded,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ViewModelBase.Hierarchy" />: a folder inside another folder is saved when it is expanded.
	/// </summary>
	[Test]
	public async Task Hierarchy_Saves_A_Nested_Folder_When_It_Is_Expanded()
	{
		// Arrange
		FolderDto parent = ItemDtoFactory.CreateFolderDto();

		FolderDto child = ItemDtoFactory.CreateFolderDto();

		child.Parent = parent;

		parent.Children.Add(child);

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(propertyWriter));

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(parent);

		// Act
		child.IsExpanded = true;

		// Assert
		await propertyWriter.Received(1).UpdateIsExpandedAsync(
			child.Id,
			true,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ViewModelBase.Hierarchy" />: a folder that left the tree is not saved any more.
	/// </summary>
	[Test]
	public async Task Hierarchy_Saves_No_Folder_After_It_Left_The_Tree()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(propertyWriter));

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(folder);

		sut.Hierarchy.Remove(folder);

		// Act
		folder.IsExpanded = true;

		// Assert
		await propertyWriter.DidNotReceive().UpdateIsExpandedAsync(
			folder.Id,
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ViewModelBase.Hierarchy" />: while an action runs, an expanded folder is not saved.
	/// </summary>
	[Test]
	public async Task Hierarchy_While_An_Action_Runs_Saves_No_Expanded_Folder()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(propertyWriter));

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(folder);

		sut.Receive(new ShowProgressBarMessage(true));

		// Act
		folder.IsExpanded = true;

		// Assert
		await propertyWriter.DidNotReceive().UpdateIsExpandedAsync(
			folder.Id,
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: an import that replaces the hierarchy drops the keys of the folders it removed.
	/// </summary>
	[Test]
	public async Task Import_Drops_The_Keys_On_Replacement()
	{
		// Arrange
		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			dataExchange
				.ImportDataAsync(Arg.Any<Collection<ExplorerItemDtoBase>>())
				.Returns(new ImportDataResult([], ImportMode.Replace));

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(contentVisibility);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.Import();

		// Assert
		contentVisibility
			.Received(1)
			.DiscardAllKeys();
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: an import that appends leaves the hierarchy in place, keys included.
	/// </summary>
	[Test]
	public async Task Import_Keeps_The_Keys_On_Appending()
	{
		// Arrange
		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			dataExchange
				.ImportDataAsync(Arg.Any<Collection<ExplorerItemDtoBase>>())
				.Returns(new ImportDataResult([], ImportMode.Append));

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(contentVisibility);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.Import();

		// Assert
		contentVisibility
			.DidNotReceive()
			.DiscardAllKeys();
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: the current hierarchy is passed to the data exchange service for import.
	/// </summary>
	[Test]
	public async Task Import_Passes_The_Hierarchy_To_The_Data_Exchange()
	{
		// Arrange
		FileDto[] editingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isEditing: true)];

		FileDto[] executingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isExecuting: true)];

		IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(dataExchange);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.AddRange(editingFiles);

		sut
			.Hierarchy
			.AddRange(executingFiles);

		// Act
		await sut.Import();

		// Assert
		await dataExchange
			.Received(1)
			.ImportDataAsync(Arg.Any<Collection<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Initialize" />: a window saved off every screen opens in the center of the screen.
	/// </summary>
	[AvaloniaTest]
	public void Initialize_Centers_A_Window_Saved_Off_The_Screens()
	{
		// Arrange
		EditorWindowSettings windowSettings = new()
		{
			IsReadOnly = false,
			IsTopmost = false,
			NavigationColumnWidth = 200.0,
			Size = new(700, 500),
			WindowState = WindowState.Normal,
			X = 50000,
			Y = 50000
		};

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		Window window = new();

		// Act
		sut.Initialize(
			window,
			windowSettings,
			new CopyHistoryViewSettings());

		// Assert
		window.WindowStartupLocation
			.Should()
			.Be(WindowStartupLocation.CenterScreen);
	}

	/// <summary>
	/// <see cref="EditorViewModel.Initialize" />: a window saved without a size takes the default one, and its navigation
	/// column a third of it.
	/// </summary>
	[AvaloniaTest]
	public void Initialize_Gives_The_Default_Size_Without_A_Saved_One()
	{
		// Arrange
		EditorWindowSettings windowSettings = new()
		{
			IsReadOnly = false,
			IsTopmost = false,
			NavigationColumnWidth = 0.0,
			Size = new(0, 0),
			WindowState = WindowState.Normal,
			X = 100,
			Y = 100
		};

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		Window window = new();

		// Act
		sut.Initialize(
			window,
			windowSettings,
			new CopyHistoryViewSettings());

		// Assert
		window.Width
			.Should()
			.Be(IViewLauncher.DefaultWindowSize.Width);

		window.Height
			.Should()
			.Be(IViewLauncher.DefaultWindowSize.Height);

		sut.NavigationColumnWidth.Value
			.Should()
			.Be(IViewLauncher.DefaultWindowSize.Width / 3.0);
	}

	/// <summary>
	/// <see cref="EditorViewModel.Initialize" />: window position/size/state and view-model properties are set from the supplied settings.
	/// </summary>
	[AvaloniaTest]
	public void Initialize_Initializes_Properties()
	{
		// Arrange
		int positiveValue = RandomValues.CreateInt(100, 300);

		EditorWindowSettings windowSettings = new()
		{
			IsReadOnly = true,
			IsTopmost = true,
			NavigationColumnWidth = positiveValue - 20,
			Size = new(positiveValue, positiveValue),
			WindowState = WindowState.Normal,
			X = positiveValue,
			Y = positiveValue
		};

		FileDto[] historyFiles = [.. ItemDtoFactory.CreateFileDtos(5)];

		CopyHistoryViewSettings copyHistorySettings = new()
		{
			ItemIds = [.. historyFiles.Select(x => x.Id)],
			SelectedItemId = Guid.NewGuid()
		};

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.AddHierarchy(historyFiles);

		Window window = new();

		// Act
		sut.Initialize(
			window,
			windowSettings,
			copyHistorySettings);

		// Assert
		window.Position
			.Should()
			.Be(new PixelPoint(windowSettings.X, windowSettings.Y));

		window.Width
			.Should()
			.Be(windowSettings.Size.Width);

		window.Height
			.Should()
			.Be(windowSettings.Size.Height);

		window.WindowState
			.Should()
			.Be(windowSettings.WindowState);

		window.Topmost
			.Should()
			.Be(windowSettings.IsTopmost);

		sut.NavigationColumnWidth.Value
			.Should()
			.Be(windowSettings.NavigationColumnWidth);

		sut.IsReadOnly
			.Should()
			.Be(windowSettings.IsReadOnly);

		sut.CopyHistorySettings
			.Should()
			.BeEquivalentTo(copyHistorySettings);
	}

	/// <summary>
	/// <see cref="EditorViewModel.Initialize" />: a window closed minimized opens in the normal state.
	/// </summary>
	[AvaloniaTest]
	public void Initialize_Opens_A_Window_Saved_Minimized_In_The_Normal_State()
	{
		// Arrange
		EditorWindowSettings windowSettings = new()
		{
			IsReadOnly = false,
			IsTopmost = false,
			NavigationColumnWidth = 200.0,
			Size = new(700, 500),
			WindowState = WindowState.Minimized,
			X = 100,
			Y = 100
		};

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		Window window = new();

		// Act
		sut.Initialize(
			window,
			windowSettings,
			new CopyHistoryViewSettings());

		// Assert
		window.WindowState
			.Should()
			.Be(WindowState.Normal);
	}

	/// <summary>
	/// <see cref="EditorViewModel.Initialize" />: a window saved at zero or negative coordinates, as at the edge of the screen
	/// or on a screen left of the main one, comes back there.
	/// </summary>
	[AvaloniaTest]
	[TestCase(0, 0)]
	[TestCase(-50, 100)]
	public void Initialize_Restores_A_Window_Saved_At_Zero_Or_Negative_Coordinates(int x, int y)
	{
		// Arrange
		EditorWindowSettings windowSettings = new()
		{
			IsReadOnly = false,
			IsTopmost = false,
			NavigationColumnWidth = 200.0,
			Size = new(700, 500),
			WindowState = WindowState.Normal,
			X = x,
			Y = y
		};

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// The window stands elsewhere, so only the restore brings it to the stored position.
		Window window = new()
		{
			Position = new PixelPoint(200, 150)
		};

		// Act
		sut.Initialize(
			window,
			windowSettings,
			new CopyHistoryViewSettings());

		// Assert
		window.Position
			.Should()
			.Be(new PixelPoint(x, y));
	}

	/// <summary>
	/// <see cref="EditorViewModel.IsRightSideSheetOpened" />: closing the right side sheet saves the item chosen in the copy
	/// history it shows.
	/// </summary>
	[AvaloniaTest]
	public void IsRightSideSheetOpened_Saves_The_Chosen_Copy_History_Item_When_The_Sheet_Closes()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		CopyHistoryViewModel copyHistory = mock.Create<CopyHistoryViewModel>();

		sut.AddHierarchy(files);

		sut
			.CopyHistorySettings
			.AddItemIds(files.Select(x => x.Id), files);

		sut
			.ShowCopyHistoryCommand
			.Execute(null);

		// The view of the side sheet hands its view model over once loaded, and the stored selection comes back through
		// the dispatcher before the choice.
		sut
			.CopyHistoryDisplayedCommand
			.Execute(copyHistory);

		Dispatcher.UIThread.RunJobs();

		copyHistory.SelectedItem = files[1];

		// Act
		sut.IsRightSideSheetOpened = false;

		// Assert
		sut.CopyHistorySettings.SelectedItemId
			.Should()
			.Be(files[1].Id);
	}

	/// <summary>
	/// <see cref="EditorViewModel.NavigationColumnWidth" />: the width is clamped to less than the view width when the view shrinks.
	/// </summary>
	[Test]
	public void NavigationColumnWidth_Is_Clamped_To_Less_Than_The_View_Width()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		const double initialViewWidth = 1000.0;

		sut.ViewWidth = initialViewWidth;

		sut.NavigationColumnWidth = new GridLength(initialViewWidth / 2);

		// Act
		sut.ViewWidth = initialViewWidth / 4;

		// Assert
		sut.NavigationColumnWidth.Value
			.Should()
			.BeLessThan(sut.ViewWidth);
	}

	/// <summary>
	/// <see cref="EditorViewModel.NotifyDecryptedContentsChanged" />: decrypted contents keep the countdown running.
	/// </summary>
	[Test]
	public void NotifyDecryptedContentsChanged_Arms_The_Countdown_For_Decrypted_Contents()
	{
		// Arrange
		IAutoLockService autoLock = Substitute.For<IAutoLockService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(autoLock));

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.AddHierarchy([ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)]);

		// Act
		sut.NotifyDecryptedContentsChanged();

		// Assert
		autoLock
			.Received(1)
			.Arm();

		autoLock
			.DidNotReceive()
			.Stop();
	}

	/// <summary>
	/// <see cref="EditorViewModel.NotifyDecryptedContentsChanged" />: nothing left decrypted stops the countdown.
	/// </summary>
	[Test]
	public void NotifyDecryptedContentsChanged_Stops_The_Countdown_Without_Decrypted_Contents()
	{
		// Arrange
		IAutoLockService autoLock = Substitute.For<IAutoLockService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(autoLock));

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.AddHierarchy([ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted)]);

		// Act
		sut.NotifyDecryptedContentsChanged();

		// Assert
		autoLock
			.Received(1)
			.Stop();

		autoLock
			.DidNotReceive()
			.Arm();
	}

	/// <summary>
	/// <see cref="ViewModelBase.Receive(CloseExecutingFileMessage)" />: only the executing file the message names is removed
	/// from executing files, unmarked, and closed in the engine.
	/// </summary>
	[Test]
	public void Receive_CloseExecutingFile_Closes_Only_The_Named_File()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		dto.IsExecuting = true;

		FileDto other = ItemDtoFactory.CreateFileDto();

		other.IsExecuting = true;

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(engine);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.ExecutingFiles
			.Add(other);

		sut
			.ExecutingFiles
			.Add(dto);

		// Act
		sut.Receive(new CloseExecutingFileMessage(dto.Id));

		// Assert
		sut.ExecutingFiles
			.Should()
			.Equal(other);

		dto.IsExecuting
			.Should()
			.BeFalse();

		other.IsExecuting
			.Should()
			.BeTrue();

		engine
			.Received(1)
			.CloseAsync(dto.Id);

		engine
			.DidNotReceive()
			.CloseAsync(other.Id);
	}

	/// <summary>
	/// <see cref="ViewModelBase.Receive(SessionAutoLockedMessage)" />: the expiry closes the open files and hides every content.
	/// </summary>
	[Test]
	public async Task Receive_SessionAutoLocked_Closes_Files_And_Hides_Contents()
	{
		// Arrange
		FileDto editingFile = ItemDtoFactory.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		FileDto executingFile = ItemDtoFactory.CreateFileDto(
			isExecuting: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		IMessenger messenger = new WeakReferenceMessenger();

		List<Task> scheduled = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler
				.When(static x => x.Watch(Arg.Any<Task>()))
				.Do(callInfo => scheduled.Add(callInfo.Arg<Task>()));

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance(exceptionHandler);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.AddHierarchy([editingFile, executingFile]);

		sut.EditorTabs = new()
		{
			Files = [editingFile],
			PreviousFile = null,
			SelectedFile = editingFile
		};

		// Act
		messenger.Send(new SessionAutoLockedMessage());

		await Task.WhenAll([.. scheduled]);

		// Assert
		editingFile.IsEditing
			.Should()
			.BeFalse();

		executingFile.IsExecuting
			.Should()
			.BeFalse();

		sut.EditorTabs
			.Should()
			.BeNull();

		contentVisibility
			.Received(1)
			.HideAllContents(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="ViewModelBase.Receive(SessionAutoLockedMessage)" />: an editor that cannot persist its changes
	/// does not keep the contents decrypted.
	/// </summary>
	[Test]
	public async Task Receive_SessionAutoLocked_Hides_Contents_When_An_Editor_Cannot_Save()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		IMessenger messenger = new StrongReferenceMessenger();

		object recipient = new();

		messenger.Register<FlushEditorsMessage>(recipient, static (_, message) => message.Reply(false));

		List<Task> scheduled = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler
				.When(static x => x.Watch(Arg.Any<Task>()))
				.Do(callInfo => scheduled.Add(callInfo.Arg<Task>()));

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance(exceptionHandler);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.AddHierarchy([file]);

		// Act
		messenger.Send(new SessionAutoLockedMessage());

		await Task.WhenAll([.. scheduled]);

		// Assert
		file.IsEditing
			.Should()
			.BeFalse();

		contentVisibility
			.Received(1)
			.HideAllContents(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ResetSelectedObject" />: SelectedObject is cleared and the object's IsSelected is reset.
	/// </summary>
	[Test]
	public void ResetSelectedObject_Resets_IsSelected_Property_And_Resets_SelectedObject()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		dto.IsSelected = true;

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.SelectedObject = dto;

		// Act
		sut.ResetSelectedObject();

		// Assert
		sut.SelectedObject
			.Should()
			.BeNull();

		dto.IsSelected
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ResetSelectedObject" />: the object that was selected is saved as not selected.
	/// </summary>
	[Test]
	public void ResetSelectedObject_Saves_The_Object_As_Not_Selected()
	{
		// Arrange
		List<(Guid Id, bool IsSelected)> saved = [];

		FileDto selected = ItemDtoFactory.CreateFileDto();

		selected.IsSelected = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

			propertyWriter.UpdateIsSelectedAsync(
				Arg.Do<ExplorerItemDtoBase>(x => saved.Add((x.Id, x.IsSelected))),
				Arg.Any<CancellationToken>());

			builder.RegisterInstance(propertyWriter);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(selected);

		sut.SelectedObject = selected;

		// Only what the reset saves counts.
		saved.Clear();

		// Act
		sut.ResetSelectedObject();

		// Assert
		saved
			.Should()
			.Equal((selected.Id, false));
	}

	/// <summary>
	/// <see cref="EditorViewModel.RestartApplication" />: the application is shut down and a new process is started.
	/// </summary>
	[Test]
	public void RestartApplication_Restarts_The_Application()
	{
		// Arrange
		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(TypedParameter.From(processManager));

		// Act
		sut.RestartApplication(null);

		// Assert
		sut.IsShutdown
			.Should()
			.BeTrue();

		processManager
			.Received(1)
			.StartProcess(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.RestartAutoLockCommand" />: the countdown starts over from the delay in the settings.
	/// </summary>
	[Test]
	public void RestartAutoLockCommand_Starts_The_Countdown_Over()
	{
		// Arrange
		IAutoLockService autoLock = Substitute.For<IAutoLockService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(autoLock));

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut
			.RestartAutoLockCommand
			.Execute(null);

		// Assert
		autoLock
			.Received(1)
			.Arm();
	}

	/// <summary>
	/// <see cref="EditorViewModel.SelectedObject" />: the object left is cleared and saved as not selected, also when the
	/// tree never showed its row and so did not clear it.
	/// </summary>
	[Test]
	public void SelectedObject_Clears_The_Object_Left_That_The_Tree_Did_Not()
	{
		// Arrange
		List<(Guid Id, bool IsSelected)> saved = [];

		FileDto left = ItemDtoFactory.CreateFileDto();

		FileDto chosen = ItemDtoFactory.CreateFileDto();

		left.IsSelected = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

			propertyWriter.UpdateIsSelectedAsync(
				Arg.Do<ExplorerItemDtoBase>(x => saved.Add((x.Id, x.IsSelected))),
				Arg.Any<CancellationToken>());

			builder.RegisterInstance(propertyWriter);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.AddRange([left, chosen]);

		sut.SelectedObject = left;

		// Act
		// The tree marks the object chosen, while the row of the object left was never shown.
		chosen.IsSelected = true;

		sut.SelectedObject = chosen;

		// Assert
		left.IsSelected
			.Should()
			.BeFalse();

		saved
			.Should()
			.Equal((chosen.Id, true), (left.Id, false));
	}

	/// <summary>
	/// <see cref="EditorViewModel.SelectedObject" />: in read-only mode a selection in the tree is not saved.
	/// </summary>
	[Test]
	public async Task SelectedObject_In_Read_Only_Mode_Saves_No_Selection()
	{
		// Arrange
		FileDto chosen = ItemDtoFactory.CreateFileDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(propertyWriter));

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(chosen);

		sut.IsReadOnly = true;

		// Act
		chosen.IsSelected = true;

		sut.SelectedObject = chosen;

		// Assert
		await propertyWriter.DidNotReceive().UpdateIsSelectedAsync(
			Arg.Any<ExplorerItemDtoBase>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.SelectedObject" />: the selection of an object removed from the tree is not saved, as the
	/// object is no longer in the database.
	/// </summary>
	[Test]
	public async Task SelectedObject_Saves_Nothing_For_An_Object_Removed_From_The_Tree()
	{
		// Arrange
		FileDto removed = ItemDtoFactory.CreateFileDto();

		removed.IsSelected = true;

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(propertyWriter));

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(removed);

		sut.SelectedObject = removed;

		sut.Hierarchy.Remove(removed);

		// Act
		// The tree drops the selection of an object it no longer holds.
		sut.SelectedObject = null;

		// Assert
		await propertyWriter.DidNotReceive().UpdateIsSelectedAsync(
			Arg.Any<ExplorerItemDtoBase>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.SelectedObject" />: a selection moved in the tree saves the object left and the object
	/// chosen, each in its new state.
	/// </summary>
	[Test]
	public void SelectedObject_Saves_The_Object_Left_And_The_Object_Chosen()
	{
		// Arrange
		List<(Guid Id, bool IsSelected)> saved = [];

		FileDto left = ItemDtoFactory.CreateFileDto();

		FileDto chosen = ItemDtoFactory.CreateFileDto();

		left.IsSelected = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

			propertyWriter.UpdateIsSelectedAsync(
				Arg.Do<ExplorerItemDtoBase>(x => saved.Add((x.Id, x.IsSelected))),
				Arg.Any<CancellationToken>());

			builder.RegisterInstance(propertyWriter);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.AddRange([left, chosen]);

		sut.SelectedObject = left;

		// Only what the move of the selection saves counts.
		saved.Clear();

		// Act
		// The tree clears the object left and marks the object chosen before it reports the new selection.
		left.IsSelected = false;

		chosen.IsSelected = true;

		sut.SelectedObject = chosen;

		// Assert
		saved
			.Should()
			.Equal((left.Id, false), (chosen.Id, true));
	}

	/// <summary>
	/// <see cref="EditorViewModel.SelectedObject" />: while an action runs, a selection in the tree is not saved.
	/// </summary>
	[Test]
	public async Task SelectedObject_While_An_Action_Runs_Saves_No_Selection()
	{
		// Arrange
		FileDto chosen = ItemDtoFactory.CreateFileDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(propertyWriter));

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(chosen);

		sut.Receive(new ShowProgressBarMessage(true));

		// Act
		chosen.IsSelected = true;

		sut.SelectedObject = chosen;

		// Assert
		await propertyWriter.DidNotReceive().UpdateIsSelectedAsync(
			Arg.Any<ExplorerItemDtoBase>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.SetFavorite" />: the IsFavorite flag is toggled and the change is delegated to the property writer.
	/// </summary>
	[Test]
	public async Task SetFavorite_Toggles_And_Delegates_To_Property_Writer([Values] bool initialValue)
	{
		// Arrange
		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		FileDto dto = ItemDtoFactory.CreateFileDto();

		dto.IsFavorite = initialValue;

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(TypedParameter.From(propertyWriter));

		// Act
		await sut.SetFavorite(dto);

		// Assert
		dto.IsFavorite
			.Should()
			.NotBe(initialValue);

		await propertyWriter
			.Received(1)
			.UpdateIsFavoriteAsync(dto, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.SetSelectedObject" />: the object selected before is saved as not selected, so one object
	/// at most stays selected in the database.
	/// </summary>
	[Test]
	public void SetSelectedObject_Saves_The_Object_Left_As_Not_Selected()
	{
		// Arrange
		List<(Guid Id, bool IsSelected)> saved = [];

		FileDto left = ItemDtoFactory.CreateFileDto();

		FileDto chosen = ItemDtoFactory.CreateFileDto();

		left.IsSelected = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

			propertyWriter.UpdateIsSelectedAsync(
				Arg.Do<ExplorerItemDtoBase>(x => saved.Add((x.Id, x.IsSelected))),
				Arg.Any<CancellationToken>());

			builder.RegisterInstance(propertyWriter);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.AddRange([left, chosen]);

		sut.SelectedObject = left;

		// Act
		sut.SetSelectedObject(chosen);

		// Assert
		saved
			.Should()
			.Equal((chosen.Id, true), (left.Id, false));
	}

	/// <summary>
	/// <see cref="EditorViewModel.SetSelectedObject" />: the object's IsSelected is set to true and it becomes the SelectedObject.
	/// </summary>
	[Test]
	public void SetSelectedObject_Sets_Object_IsSelected_Property_To_True_And_SelectedObject()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		dto.IsSelected = false;

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.SetSelectedObject(dto);

		// Assert
		dto.IsSelected
			.Should()
			.BeTrue();

		sut.SelectedObject
			.Should()
			.Be(dto);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowExecutingFilesCommand" />: a switch of the right side sheet to the executing files
	/// saves the item chosen in the copy history it showed.
	/// </summary>
	[AvaloniaTest]
	public void ShowExecutingFilesCommand_Saves_The_Chosen_Copy_History_Item()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		CopyHistoryViewModel copyHistory = mock.Create<CopyHistoryViewModel>();

		sut.AddHierarchy(files);

		sut
			.CopyHistorySettings
			.AddItemIds(files.Select(x => x.Id), files);

		sut
			.ShowCopyHistoryCommand
			.Execute(null);

		// The view of the side sheet hands its view model over once loaded, and the stored selection comes back through
		// the dispatcher before the choice.
		sut
			.CopyHistoryDisplayedCommand
			.Execute(copyHistory);

		Dispatcher.UIThread.RunJobs();

		copyHistory.SelectedItem = files[1];

		// Act
		sut
			.ShowExecutingFilesCommand
			.Execute(null);

		// Assert
		sut.CopyHistorySettings.SelectedItemId
			.Should()
			.Be(files[1].Id);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowFavorites" />: the favorites window gets the tabs of the editor with the selected
	/// one and the previous one.
	/// </summary>
	[AvaloniaTest]
	public void ShowFavorites_Passes_The_Editor_Tabs()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(count: 3)];

		EditorTabsState? passed = null;

		FavoritesWindow? favoritesWindow = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

			favoritesWindow = windowMock.Create<FavoritesWindow>();

			viewLauncher.CreateFavoritesWindow(
				Arg.Any<IEnumerable<ExplorerItemDtoBase>>(),
				Arg.Do<EditorTabsState?>(x => passed = x),
				Arg.Any<IEnumerable<FileDto>>())
			.Returns(favoritesWindow);

			builder.RegisterInstance(viewLauncher);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		EditingFilesViewModel editingFiles = mock.Create<EditingFilesViewModel>();

		editingFiles
			.Items
			.AddRange(files);

		editingFiles.SelectedIndex = 1;

		editingFiles.PreviousFile = files[2];

		sut
			.EditingFilesViewLoadedCommand
			.Execute(editingFiles);

		// Act
		sut.ShowFavorites(null);

		// Closed here, otherwise its dialog host stays in the list every headless test shares.
		favoritesWindow?.Close();

		// Assert
		passed!.Files
			.Should()
			.Equal(files);

		passed.SelectedFile
			.Should()
			.BeSameAs(files[1]);

		passed.PreviousFile
			.Should()
			.BeSameAs(files[2]);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowFavorites" />: the favorites window is configured and shown without shutting down the application.
	/// </summary>
	[AvaloniaTest]
	public void ShowFavorites_Shows_Favorites_Window()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		FavoritesWindow? favoritesWindow = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			favoritesWindow = windowMock.Create<FavoritesWindow>();

			viewLauncher.CreateFavoritesWindow(
				Arg.Any<IEnumerable<ExplorerItemDtoBase>>(),
				Arg.Any<EditorTabsState?>(),
				Arg.Any<IEnumerable<FileDto>>())
			.Returns(favoritesWindow);

			builder.RegisterInstance(viewLauncher);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.ShowFavorites(null);

		// Closed here, otherwise its dialog host stays in the list every headless test shares.
		favoritesWindow?.Close();

		// Assert
		sut.IsShutdown
			.Should()
			.BeFalse();

		viewLauncher.Received(1).CreateFavoritesWindow(
			Arg.Any<IEnumerable<ExplorerItemDtoBase>>(),
			Arg.Any<EditorTabsState?>(),
			Arg.Any<IEnumerable<FileDto>>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowFolderContents" />: open files are closed and the folder's contents are shown.
	/// </summary>
	[Test]
	public async Task ShowFolderContents_Closes_Open_Files_And_Shows_The_Contents()
	{
		// Arrange
		FileDto[] editingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isEditing: true)];

		FileDto[] executingFiles = [.. ItemDtoFactory.CreateFileDtos(
			count: 5,
			isExecuting: true)];

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(contentVisibility);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ShowFolderContents(folder);

		// Assert
		editingFiles
			.Should()
			.OnlyContain(x => !x.IsEditing);

		executingFiles
			.Should()
			.OnlyContain(x => !x.IsExecuting);

		await contentVisibility
			.Received(1)
			.ShowFolderContentsAsync(Arg.Any<FolderDto>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowFolderContents" />: nothing happens when the folder has no files to close.
	/// </summary>
	[Test]
	public async Task ShowFolderContents_Does_Nothing_If_Missing_Files()
	{
		// Arrange
		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(TypedParameter.From(dialogService));

		// Act
		await sut.ShowFolderContents(ItemDtoFactory.CreateFolderDto());

		// Assert
		await dialogService
			.DidNotReceive()
			.RequestCloseFilesAsync();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowInEditorAsync" />: the object is selected only once the window has loaded, as
	/// a selection made before would not be saved.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowInEditorAsync_Selects_The_Object_Once_The_Window_Has_Loaded()
	{
		// Arrange
		List<bool> isLoadedAtSelection = [];

		FileDto shown = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(shown);

		EditorWindow window = new(sut);

		using IDisposable selections = sut
			.WhenPropertyChanged(x => x.SelectedObject, notifyOnInitialValue: false)
			.Subscribe(_ => isLoadedAtSelection.Add(window.IsLoaded));

		// The window lays out its tree at once and loads only when the dispatcher gets to it.
		window.Show();

		// Act
		_ = sut.ShowInEditorAsync(shown.Id, window);

		await WaitUntilAsync(() => sut.SelectedObject == shown);

		window.Close();

		// Assert
		isLoadedAtSelection
			.Should()
			.Equal(true);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Lets the UI thread run until <paramref name="condition" /> holds, a few seconds at most.
	/// </summary>
	private static async Task WaitUntilAsync(Func<bool> condition)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();

		while (!condition() && stopwatch.Elapsed < TimeSpan.FromSeconds(5.0))
		{
			await Task.Delay(20);

			Dispatcher.UIThread.RunJobs();
		}
	}
	#endregion
}
