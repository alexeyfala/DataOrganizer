using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities;
using DataOrganizer.DTO.Execution;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Messages;
using DataOrganizer.UnitTests.Helpers;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using Entities.Enums;
using NSubstitute;
using Repository.DTO;
using Repository.Interfaces;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(EditorViewModel)}"" type")]
internal class EditorViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="EditorViewModel.AddAsync" />: delegates to the hierarchy editor and, on success, refreshes the object count.
	/// </summary>
	[Test]
	public async Task AddAsync_Delegates_To_Hierarchy_Editor(
		[Values] EntityType type,
		[Values] bool hasParent)
	{
		// Arrange
		IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

		ExplorerModelBaseDto created = Substitute.For<ExplorerModelBaseDto>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			hierarchyEditor
				.AddAsync(
					Arg.Any<string>(),
					Arg.Any<EntityType>(),
					Arg.Any<FolderModelDto>(),
					Arg.Any<Collection<ExplorerModelBaseDto>>(),
					Arg.Any<CancellationToken>())
				.Returns(created);

			builder.RegisterInstance(hierarchyEditor);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		FolderModelDto? parent = hasParent ? TestUtils.CreateFolderDto() : null;

		string name = AppUtils.CreateRandomString(10);

		// Act
		ExplorerModelBaseDto? entity = await sut.AddAsync(name, type, parent);

		// Assert
		entity
			.Should()
			.BeSameAs(created);

		await hierarchyEditor.Received().AddAsync(
			name,
			type,
			parent,
			sut.Hierarchy,
			Arg.Any<CancellationToken>());

		sut.BottomLeftCornerInfo
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

		ExplorerModelBaseDto[] hierarchy = [.. TestUtils.CreateFoldersDto(5).Concat<ExplorerModelBaseDto>(TestUtils.CreateFilesDto(5))];

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
	public async Task ChangePassword_Does_Work()
	{
		// Arrange
		FileModelDto[] editingFiles = [.. TestUtils.CreateFilesDto(
			count: 5,
			isEditing: true)];

		FileModelDto[] executingFiles = [.. TestUtils.CreateFilesDto(
			count: 5,
			isExecuting: true)];

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
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

		await entityEncryption
			.Received()
			.ChangePasswordAsync(Arg.Any<FolderModelDto>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.CloseExecutingFile" />: the file is removed from executing files, unmarked, and closed in the engine.
	/// </summary>
	[Test]
	public void CloseExecutingFile_Closes_File()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		dto.IsExecuting = true;

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(
			TypedParameter.From(engine),
			TypedParameter.From<IDispatcherAccessor>(new InlineDispatcherAccessor()));

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
			.Received()
			.CloseAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.CloseFiles" />: both editing and executing files are unmarked.
	/// </summary>
	[Test]
	public void CloseFiles_Closes_Editing_And_Executing_Files()
	{
		// Arrange
		FileModelDto[] editingFiles = [.. TestUtils.CreateFilesDto(2)];

		FileModelDto[] executingFiles = [.. TestUtils.CreateFilesDto(2)];

		editingFiles.ForEach(x => x.IsEditing = true);

		executingFiles.ForEach(x => x.IsExecuting = true);

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(
			TypedParameter.From<IDispatcherAccessor>(new InlineDispatcherAccessor()));

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
		await sut.DecryptFolder(TestUtils.CreateFolderDto());

		// Assert
		await dialogService
			.DidNotReceive()
			.RequestCloseFilesAsync();
	}

	/// <summary>
	/// <see cref="EditorViewModel.DecryptFolder" />: open files are closed and the folder is decrypted.
	/// </summary>
	[Test]
	public async Task DecryptFolder_Does_Work()
	{
		// Arrange
		FileModelDto[] editingFiles = [.. TestUtils.CreateFilesDto(
			count: 5,
			isEditing: true)];

		FileModelDto[] executingFiles = [.. TestUtils.CreateFilesDto(
			count: 5,
			isExecuting: true)];

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
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

		await entityEncryption
			.Received()
			.DecryptFolderAsync(Arg.Any<FolderModelDto>(), Arg.Any<FileModelDto[]>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.DeleteAsync" />: on success the deleted file is closed and removed from executing files.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Closes_File_On_Success()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(isExecuting: true);

		IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

		hierarchyEditor
			.DeleteAsync(
				Arg.Any<ExplorerModelBaseDto>(),
				Arg.Any<Collection<ExplorerModelBaseDto>>(),
				Arg.Any<CancellationToken>())
			.Returns(true);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(hierarchyEditor);

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
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

		await hierarchyEditor.Received().DeleteAsync(
			file,
			sut.Hierarchy,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.DeleteAsync" />: when the editor reports failure the file is left open.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Keeps_File_When_Editor_Fails()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(isExecuting: true);

		IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

		hierarchyEditor
			.DeleteAsync(
				Arg.Any<ExplorerModelBaseDto>(),
				Arg.Any<Collection<ExplorerModelBaseDto>>(),
				Arg.Any<CancellationToken>())
			.Returns(false);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(hierarchyEditor));

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
	/// <see cref="EditorViewModel.EditNote" />: the note of the dialog is stored through the note editor.
	/// </summary>
	[Test]
	public async Task EditNote_Delegates_To_Note_Editor()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto();

		string storedNote = AppUtils.CreateRandomString(20);

		string editedNote = AppUtils.CreateRandomString(20);

		IDialogService dialogService = Substitute.For<IDialogService>();

		INoteEditor noteEditor = Substitute.For<INoteEditor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dialogService
				.RequestMultilineTextAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>())
				.Returns(new ValueIsValidPair(true, editedNote));

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
			Arg.Any<CancellationToken>());

		await noteEditor.Received(1).EditAsync(
			file,
			editedNote,
			Arg.Any<DateTime>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.EditNote" />: a cancelled dialog leaves the note untouched.
	/// </summary>
	[Test]
	public async Task EditNote_Keeps_Note_When_Dialog_Is_Cancelled()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto();

		INoteEditor noteEditor = Substitute.For<INoteEditor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestMultilineTextAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>())
				.Returns(new ValueIsValidPair());

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(noteEditor);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.EditNote(file);

		// Assert
		await noteEditor.DidNotReceive().EditAsync(
			Arg.Any<ExplorerModelBaseDto>(),
			Arg.Any<string>(),
			Arg.Any<DateTime>(),
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
			.CanExecute(TestUtils.CreateFileDto(encryptionStatus: encryptionStatus));

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
		await sut.EncryptFolder(TestUtils.CreateFolderDto());

		// Assert
		await dialogService
			.DidNotReceive()
			.RequestCloseFilesAsync();
	}

	/// <summary>
	/// <see cref="EditorViewModel.EncryptFolder" />: open files are closed and the folder is encrypted.
	/// </summary>
	[Test]
	public async Task EncryptFolder_Does_Work()
	{
		// Arrange
		FileModelDto[] editingFiles = [.. TestUtils.CreateFilesDto(
			count: 5,
			isEditing: true)];

		FileModelDto[] executingFiles = [.. TestUtils.CreateFilesDto(
			count: 5,
			isExecuting: true)];

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
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

		await entityEncryption
			.Received()
			.EncryptFolderAsync(Arg.Any<FolderModelDto>(), Arg.Any<FileModelDto[]>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ExecuteFile" />: contents are not loaded when the file is already executing.
	/// </summary>
	[Test]
	public async Task ExecuteFile_Contents_Should_Not_Be_Loaded_If_It_Is_Already_Opened()
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

			builder.RegisterInstance(engine);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ExecuteFile(TestUtils.CreateFileDto());

		// Assert
		await dbAccess
			.DidNotReceive()
			.GetFileContentsAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ExecuteFile" />: the file is marked executing, added to executing files, and executed by the engine.
	/// </summary>
	[Test]
	public async Task ExecuteFile_Does_Work()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			engine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>())
				.Returns(true);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ContentsIsValidPair pair = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(pair);

			builder.RegisterInstance(dbAccess);

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
			.Received()
			.ExecuteAsync(Arg.Any<ExecuteFileParameters>());
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
	public async Task ExpandCollapseAllFoldersAsync_Should_Act_To_All_Folders([Values] bool isExpandAll)
	{
		// Arrange
		FolderModelDto selectedFolder = TestUtils.CreateFolderDto();

		selectedFolder.IsSelected = true;

		FolderModelDto[] folders = [.. TestUtils.CreateFoldersDto(5)];

		folders = [.. folders, .. selectedFolder.ToEnumerable()];

		folders
			.ForEach(x => x.Children.AddRange(TestUtils.CreateFoldersDto(5)))
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
	/// <see cref="EditorViewModel.HandleChangeSettingsAsync" />: a changed auto-lock delay restarts the countdown at once.
	/// </summary>
	[Test]
	public async Task HandleChangeSettingsAsync_Applies_A_Changed_Auto_Lock_Delay()
	{
		// Arrange
		IAutoLockService autoLock = Substitute.For<IAutoLockService>();

		AppSettings saved = TestUtils.CreateRandomSettings();

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

		sut.AddHierarchy([TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)]);

		AppSettings changed = TestUtils.CreateRandomSettings();

		changed.AutoLockMinutes = 5;

		// Act
		await sut.HandleChangeSettingsAsync(true, changed);

		// Assert
		autoLock
			.Received(1)
			.Arm();
	}

	/// <summary>
	/// <see cref="EditorViewModel.HandleChangeSettingsAsync" />: on save hotkeys are restarted and settings persisted, otherwise the material theme is reapplied.
	/// </summary>
	[Test]
	public async Task HandleChangeSettingsAsync_Handles_Bussiness_Logic_After_Settings_Changing([Values] bool isSave)
	{
		// Arrange
		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		IAppThemeService themeService = Substitute.For<IAppThemeService>();

		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		AppSettings settings = TestUtils.CreateRandomSettings(trackHotkeys: true);

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

		// Act
		await sut.HandleChangeSettingsAsync(isSave, settings);

		// Assert
		if (isSave)
		{
			await hook
				.Received()
				.StopTrackingAsync();

			await hook
				.Received()
				.StartTrackingAsync(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());

			settingsStore
				.Received()
				.Overwrite(Arg.Any<AppSettings>());

			settingsStore
				.Received()
				.Save();
		}
		else
		{
			themeService
				.Received()
				.ApplyMaterialTheme();

		}
	}

	/// <summary>
	/// <see cref="EditorViewModel.HandleChangeSettingsAsync" />: an unchanged auto-lock delay leaves the countdown running.
	/// </summary>
	[Test]
	public async Task HandleChangeSettingsAsync_Keeps_An_Unchanged_Auto_Lock_Delay()
	{
		// Arrange
		IAutoLockService autoLock = Substitute.For<IAutoLockService>();

		AppSettings settings = TestUtils.CreateRandomSettings();

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

		sut.AddHierarchy([TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)]);

		// Act
		await sut.HandleChangeSettingsAsync(true, settings);

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
	public async Task HideAllFileContents_Does_Work()
	{
		// Arrange
		FileModelDto[] editingFiles = [.. TestUtils.CreateFilesDto(
			count: 5,
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted)];

		FileModelDto[] executingFiles = [.. TestUtils.CreateFilesDto(
			count: 5,
			isExecuting: true,
			encryptionStatus: EncryptionStatus.Decrypted)];

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
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
		entityEncryption
			.Received(1)
			.HideAllContents(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.HideAllFileContents" />: nothing is hidden and no file is closed while an editor
	/// cannot persist its changes, so hiding never discards them.
	/// </summary>
	[Test]
	public async Task HideAllFileContents_Keeps_Contents_When_An_Editor_Cannot_Save()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		IMessenger messenger = new StrongReferenceMessenger();

		object recipient = new();

		messenger.Register<FlushEditorsMessage>(recipient, static (_, message) => message.Reply(false));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.AddHierarchy([file]);

		// Act
		await sut.HideAllFileContents();

		// Assert
		file.IsEditing
			.Should()
			.BeTrue();

		entityEncryption
			.DidNotReceive()
			.HideAllContents(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.HideFileContents" />: the file is closed and its contents marked encrypted.
	/// </summary>
	[Test]
	public async Task HideFileContents_Does_Work([Values] bool isEditing)
	{
		// Arrange
		FileModelDto file = isEditing
			? TestUtils.CreateFileDto(isEditing: true)
			: TestUtils.CreateFileDto(isExecuting: true);

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.HideFileContents(file);

		// Assert
		// Marking the object and dropping the key belongs to the encryption service.
		entityEncryption
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
		FileModelDto file = TestUtils.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		IDialogService dialogService = Substitute.For<IDialogService>();

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		IMessenger messenger = new StrongReferenceMessenger();

		object recipient = new();

		messenger.Register<FlushEditorsMessage>(recipient, static (_, message) => message.Reply(false));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.HideFileContents(file);

		// Assert
		await dialogService
			.DidNotReceive()
			.RequestCloseFilesAsync();

		entityEncryption
			.DidNotReceive()
			.HideFileContents(Arg.Any<FileModelDto>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.HideFolderContents" />: the folder's open files are closed and its contents hidden.
	/// </summary>
	[Test]
	public async Task HideFolderContents_Does_Work()
	{
		// Arrange
		FileModelDto[] editingFiles = [.. TestUtils.CreateFilesDto(
			count: 5,
			isEditing: true)];

		FileModelDto[] executingFiles = [.. TestUtils.CreateFilesDto(
			count: 5,
			isExecuting: true)];

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
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

		entityEncryption
			.Received()
			.HideFolderContents(Arg.Any<FolderModelDto>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.HideFolderContents" />: a folder is not hidden while an editor cannot persist its changes.
	/// </summary>
	[Test]
	public async Task HideFolderContents_Keeps_Contents_When_An_Editor_Cannot_Save()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		FileModelDto file = TestUtils.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		folder
			.Children
			.Add(file);

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		IMessenger messenger = new StrongReferenceMessenger();

		object recipient = new();

		messenger.Register<FlushEditorsMessage>(recipient, static (_, message) => message.Reply(false));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.HideFolderContents(folder);

		// Assert
		file.IsEditing
			.Should()
			.BeTrue();

		entityEncryption
			.DidNotReceive()
			.HideFolderContents(Arg.Any<FolderModelDto>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: the current hierarchy is passed to the data exchange service for import.
	/// </summary>
	[Test]
	public async Task Import_Does_Work()
	{
		// Arrange
		FileModelDto[] editingFiles = [.. TestUtils.CreateFilesDto(
			count: 5,
			isEditing: true)];

		FileModelDto[] executingFiles = [.. TestUtils.CreateFilesDto(
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
			.Received()
			.ImportDataAsync(Arg.Any<Collection<ExplorerModelBaseDto>>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Initialize" />: window position/size/state and view-model properties are set from the supplied settings.
	/// </summary>
	[AvaloniaTest]
	public void Initialize_Initializes_Properties()
	{
		// Arrange
		int positiveValue = TestUtils.CreateRandomInt(100, 300);

		EditorWindowSettings windowSettings = new()
		{
			IsReadOnly = true,
			NavigationColumnWidth = positiveValue - 20,
			Size = new(positiveValue, positiveValue),
			WindowState = WindowState.Normal,
			X = positiveValue,
			Y = positiveValue
		};

		FileModelDto[] historyFiles = [.. TestUtils.CreateFilesDto(5)];

		CopyHistoryViewSettings copyHistorySettings = new()
		{
			Items = [.. historyFiles.Select(x => x.Id)],
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
		window.Position.X
			.Should()
			.Be(windowSettings.X);

		window.Position.Y
			.Should()
			.Be(windowSettings.Y);

		window.Width
			.Should()
			.Be(windowSettings.Size.Width);

		window.Height
			.Should()
			.Be(windowSettings.Size.Height);

		window.WindowState
			.Should()
			.Be(windowSettings.WindowState);

		sut.NavigationColumnWidth.Value
			.Should()
			.Be(windowSettings.NavigationColumnWidth);

		sut.IsReadOnly
			.Should()
			.Be(windowSettings.IsReadOnly);

		sut.CopyHistorySettings.SelectedItemId
			.Should()
			.Be(copyHistorySettings.SelectedItemId);

		sut.CopyHistorySettings.Items
			.Should()
			.Contain(copyHistorySettings.Items);
	}

	/// <summary>
	/// <see cref="EditorViewModel.NavigationColumnWidth" />: the width is clamped to less than the view width when the view shrinks.
	/// </summary>
	[Test]
	public void NavigationColumnWidth_Should_Be_Less_Than_The_Window_Width()
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

		sut.AddHierarchy([TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)]);

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

		sut.AddHierarchy([TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted)]);

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
	/// <see cref="ViewModelBase.Receive(SessionAutoLockedMessage)" />: the expiry closes the open files and hides every content.
	/// </summary>
	[Test]
	public async Task Receive_SessionAutoLocked_Closes_Files_And_Hides_Contents()
	{
		// Arrange
		FileModelDto editingFile = TestUtils.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		FileModelDto executingFile = TestUtils.CreateFileDto(
			isExecuting: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		IMessenger messenger = new WeakReferenceMessenger();

		List<Task> scheduled = [];

		ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

		exceptionHandler
			.When(static x => x.Watch(Arg.Any<Task>()))
			.Do(callInfo => scheduled.Add(callInfo.Arg<Task>()));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance(exceptionHandler);

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.AddHierarchy([editingFile, executingFile]);

		sut
			.OpenedInEditorFiles
			.Add(editingFile);

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

		sut.OpenedInEditorFiles
			.Should()
			.BeEmpty();

		entityEncryption
			.Received(1)
			.HideAllContents(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());
	}

	/// <summary>
	/// <see cref="ViewModelBase.Receive(SessionAutoLockedMessage)" />: an editor that cannot persist its changes
	/// does not keep the contents decrypted.
	/// </summary>
	[Test]
	public async Task Receive_SessionAutoLocked_Hides_Contents_When_An_Editor_Cannot_Save()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		IMessenger messenger = new StrongReferenceMessenger();

		object recipient = new();

		messenger.Register<FlushEditorsMessage>(recipient, static (_, message) => message.Reply(false));

		List<Task> scheduled = [];

		ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

		exceptionHandler
			.When(static x => x.Watch(Arg.Any<Task>()))
			.Do(callInfo => scheduled.Add(callInfo.Arg<Task>()));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance(exceptionHandler);

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
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

		entityEncryption
			.Received(1)
			.HideAllContents(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ResetSelectedObject" />: SelectedObject is cleared and the object's IsSelected is reset.
	/// </summary>
	[Test]
	public void ResetSelectedObject_Resets_IsSelected_Property_And_Resets_SelectedObject()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

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
	/// <see cref="EditorViewModel.RestartApplication" />: the application is shut down and a new process is started.
	/// </summary>
	[Test]
	public void RestartApplication_Restarts_The_Application()
	{
		// Arrange
		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(TypedParameter.From(processUtils));

		// Act
		sut.RestartApplication(null);

		// Assert
		sut.IsShutdown
			.Should()
			.BeTrue();

		processUtils
			.Received()
			.StartProcess(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.SetFavorite" />: the IsFavorite flag is toggled and the change is delegated to the property writer.
	/// </summary>
	[Test]
	public async Task SetFavorite_Toggles_And_Delegates_To_Property_Writer([Values] bool initialValue)
	{
		// Arrange
		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		FileModelDto dto = TestUtils.CreateFileDto();

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
			.Received()
			.UpdateIsFavoriteAsync(dto, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.SetSelectedObject" />: the object's IsSelected is set to true and it becomes the SelectedObject.
	/// </summary>
	[Test]
	public void SetSelectedObject_Sets_Object_IsSelected_Property_To_True_And_SelectedObject()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

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
	/// <see cref="EditorViewModel.ShowFavorites" />: the favorites window is configured and shown without shutting down the application.
	/// </summary>
	[AvaloniaTest]
	public void ShowFavorites_Shows_Favorites_Window()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock windowMock = AutoMock.GetLoose();

			viewLauncher.ConfigureFavoritesWindow(
				Arg.Any<IEnumerable<ExplorerModelBaseDto>>(),
				Arg.Any<IEnumerable<FileModelDto>>(),
				Arg.Any<IEnumerable<FileModelDto>>())
			.Returns(windowMock.Create<FavoritesWindow>());

			builder.RegisterInstance(viewLauncher);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.ShowFavorites(null);

		// Assert
		sut.IsShutdown
			.Should()
			.BeFalse();

		viewLauncher.Received().ConfigureFavoritesWindow(
			Arg.Any<IEnumerable<ExplorerModelBaseDto>>(),
			Arg.Any<IEnumerable<FileModelDto>>(),
			Arg.Any<IEnumerable<FileModelDto>>());
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
		await sut.ShowFolderContents(TestUtils.CreateFolderDto());

		// Assert
		await dialogService
			.DidNotReceive()
			.RequestCloseFilesAsync();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowFolderContents" />: open files are closed and the folder's contents are shown.
	/// </summary>
	[Test]
	public async Task ShowFolderContents_Does_Work()
	{
		// Arrange
		FileModelDto[] editingFiles = [.. TestUtils.CreateFilesDto(
			count: 5,
			isEditing: true)];

		FileModelDto[] executingFiles = [.. TestUtils.CreateFilesDto(
			count: 5,
			isExecuting: true)];

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(entityEncryption);

			builder.RegisterInstance<IDispatcherAccessor>(new InlineDispatcherAccessor());
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

		await entityEncryption
			.Received()
			.ShowFolderContentsAsync(Arg.Any<FolderModelDto>());
	}
	#endregion
}
