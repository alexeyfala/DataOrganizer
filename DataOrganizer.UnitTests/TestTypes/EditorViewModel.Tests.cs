using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.UnitTests.Helpers;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using Entities.Abstract;
using Entities.Enums;
using Entities.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using Repository.DTO;
using Repository.Interfaces;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

			builder.RegisterInstance(Substitute.For<IDispatcher>().RunPostInline());
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
	/// Test of <see cref="EditorViewModel.CloseExecutingFile" />.
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
			TypedParameter.From(Substitute.For<IDispatcher>().RunPostInline()));

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
	/// Test of <see cref="EditorViewModel.CloseFiles" />.
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
			TypedParameter.From(Substitute.For<IDispatcher>().RunPostInline()));

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
			.DidNotReceive()
			.RequestCloseFilesAsync();
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

			builder.RegisterInstance(Substitute.For<IDispatcher>().RunPostInline());
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

			builder.RegisterInstance(Substitute.For<IDispatcher>().RunPostInline());
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.AddRange(toBeDeleted.ToEnumerable(TestUtils.CreateFoldersDto(5)));

		if (type != EntityType.Folder)
		{
			sut
				.ExecutingFiles
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

		sut.ExecutingFiles
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
			.DidNotReceive()
			.RequestCloseFilesAsync();
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

			builder.RegisterInstance(Substitute.For<IDispatcher>().RunPostInline());
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
				.ApplyMaterialTheme();

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
			isExecuting: true,
			encryptionStatus: EncryptionStatus.Decrypted)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(Substitute.For<IDispatcher>().RunPostInline());
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
			.OnlyContain(x => !x.IsExecuting && x.EncryptionStatus == EncryptionStatus.Encrypted);
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
				.RequestCloseFilesAsync()
				.Returns(true);

			builder.RegisterInstance(dialogService);

			builder.RegisterInstance(Substitute.For<IDispatcher>().RunPostInline());
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

		file.IsExecuting
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

			builder.RegisterInstance(Substitute.For<IDispatcher>().RunPostInline());
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
		ExplorerModelBaseDto dto = Substitute.For<ExplorerModelBaseDto>();

		string newName = AppUtils.CreateRandomString(10);

		dto.Name = AppUtils.CreateRandomString(10);

		DateTime updatedDate = DateTime.Now;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess.UpdateFolderPropertiesAsync(
				Arg.Any<Guid>(),
				Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>())
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
		ExplorerModelBaseDto toBeRenamed = Substitute.For<ExplorerModelBaseDto>();

		string newName = AppUtils.CreateRandomString(10);

		toBeRenamed.Name = newName;

		DateTime updatedDate = DateTime.Now;

		using AutoMock mock = AutoMock.GetLoose();

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		bool result = await sut.RenameAsync(toBeRenamed, newName, updatedDate);

		// Assert
		result
			.Should()
			.BeFalse();

		toBeRenamed.UpdatedDate
			.Should()
			.NotBe(updatedDate);
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

		await dbAccess.Received().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
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
			.DidNotReceive()
			.RequestCloseFilesAsync();
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

			builder.RegisterInstance(Substitute.For<IDispatcher>().RunPostInline());
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
