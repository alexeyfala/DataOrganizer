using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EditorViewModel)}"" type")]
internal class EditorViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="EditorViewModel.AddAsync" />.
	/// </summary>
	[Test]
	public async Task AddAsync_Returns_Entity(
		[Values] EntityType type,
		[Values] bool hasParent)
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
	/// Test of <see cref="EditorViewModel.AddHierarchy" />.
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
	/// Test of <see cref="EditorViewModel.ChangePassword" />.
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
			isExecuted: true)];

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IEntityEcryption entityEcryption = Substitute.For<IEntityEcryption>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestUserCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(entityEcryption);
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
			.OnlyContain(x => !x.IsExecuted);

		await entityEcryption
			.Received()
			.ChangePasswordAsync(Arg.Any<FolderModelDto>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.CloseExecutingFile" />.
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
		sut.CloseExecutingFile(dto);

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
	/// Test of <see cref="EditorViewModel.CloseFiles" />.
	/// </summary>
	[Test]
	public void CloseFiles_Closes_Editing_And_Executing_Files()
	{
		// Arrange
		FileModelDto[] editingFiles = [.. TestUtils.CreateFilesDto(2)];

		FileModelDto[] executingFiles = [.. TestUtils.CreateFilesDto(2)];

		editingFiles.ForEach(x => x.IsEditing = true);

		executingFiles.ForEach(x => x.IsExecuted = true);

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.CloseFiles(editingFiles, executingFiles);

		// Assert
		editingFiles
			.Should()
			.OnlyContain(x => !x.IsEditing);

		executingFiles
			.Should()
			.OnlyContain(x => !x.IsExecuted);
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.DecryptFolder" />.
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
			.Received(0)
			.RequestUserCloseFilesAsync();
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.DecryptFolder" />.
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
			isExecuted: true)];

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IEntityEcryption entityEcryption = Substitute.For<IEntityEcryption>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestUserCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(entityEcryption);
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
			.OnlyContain(x => !x.IsExecuted);

		await entityEcryption
			.Received()
			.DecryptFolderAsync(Arg.Any<FolderModelDto>(), Arg.Any<FileModelDto[]>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.DeleteAsync" />.
	/// </summary>
	[TestCase(EntityType.Folder)]
	[TestCase(EntityType.File)]
	public async Task DeleteAsync_Deletes_Entity_In_Database_And_In_Treeview(EntityType type)
	{
		// Arrange
		ExplorerModelBaseDto toBeDeleted = type switch
		{
			EntityType.Folder => TestUtils.CreateFolderDto(),
			EntityType.File => TestUtils.CreateFileDto(isExecuting: true),
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
	/// Test of <see cref="EditorViewModel.DeleteAsync" />.
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
	/// Test of <see cref="EditorViewModel.EncryptFolder" />.
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
			.Received(0)
			.RequestUserCloseFilesAsync();
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.EncryptFolder" />.
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
			isExecuted: true)];

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IEntityEcryption entityEcryption = Substitute.For<IEntityEcryption>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestUserCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(entityEcryption);
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
			.OnlyContain(x => !x.IsExecuted);

		await entityEcryption
			.Received()
			.EncryptFolderAsync(Arg.Any<FolderModelDto>(), Arg.Any<FileModelDto[]>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.ExecuteFile" />.
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
	/// Test of <see cref="EditorViewModel.ExecuteFile" />.
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
			.ExecuteAsync(Arg.Any<ExecuteFileParameters>());
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
	/// Test of <see cref="EditorViewModel.ExpandCollapseAllFoldersAsync" />.
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
	/// Test of <see cref="EditorViewModel.HandleChangeSettingsAsync" />.
	/// </summary>
	[Test]
	public async Task HandleChangeSettingsAsync_Handles_Bussiness_Logic_After_Settings_Changing([Values] bool isSave)
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
		await sut.HandleChangeSettingsAsync(isSave, settings);

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

		await hook
			.Received()
			.StartTrackingAsync(Arg.Any<IEnumerable<ExplorerModelBaseDto>>());

		await hook
			.Received()
			.StopTrackingAsync();
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.HideAllFileContents" />.
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
			isExecuted: true,
			encryptionStatus: EncryptionStatus.Decrypted)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestUserCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.AddHierarchy(editingFiles.Concat(executingFiles));

		// Act
		await sut.HideAllFileContents();

		// Assert
		editingFiles
			.Should()
			.OnlyContain(x => !x.IsEditing && x.EncryptionStatus == EncryptionStatus.Encrypted);

		executingFiles
			.Should()
			.OnlyContain(x => !x.IsExecuted && x.EncryptionStatus == EncryptionStatus.Encrypted);
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.HideFileContents" />.
	/// </summary>
	[Test]
	public async Task HideFileContents_Does_Work([Values] bool isEditing)
	{
		// Arrange
		FileModelDto file = isEditing
			? TestUtils.CreateFileDto(isEditing: true)
			: TestUtils.CreateFileDto(isExecuting: true);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestUserCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.HideFileContents(file);

		// Assert
		file.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);

		file.IsEditing
			.Should()
			.BeFalse();

		file.IsExecuted
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.HideFolderContents" />.
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
			isExecuted: true)];

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IEntityEcryption entityEcryption = Substitute.For<IEntityEcryption>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestUserCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(entityEcryption);
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
			.OnlyContain(x => !x.IsExecuted);

		entityEcryption
			.Received()
			.HideFolderContents(Arg.Any<FolderModelDto>(), Arg.Any<IEnumerable<ExplorerModelBaseDto>>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.Import" />.
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
			isExecuted: true)];

		IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestUserCloseFilesAsync()
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
	/// Test of <see cref="EditorViewModel.RenameAsync" />.
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
	/// Test of <see cref="EditorViewModel.RenameAsync" />.
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
	/// Test of <see cref="EditorViewModel.SetFavorite" />.
	/// </summary>
	[Test]
	public async Task SetFavorite_Sets_IsFavorite_Property_And_Saves_In_database([Values] bool initialValue)
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
	/// Test of <see cref="EditorViewModel.SetSelectedObject" />.
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
	/// Test of <see cref="EditorViewModel.ShowFavorites" />.
	/// </summary>
	[AvaloniaTest]
	public void ShowFavorites_Shows_Favorites_Window()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock mock = AutoMock.GetLoose();

			viewLauncher.ConfigureFavoritesWindow(
				Arg.Any<IEnumerable<ExplorerModelBaseDto>>(),
				Arg.Any<IEnumerable<FileModelDto>>(),
				Arg.Any<IEnumerable<FileModelDto>>())
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

		viewLauncher.Received().ConfigureFavoritesWindow(
			Arg.Any<IEnumerable<ExplorerModelBaseDto>>(),
			Arg.Any<IEnumerable<FileModelDto>>(),
			Arg.Any<IEnumerable<FileModelDto>>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.ShowFolderContents" />.
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
			.Received(0)
			.RequestUserCloseFilesAsync();
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.ShowFolderContents" />.
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
			isExecuted: true)];

		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder
			.Children
			.AddRange(editingFiles.Concat(executingFiles));

		IEntityEcryption entityEcryption = Substitute.For<IEntityEcryption>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestUserCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(entityEcryption);
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
			.OnlyContain(x => !x.IsExecuted);

		await entityEcryption
			.Received()
			.ShowFolderContentsAsync(Arg.Any<FolderModelDto>());
	}

	/// <summary>
	/// Test of <see cref="EditorViewModel.ShowHotkeysEditor" />.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowHotkeysEditor_Shows_The_Hotkeys_Editor_View()
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
		await sut.ShowHotkeysEditor(dto);

		viewFactory
			.Received()
			.CreateUserControl<HotkeysEditorView>();

		view.ViewModel.Buffer
			.Should()
			.BeEquivalentTo(dto.Hotkeys.ToCodeMaskPairs());

		await hook
			.Received()
			.StopTrackingAsync();
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
	#endregion
}
