using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using DataOrganizer.Windows;
using Entities.Abstract;
using Entities.Enums;
using Entities.Models;
using MapsterMapper;
using NSubstitute;
using Repository.DTO;
using Repository.Interfaces;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EditorViewModel)}"" type")]
internal class EditorViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="EditorViewModel.AddAsync(string, EntityType, FolderModelDto?, CancellationToken)" />.
	/// </summary>
	[TestCase(EntityType.Folder, false)]
	[TestCase(EntityType.File, true)]
	public async Task AddAsync_Returns_Entity(EntityType type, bool hasParent)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.AddEntityAsync(Arg.Any<AddEntityParameters>())
				.Returns(Substitute.For<ExplorerModelBase>());

			builder.RegisterInstance(dbAccess);

			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Map<ExplorerModelBase, ExplorerModelBaseDto>(Arg.Any<ExplorerModelBase>())
				.Returns(Substitute.For<ExplorerModelBaseDto>());

			builder.RegisterInstance(mapper);
		});

		FolderModelDto? parent = null;

		if (hasParent)
		{
			parent = new()
			{
				Id = Guid.NewGuid(),
				CreatedDate = default,
				EntityType = EntityType.Folder,
				Index = 0,
				UpdatedDate = default
			};
		}

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		ExplorerModelBaseDto? entity = await sut.AddAsync(AppUtils.CreateRandomString(10), type, parent);

		// Assert
		entity
			.Should()
			.NotBeNull();

		if (parent is null)
		{
			return;
		}

		entity.Parent
			.Should()
			.BeSameAs(parent);

		parent.Children
			.Should()
			.Contain(entity);

		parent.IsExpanded
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.AddHierarchy(IEnumerable{ExplorerModelBaseDto})" />.
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
	/// Test of <see cref="EditorViewModel.ClearExecutedFilesView" />.
	/// </summary>
	[Test]
	public void ClearExecutedFilesView_Clears_ExecutedFilesView()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.IsRightSideSheetOpened = true;

		ExecutedFilesView view = mock.Create<ExecutedFilesView>();

		sut.RightSideSheetContent = view;

		// Act
		sut.ClearExecutedFilesView();

		// Assert
		view.ViewModel.ExecutedFiles
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.CloseExecutedFile(FileModelDto)" />.
	/// </summary>
	[Test]
	public void CloseExecutedFile_Closes_File()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		dto.IsExecuted = true;

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(TypedParameter.From(engine));

		sut
			.ExecutedFiles
			.Add(dto);

		// Act
		sut.CloseExecutedFile(dto);

		// Assert
		sut.ExecutedFiles
			.Should()
			.NotContain(dto);

		dto.IsExecuted
			.Should()
			.BeFalse();

		engine
			.Received()
			.CloseAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.DeleteAsync(ExplorerModelBaseDto, CancellationToken)" />.
	/// </summary>
	[TestCase(EntityType.Folder)]
	[TestCase(EntityType.File)]
	public async Task DeleteAsync_Deletes_Entity_In_Database_And_In_Treeview(EntityType type)
	{
		// Arrange
		ExplorerModelBaseDto toBeDeleted = type switch
		{
			EntityType.Folder => TestUtils.CreateFolderDto(),
			EntityType.File => TestUtils.CreateFileDto(isExecuted: true),
			_ => throw new NotImplementedException()
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			if (type == EntityType.Folder)
			{
				dbAccess
					.DeleteFolderAsync(toBeDeleted.Id)
					.Returns(true);
			}
			else
			{
				dbAccess
					.DeleteFileAsync(toBeDeleted.Id)
					.Returns(true);
			}

			builder.RegisterInstance(dbAccess);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.AddRange(toBeDeleted.ToEnumerable(TestUtils.CreateFoldersDto(5)));

		if (type != EntityType.Folder)
		{
			sut
				.ExecutedFiles
				.Add((FileModelDto)toBeDeleted);
		}

		// Act
		bool result = await sut.DeleteAsync(toBeDeleted);

		// Assert
		result
			.Should()
			.BeTrue();

		sut.Hierarchy
			.Should()
			.NotContain(toBeDeleted);

		if (toBeDeleted is not FileModelDto file)
		{
			return;
		}

		sut.ExecutedFiles
			.Should()
			.NotContain(file);
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.DeleteAsync(ExplorerModelBaseDto, CancellationToken)" />.
	/// </summary>
	[TestCase(EntityType.Folder)]
	[TestCase(EntityType.File)]
	public async Task DeleteAsync_Should_Not_Delete_Entity_In_Database_And_In_Treeview(EntityType type)
	{
		// Arrange
		ExplorerModelBaseDto entity = type switch
		{
			EntityType.Folder => TestUtils.CreateFolderDto(),
			EntityType.File => TestUtils.CreateFileDto(),
			_ => throw new NotImplementedException()
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			if (type == EntityType.Folder)
			{
				dbAccess
					.DeleteFolderAsync(entity.Id)
					.Returns(false);
			}
			else
			{
				dbAccess
					.DeleteFileAsync(entity.Id)
					.Returns(false);
			}

			builder.RegisterInstance(dbAccess);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.AddRange(entity.ToEnumerable(TestUtils.CreateFoldersDto(5)));

		// Act
		bool result = await sut.DeleteAsync(entity);

		// Assert
		result
			.Should()
			.BeFalse();

		sut.Hierarchy
			.Should()
			.Contain(entity);
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.DisplayCopyHistory" />.
	/// </summary>
	[AvaloniaTest]
	public void DisplayCopyHistory_Displays_CopyHistoryView_In_Right_Side_Sheet()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			using AutoMock mock = AutoMock.GetLoose();

			viewFactory
				.CreateUserControl<CopyHistoryView>()
				.Returns(mock.Create<CopyHistoryView>());

			builder.RegisterInstance(viewFactory);

		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.DisplayCopyHistory();

		// Assert
		sut.RightSideSheetContent
			.Should()
			.BeOfType<CopyHistoryView>();

		sut.RightSideSheetContentType
			.Should()
			.Be(EditorRightSideSheetContentType.CopyHistory);

		sut.IsRightSideSheetOpened
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.DisplayExecutedFiles" />.
	/// </summary>
	[Test]
	public void DisplayExecutedFiles_Displays_ExecutedFilesView_In_Right_Side_Sheet()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			using AutoMock mock = AutoMock.GetLoose();

			viewFactory
				.CreateUserControl<ExecutedFilesView>()
				.Returns(mock.Create<ExecutedFilesView>());

			builder.RegisterInstance(viewFactory);

		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.DisplayExecutedFiles();

		// Assert
		sut.RightSideSheetContent
			.Should()
			.BeOfType<ExecutedFilesView>();

		((ExecutedFilesView)sut.RightSideSheetContent).ViewModel.ExecutedFiles
			.Should()
			.BeSameAs(sut.ExecutedFiles);

		sut.RightSideSheetContentType
			.Should()
			.Be(EditorRightSideSheetContentType.ExecutedFiles);

		sut.IsRightSideSheetOpened
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.EncryptFilesAsync" />.
	/// </summary>
	[Test]
	public async Task EncryptFilesAsync_Does_Nothing_If_Db_Returns_Invalid_Contents()
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: false).ToAsyncEnumerable());

			builder.RegisterInstance(dbAccess);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		FilesEncryptionResult result = await sut.EncryptFilesAsync(
			TestUtils.CreateFolderDto(),
			files,
			AppUtils.CreateRandomString(10));

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.FailedToLoadContents);
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.EncryptFilesAsync" />.
	/// </summary>
	[Test]
	public async Task EncryptFilesAsync_Does_Nothing_If_Db_Returns_Not_Required_Contents()
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length - 2, isValid: true).ToAsyncEnumerable());

			builder.RegisterInstance(dbAccess);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		FilesEncryptionResult result = await sut.EncryptFilesAsync(
			TestUtils.CreateFolderDto(),
			files,
			AppUtils.CreateRandomString(10));

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.FailedToLoadContents);
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.EncryptFilesAsync" />.
	/// </summary>
	[Test]
	public async Task EncryptFilesAsync_Does_Nothing_If_Encrypted_Contents_Are_Invalid_Or_Have_No_Identifiers()
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: false, generateId: false));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(encryption);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		FilesEncryptionResult result = await sut.EncryptFilesAsync(
			TestUtils.CreateFolderDto(),
			files,
			AppUtils.CreateRandomString(10));

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.FailedToEncryptContents);
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.EncryptFilesAsync" />.
	/// </summary>
	[Test]
	public async Task EncryptFilesAsync_Does_Nothing_If_Encrypted_Not_Required_Contents()
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateContents(files.Length - 2, isValid: true));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(encryption);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		FilesEncryptionResult result = await sut.EncryptFilesAsync(
			TestUtils.CreateFolderDto(),
			files,
			AppUtils.CreateRandomString(10));

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.FailedToEncryptContents);
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.EncryptFilesAsync" />.
	/// </summary>
	[Test]
	public async Task EncryptFilesAsync_Does_Nothing_If_Failed_To_Save_Contents()
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabase(out _)
				.Returns(x =>
				{
					x[0] = AppUtils.CreateRandomFileName(10);

					return true;
				});

			dbAccess
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, PropertyNameValuePair[]>>())
				.Returns(false);

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(fileSystem);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		FilesEncryptionResult result = await sut.EncryptFilesAsync(
			TestUtils.CreateFolderDto(),
			files,
			AppUtils.CreateRandomString(10));

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.FailedToSaveContents);

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.EncryptFilesAsync" />.
	/// </summary>
	[Test]
	public async Task EncryptFilesAsync_Does_Nothing_If_Failed_ToSave_Hash_Of_Password()
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabase(out _)
				.Returns(x =>
				{
					x[0] = AppUtils.CreateRandomFileName(10);

					return true;
				});

			dbAccess
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, PropertyNameValuePair[]>>())
				.Returns(true);

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(fileSystem);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		FilesEncryptionResult result = await sut.EncryptFilesAsync(
			TestUtils.CreateFolderDto(),
			files,
			AppUtils.CreateRandomString(10));

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.FailedToSavePasswordHash);

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.EncryptFilesAsync" />.
	/// </summary>
	[Test]
	public async Task EncryptFilesAsync_Does_Nothing_If_Unable_To_Create_Database_Backup()
	{
		// Arrange
		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.BackupDatabase(out _)
				.Returns(false);

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(encryption);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		FilesEncryptionResult result = await sut.EncryptFilesAsync(
			TestUtils.CreateFolderDto(),
			files,
			AppUtils.CreateRandomString(10));

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.UnableToCreateDatabaseBackup);
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.EncryptFilesAsync" />.
	/// </summary>
	[Test]
	public async Task EncryptFilesAsync_Successfully_Encrypts_Files()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			dbAccess
				.GetFilesContentsAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true).ToAsyncEnumerable());

			dbAccess
				.BackupDatabase(out _)
				.Returns(x =>
				{
					x[0] = AppUtils.CreateRandomFileName(10);

					return true;
				});

			dbAccess
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, PropertyNameValuePair[]>>())
				.Returns(true);

			dbAccess
				.UpdatePropertyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.EncryptContents(Arg.Any<ContentsIsValidPair[]>(), Arg.Any<byte[]>())
				.Returns(TestUtils.CreateContents(files.Length, isValid: true));

			encryption
				.EnhancedHashPassword(Arg.Any<string>())
				.Returns(AppUtils.CreateRandomString(10));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(fileSystem);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		FilesEncryptionResult result = await sut.EncryptFilesAsync(
			folder,
			files,
			AppUtils.CreateRandomString(10));

		// Assert
		result
			.Should()
			.Be(FilesEncryptionResult.Encrypted);

		folder.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);

		files
			.Should()
			.OnlyContain(x => x.EncryptionStatus == EncryptionStatus.Encrypted);

		folder.PasswordHash
			.Should()
			.NotBeNullOrEmpty();

		fileSystem
			.Received()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.ExecuteFile(FileModelDto)" />.
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
				.IsExecuted(Arg.Any<Guid>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(engine);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ExecuteFile(TestUtils.CreateFileDto());

		// Assert
		await dbAccess
			.Received(0)
			.GetFileContentsAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.ExecuteFile(FileModelDto)" />.
	/// </summary>
	[Test]
	public async Task ExecuteFile_Executes_File()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			engine
				.ExecuteAsync(Arg.Any<FileModelDto>(), Arg.Any<byte[]>(), Arg.Any<bool>())
				.Returns(true);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ContentsIsValidPair pair = new()
			{
				Contents = [],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
				.Returns(pair);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(engine);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ExecuteFile(dto);

		// Assert
		dto.IsExecuted
			.Should()
			.BeTrue();

		sut.ExecutedFiles
			.Should()
			.Contain(dto);

		await engine
			.Received()
			.ExecuteAsync(Arg.Any<FileModelDto>(), Arg.Any<byte[]>(), Arg.Any<bool>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.Exit" />.
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
	/// Test of <see cref="EditorViewModel.ExpandCollapseAllFoldersAsync(bool)" />.
	/// </summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task ExpandCollapseAllFoldersAsync_Should_Act_To_All_Folders(bool isExpandAll)
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
	/// Test of <see cref="EditorViewModel.HandleChangeHotkeysAsync" />.
	/// </summary>
	[Test]
	public async Task HandleChangeHotkeysAsync_Handles_Bussiness_Logic_After_Hotkeys_Editing()
	{
		// Arrange
		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsManager settingsManager = Substitute.For<IAppSettingsManager>();

			settingsManager
				.Settings
				.Returns(TestUtils.CreateRandomSettings(trackHotkeys: true));

			builder.RegisterInstance(settingsManager);

			builder.RegisterInstance(hook);
		});

		HotkeysEditorViewModel viewModel = mock.Create<HotkeysEditorViewModel>();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.HandleChangeHotkeysAsync(viewModel, TestUtils.CreateFileDto());

		// Assert
		viewModel.IsDisposed
			.Should()
			.BeTrue();

		await hook
			.Received()
			.StartTrackingAsync(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.HandleChangeSettings" />.
	/// </summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task HandleChangeSettings_Handles_Bussiness_Logic_After_Settings_Changing(bool isSave)
	{
		// Arrange
		IAppSettingsManager settingsManager = Substitute.For<IAppSettingsManager>();

		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		AppSettings settings = TestUtils.CreateRandomSettings(trackHotkeys: true);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			hook
				.IsRunning
				.Returns(true);

			settingsManager
				.Settings
				.Returns(settings);

			builder.RegisterInstance(settingsManager);

			builder.RegisterInstance(hook);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.HandleChangeSettings(isSave, settings);

		// Assert
		if (isSave)
		{
			settingsManager
				.Received()
				.OverwriteSettings(Arg.Any<AppSettings>());

			settingsManager
				.Received()
				.SaveSettingsInFile();
		}
		else
		{
			settingsManager
				.Received()
				.ApplyMeterialTheme();

		}

		hook
			.Received()
			.StopTracking();

		await hook
			.Received()
			.StartTrackingAsync(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.Initialize" />.
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

		CopyHistoryViewSettings copyHistorySettings = new()
		{
			CopyHistory = [.. TestUtils.CreateGuids(5)],
			SelectedCopyHistoryItemId = Guid.NewGuid()
		};

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

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

		sut.CopyHistorySettings.SelectedCopyHistoryItemId
			.Should()
			.Be(copyHistorySettings.SelectedCopyHistoryItemId);

		sut.CopyHistorySettings.CopyHistory
			.Should()
			.Contain(copyHistorySettings.CopyHistory);
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.NavigationColumnWidth" />.
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
	/// Test of <see cref="EditorViewModel.OverwriteFileHotkeysAsync" />.
	/// </summary>
	[Test]
	public async Task OverwriteFileHotkeysAsync_Deletes_Hotkeys_In_Database_And_Returns_EmptySequence()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		dto
			.Hotkeys
			.AddRange(TestUtils.CreateHotkeysDto(5));

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(TypedParameter.From(dbAccess));

		// Act
		OverwriteHotkeysResult result = await sut.OverwriteFileHotkeysAsync(dto, []);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysResult.EmptySequence);

		dto.Hotkeys
			.Should()
			.BeEmpty();

		await dbAccess
			.Received()
			.DeleteHotkeysAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.OverwriteFileHotkeysAsync" />.
	/// </summary>
	[Test]
	public async Task OverwriteFileHotkeysAsync_Returns_AlreadyInUse()
	{
		// Arrange
		CodeMaskPair[] newHotkeys = [.. TestUtils.CreateCodeMaskPairs(5)];

		FileModelDto dto = TestUtils.CreateFileDto();

		dto
			.Hotkeys
			.AddRange(newHotkeys.ToHotkeyModelsDto());

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.Add(dto);

		// Act
		OverwriteHotkeysResult result = await sut.OverwriteFileHotkeysAsync(TestUtils.CreateFileDto(), newHotkeys);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysResult.AlreadyInUse);
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.OverwriteFileHotkeysAsync" />.
	/// </summary>
	[Test]
	public async Task OverwriteFileHotkeysAsync_Returns_Rewritten()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		CodeMaskPair[] newHotkeys = [.. TestUtils.CreateCodeMaskPairs(5)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Map<HotkeyModel[], HotkeyModelDto[]>(Arg.Any<HotkeyModel[]>())
				.Returns([.. TestUtils.CreateHotkeysDto(newHotkeys.Length)]);

			builder.RegisterInstance(mapper);

			builder.RegisterInstance(dbAccess);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		OverwriteHotkeysResult result = await sut.OverwriteFileHotkeysAsync(dto, newHotkeys);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysResult.Rewritten);

		dto.Hotkeys
			.Should()
			.HaveCount(newHotkeys.Length);

		dto.HotkeysToolTip
			.Should()
			.NotBeNullOrEmpty();

		await dbAccess
			.Received()
			.AddHotkeysAsync(Arg.Any<Guid>(), Arg.Any<CodeMaskPair[]>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.OverwriteFileHotkeysAsync" />.
	/// </summary>
	[Test]
	public async Task OverwriteFileHotkeysAsync_Returns_SameHotkeys()
	{
		// Arrange
		CodeMaskPair[] newHotkeys = [.. TestUtils.CreateCodeMaskPairs(5)];

		FileModelDto dto = TestUtils.CreateFileDto();

		dto
			.Hotkeys
			.AddRange(newHotkeys.ToHotkeyModelsDto());

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		OverwriteHotkeysResult result = await sut.OverwriteFileHotkeysAsync(dto, newHotkeys);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysResult.SameHotkeys);
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.RenameAsync(ExplorerModelBaseDto, string, DateTime, CancellationToken)" />.
	/// </summary>
	[Test]
	public async Task RenameAsync_Renames_Dto_And_Updates_Name_In_Database_Entity()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		ExplorerModelBaseDto dto = Substitute.For<ExplorerModelBaseDto>();

		string newName = AppUtils.CreateRandomString(10);

		dto.Name = AppUtils.CreateRandomString(10);

		DateTime updatedDate = DateTime.Now;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess.UpdatePropertiesAsync(
				Arg.Any<Guid>(),
				Arg.Any<CancellationToken>(),
				Arg.Any<PropertyNameValuePair[]>())
			.Returns(true);

			builder.RegisterInstance(dbAccess);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		bool result = await sut.RenameAsync(dto, newName, updatedDate);

		// Assert
		result
			.Should()
			.BeTrue();

		dto.Name
			.Should()
			.Be(newName);

		dto.UpdatedDate
			.Should()
			.Be(updatedDate);
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.RenameAsync(ExplorerModelBaseDto, string, DateTime, CancellationToken)" />.
	/// </summary>
	[Test]
	public async Task RenameAsync_Should_Do_Nothing_If_Name_Is_The_Same()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		ExplorerModelBaseDto toBeRenamed = Substitute.For<ExplorerModelBaseDto>();

		string newName = AppUtils.CreateRandomString(10);

		toBeRenamed.Name = newName;

		DateTime updatedDate = DateTime.Now;

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(TypedParameter.From(dbAccess));

		// Act
		bool result = await sut.RenameAsync(toBeRenamed, newName, updatedDate);

		// Assert
		result
			.Should()
			.BeFalse();

		toBeRenamed.UpdatedDate
			.Should()
			.NotBe(updatedDate);

		await dbAccess.Received(0).UpdatePropertyAsync(
			Arg.Any<Guid>(),
			Arg.Any<string>(),
			Arg.Any<string>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.ResetSelectedObject" />.
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
	/// Test of <see cref="EditorViewModel.RestartApplication" />.
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
	/// Test of <see cref="EditorViewModel.SetFavorite(FileModelDto?)" />.
	/// </summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task SetFavorite_Sets_IsFavorite_Property_And_Saves_In_database(bool initialValue)
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		FileModelDto dto = TestUtils.CreateFileDto();

		dto.IsFavorite = initialValue;

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(TypedParameter.From(dbAccess));

		// Act
		await sut.SetFavorite(dto);

		// Assert
		dto.IsFavorite
			.Should()
			.NotBe(initialValue);

		await dbAccess.Received().UpdatePropertyAsync(
			Arg.Any<Guid>(),
			Arg.Any<string>(),
			Arg.Any<bool>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.SetSelectedObject(ExplorerModelBaseDto)" />.
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
	/// Test of <see cref="EditorViewModel.ShowFavorites(EditorWindow?)" />.
	/// </summary>
	[AvaloniaTest]
	public void ShowFavorites_Shows_Favorites_Window()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock mock = AutoMock.GetLoose();

			viewLauncher
				.ConfigureFavoritesWindow(Arg.Any<IEnumerable<ExplorerModelBaseDto>>())
				.Returns(mock.Create<FavoritesWindow>());

			builder.RegisterInstance(viewLauncher);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.ShowFavorites(null);

		// Assert
		sut.IsShutdown
			.Should()
			.BeFalse();

		viewLauncher
			.Received()
			.ConfigureFavoritesWindow(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.ShowHotkeysEditor" />.
	/// </summary>
	[AvaloniaTest]
	public void ShowHotkeysEditor_Shows_The_Hotkeys_Editor_View()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		dto
			.Hotkeys
			.AddRange(TestUtils.CreateHotkeysDto(5));

		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		IViewFactory viewFactory = Substitute.For<IViewFactory>();

		using AutoMock temp = AutoMock.GetLoose();

		HotkeysEditorView view = temp.Create<HotkeysEditorView>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			viewFactory
				.CreateUserControl<HotkeysEditorView>()
				.Returns(view);

			hook
				.IsRunning
				.Returns(true);

			builder.RegisterInstance(hook);

			builder.RegisterInstance(viewFactory);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.ShowHotkeysEditor(dto);

		hook
			.Received()
			.StopTracking();

		viewFactory
			.Received()
			.CreateUserControl<HotkeysEditorView>();

		view.ViewModel.Buffer
			.Should()
			.BeEquivalentTo(dto.Hotkeys.ToCodeMaskPairs());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.ShowSettings" />.
	/// </summary>
	[AvaloniaTest]
	public void ShowSettings_Shows_The_Settings_View()
	{
		// Arrange
		IViewFactory viewFactory = Substitute.For<IViewFactory>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock mock = AutoMock.GetLoose();

			viewFactory
				.CreateUserControl<SettingsView>()
				.Returns(mock.Create<SettingsView>());

			builder.RegisterInstance(viewFactory);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.IsLeftDrawerOpened = true;

		// Act
		sut.ShowSettings();

		// Assert
		sut.IsLeftDrawerOpened
			.Should()
			.BeFalse();

		viewFactory
			.Received()
			.CreateUserControl<SettingsView>();
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.TakeCryptPasswordAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task TakeCryptPasswordAsync_Does_Nothing_If_File_Is_Being_Edited_Or_Executed(CryptoAction action)
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		FileModelDto[] files =
		[
			.. TestUtils.CreateFilesDto(5, isEdited: true),
			.. TestUtils.CreateFilesDto(5, isExecuted: true)
		];

		folder
			.Children
			.AddRange(files);

		IViewFactory viewFactory = Substitute.For<IViewFactory>();

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(TypedParameter.From(viewFactory));

		// Act
		await sut.TakeCryptPasswordAsync(folder, action);

		// Assert
		viewFactory
			.Received(0)
			.CreateUserControl<PasswordBox>();
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.TakeCryptPasswordAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task TakeCryptPasswordAsync_Does_Nothing_If_Folder_Has_No_Files(CryptoAction action)
	{
		// Arrange
		IViewFactory viewFactory = Substitute.For<IViewFactory>();

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(TypedParameter.From(viewFactory));

		// Act
		await sut.TakeCryptPasswordAsync(TestUtils.CreateFolderDto(), action);

		// Assert
		viewFactory
			.Received(0)
			.CreateUserControl<PasswordBox>();
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.TakeCryptPasswordAsync" />.
	/// </summary>
	[TestCase(CryptoAction.Encrypt)]
	[TestCase(CryptoAction.Decrypt)]
	public async Task TakeCryptPasswordAsync_Shows_Password_Box(CryptoAction action)
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder
			.Children
			.AddRange(TestUtils.CreateFilesDto(5));

		IViewFactory viewFactory = Substitute.For<IViewFactory>();

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>(TypedParameter.From(viewFactory));

		// Act
		await sut.TakeCryptPasswordAsync(folder, action);

		// Assert
		viewFactory
			.Received()
			.CreateUserControl<PasswordBox>();
	}
	#endregion
}
