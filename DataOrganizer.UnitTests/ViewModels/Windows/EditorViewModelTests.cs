using Autofac;
using Autofac.Extras.Moq;
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
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Enums.Views;
using DataOrganizer.Extensions.Hierarchy;
using DataOrganizer.Extensions.Hotkeys;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Exchange;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Editor;
using DataOrganizer.Messages.Encryption;
using DataOrganizer.Messages.Runtime;
using DataOrganizer.Messages.Updates;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Documents;
using DataOrganizer.Services.Encryption;
using DataOrganizer.Services.Exchange;
using DataOrganizer.Services.Execution;
using DataOrganizer.Services.Hierarchy;
using DataOrganizer.Services.Hotkeys;
using DataOrganizer.Services.Runtime;
using DataOrganizer.Services.Settings;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Hierarchy;
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

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(folderProtection);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
	/// <see cref="EditorViewModel.ChangePasswordCommand" /> CanExecute: only the password of a password keeper changes, and
	/// not in read-only mode.
	/// </summary>
	[Test]
	[TestCase(true, false, ExpectedResult = true)]
	[TestCase(false, false, ExpectedResult = false)]
	[TestCase(true, true, ExpectedResult = false)]
	public bool ChangePasswordCommand_CanExecute_Allows_Only_A_Password_Keeper(bool isKeeper, bool isReadOnly)
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		if (isKeeper)
		{
			folder.EncryptedDek = RandomValues.CreateBytes(10);
		}

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.IsReadOnly = isReadOnly;

		// Act
		return sut
			.ChangePasswordCommand
			.CanExecute(folder);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ClearRightSideSheetCommand" />: an empty copy history is not worth a question.
	/// </summary>
	[Test]
	public async Task ClearRightSideSheetCommand_Asks_Nothing_For_An_Empty_Copy_History()
	{
		// Arrange
		IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(confirmationPrompt);

			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.ShowCopyHistoryCommand
			.Execute(null);

		// Act
		await sut
			.ClearRightSideSheetCommand
			.ExecuteAsync(null);

		// Assert
		await confirmationPrompt
			.DidNotReceive()
			.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ClearRightSideSheetCommand" />: the copy history and the list that shows it are emptied
	/// only when the clear is confirmed.
	/// </summary>
	[AvaloniaTest]
	public async Task ClearRightSideSheetCommand_Clears_The_Copy_History_Only_When_Confirmed([Values] bool isConfirmed)
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(isConfirmed);

			builder.RegisterInstance(confirmationPrompt);

			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		CopyHistory copyHistory = mock.Create<CopyHistory>();

		sut
			.Hierarchy
			.AddRange(files);

		copyHistory.Add(files[1], showAtOnce: false);

		copyHistory.Add(files[0], showAtOnce: false);

		sut
			.ShowCopyHistoryCommand
			.Execute(null);

		CopyHistoryViewModel copyHistoryList = sut.CopyHistoryList!;

		Dispatcher.UIThread.RunJobs();

		// Act
		await sut
			.ClearRightSideSheetCommand
			.ExecuteAsync(null);

		Dispatcher.UIThread.RunJobs();

		// Assert
		copyHistory.ItemIds.Count
			.Should()
			.Be(isConfirmed ? 0 : files.Length);

		copyHistoryList.Items.Count
			.Should()
			.Be(isConfirmed ? 0 : files.Length);
	}

	/// <summary>
	/// <see cref="ViewModelBase.CloseExecutingFile" />: the file is removed from executing files, unmarked, and closed in the engine.
	/// </summary>
	[Test]
	public async Task CloseExecutingFile_Closes_File()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		dto.IsExecuting = true;

		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			engine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(engine);

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		await mock
			.Create<IExecutingFiles>()
			.ExecuteAsync(new ExecuteFileParameters
			{
				Contents = [],
				File = dto,
				IsReadOnly = false
			});

		// Act
		sut.CloseExecutingFile(dto);

		// Assert
		sut.ExecutingFiles
			.Should()
			.NotContain(dto);

		dto.IsExecuting
			.Should()
			.BeFalse();

		await engine
			.Received(1)
			.CloseAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.CloseOpenedFileCommand" />: closing a file open in the editor closes its tab.
	/// </summary>
	[Test]
	public void CloseOpenedFileCommand_Closes_The_Tab_Of_An_Edited_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(new WeakReferenceMessenger())
				.As<IMessenger>();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.EditingFiles.OpenInEditor(file);

		// Act
		sut
			.CloseOpenedFileCommand
			.Execute(file);

		// Assert
		sut.EditingFiles.Items
			.Should()
			.BeEmpty();

		file.IsEditing
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EditorViewModel.CopyContentByContextMenuCommand" />: a file copied again moves to the top of the copy
	/// history without a second entry.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyContentByContextMenuCommand_Moves_The_File_To_The_Top_Of_The_Copy_History()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

			dbAccess
				.ExistsAsync(files[1].Id, Arg.Any<CancellationToken>())
				.Returns(true);

			fileTextCopier
				.CopyAsync(files[1], Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(FileTextStatus.Text);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);

			builder
				.RegisterType<ContentCopier>()
				.As<IContentCopier>();

			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		CopyHistory copyHistory = mock.Create<CopyHistory>();

		sut
			.Hierarchy
			.AddRange(files);

		copyHistory.Add(files[1], showAtOnce: false);

		copyHistory.Add(files[0], showAtOnce: false);

		// Act
		await sut
			.CopyContentByContextMenuCommand
			.ExecuteAsync(files[1]);

		// Assert
		copyHistory.ItemIds
			.Should()
			.Equal(files[1].Id, files[0].Id);
	}

	/// <summary>
	/// <see cref="EditorViewModel.CopyContentByContextMenuCommand" />: the copied file shows at once in the open copy
	/// history.
	/// </summary>
	[AvaloniaTest]
	public async Task CopyContentByContextMenuCommand_Shows_The_File_In_The_Open_Copy_History()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			fileTextCopier
				.CopyAsync(file, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(FileTextStatus.Text);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);

			builder
				.RegisterType<ContentCopier>()
				.As<IContentCopier>();

			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(file);

		sut
			.ShowCopyHistoryCommand
			.Execute(null);

		CopyHistoryViewModel copyHistory = sut.CopyHistoryList!;

		Dispatcher.UIThread.RunJobs();

		// Act
		await sut
			.CopyContentByContextMenuCommand
			.ExecuteAsync(file);

		Dispatcher.UIThread.RunJobs();

		// Assert
		copyHistory.Items
			.Should()
			.Equal(file);
	}

	/// <summary>
	/// <see cref="ViewModelBase.CopyHistoryItemIds" />: the window counts the files of the copy history.
	/// </summary>
	[Test]
	public void CopyHistoryItemIds_Lists_The_Copy_History()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		mock
			.Create<CopyHistory>()
			.Add(file, showAtOnce: false);

		// Assert
		sut.CopyHistoryItemIds
			.Should()
			.Equal(file.Id);
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
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(folderProtection);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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

		// Every file of the folder is decrypted, the closed ones too.
		await folderProtection
			.Received(1)
			.DecryptFolderAsync(folder, Arg.Is<FileDto[]>(x => x.ToHashSet().SetEquals(editingFiles.Concat(executingFiles))));
	}

	/// <summary>
	/// <see cref="EditorViewModel.DecryptFolder" />: a folder without files is not decrypted.
	/// </summary>
	[Test]
	public async Task DecryptFolder_Does_Nothing_If_Missing_Files()
	{
		// Arrange
		IFolderProtection folderProtection = Substitute.For<IFolderProtection>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(folderProtection);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.DecryptFolder(ItemDtoFactory.CreateFolderDto());

		// Assert
		await folderProtection
			.DidNotReceiveWithAnyArgs()
			.DecryptFolderAsync(default!, default!);
	}

	/// <summary>
	/// <see cref="EditorViewModel.DecryptFolderCommand" /> CanExecute: only an encrypted password keeper whose children are
	/// all encrypted is decrypted, and not in read-only mode.
	/// </summary>
	[Test]
	[TestCase(true, EncryptionStatus.Encrypted, false, ExpectedResult = true)]
	[TestCase(true, EncryptionStatus.Decrypted, false, ExpectedResult = false)]
	[TestCase(false, EncryptionStatus.Encrypted, false, ExpectedResult = false)]
	[TestCase(true, EncryptionStatus.Encrypted, true, ExpectedResult = false)]
	public bool DecryptFolderCommand_CanExecute_Allows_Only_A_Keeper_With_Encrypted_Children(
		bool isKeeper,
		EncryptionStatus childStatus,
		bool isReadOnly)
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

		FileDto child = ItemDtoFactory.CreateFileDto(encryptionStatus: childStatus);

		child.Parent = folder;

		folder
			.Children
			.Add(child);

		if (isKeeper)
		{
			folder.EncryptedDek = RandomValues.CreateBytes(10);
		}

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.IsReadOnly = isReadOnly;

		// Act
		return sut
			.DecryptFolderCommand
			.CanExecute(folder);
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
			IExecutionEngine engine = Substitute.For<IExecutionEngine>();

			engine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>(), Arg.Any<CancellationToken>())
				.Returns(true);

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

			builder.RegisterInstance(engine);

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<DeletionScenario>()
				.As<IDeletionScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		await mock
			.Create<IExecutingFiles>()
			.ExecuteAsync(new ExecuteFileParameters
			{
				Contents = [],
				File = file,
				IsReadOnly = false
			});

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
			executionEngine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>(), Arg.Any<CancellationToken>())
				.Returns(true);

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

			builder
				.RegisterInstance(new WeakReferenceMessenger())
				.As<IMessenger>();

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<DeletionScenario>()
				.As<IDeletionScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		await mock
			.Create<IExecutingFiles>()
			.ExecuteAsync(new ExecuteFileParameters
			{
				Contents = [],
				File = executingFile,
				IsReadOnly = false
			});

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
	/// <see cref="EditorViewModel.DeleteAsync" />: a deletion counts the objects of the tree again.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Counts_The_Objects_Again()
	{
		// Arrange
		FileDto deleted = ItemDtoFactory.CreateFileDto();

		FileDto kept = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			// The editor takes the object out of the tree, as the real one does.
			hierarchyEditor
				.DeleteAsync(
					deleted,
					Arg.Any<Collection<ExplorerItemDtoBase>>(),
					Arg.Any<CancellationToken>())
				.Returns(x => Task.FromResult(x
					.ArgAt<Collection<ExplorerItemDtoBase>>(1)
					.Remove(deleted)));

			WorkSession session = new();

			session
				.Hierarchy
				.AddRange([deleted, kept]);

			builder.RegisterInstance(hierarchyEditor);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<DeletionScenario>()
				.As<IDeletionScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.DeleteAsync(deleted);

		// Assert
		sut.HierarchySummary
			.Should()
			.Be(new ExplorerItemDtoBase[] { kept }.GetCount().AsString());
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

		IContentKeys contentKeys = Substitute.For<IContentKeys>();

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

			builder.RegisterInstance(contentKeys);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<DeletionScenario>()
				.As<IDeletionScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		bool result = await sut.DeleteAsync(folder);

		// Assert
		result
			.Should()
			.BeTrue();

		contentKeys
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
			IExecutionEngine engine = Substitute.For<IExecutionEngine>();

			engine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>(), Arg.Any<CancellationToken>())
				.Returns(true);

			IHierarchyEditor hierarchyEditor = Substitute.For<IHierarchyEditor>();

			hierarchyEditor
				.DeleteAsync(
					Arg.Any<ExplorerItemDtoBase>(),
					Arg.Any<Collection<ExplorerItemDtoBase>>(),
					Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(hierarchyEditor);

			builder.RegisterInstance(engine);

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<DeletionScenario>()
				.As<IDeletionScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		await mock
			.Create<IExecutingFiles>()
			.ExecuteAsync(new ExecuteFileParameters
			{
				Contents = [],
				File = file,
				IsReadOnly = false
			});

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

		IContentKeys contentKeys = Substitute.For<IContentKeys>();

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

			builder.RegisterInstance(contentKeys);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<DeletionScenario>()
				.As<IDeletionScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		bool result = await sut.DeleteAsync(folder);

		// Assert
		result
			.Should()
			.BeFalse();

		contentKeys
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

		IContentKeys contentKeys = Substitute.For<IContentKeys>();

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

			builder.RegisterInstance(contentKeys);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<DeletionScenario>()
				.As<IDeletionScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		bool result = await sut.DeleteAsync(file);

		// Assert
		result
			.Should()
			.BeTrue();

		contentKeys
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

			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<DeletionScenario>()
				.As<IDeletionScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		CopyHistory copyHistory = mock.Create<CopyHistory>();

		copyHistory.Add(nestedFile, showAtOnce: false);

		copyHistory.Add(keptFile, showAtOnce: false);

		// Act
		bool result = await sut.DeleteAsync(folder);

		// Assert
		result
			.Should()
			.BeTrue();

		copyHistory.ItemIds
			.Should()
			.Equal(keptFile.Id);
	}

	/// <summary>
	/// <see cref="EditorViewModel.DeleteCommand" />: without an object the command runs only while a row of the tree is
	/// selected.
	/// </summary>
	[Test]
	public void DeleteCommand_Without_An_Object_Needs_A_Selected_Row()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(ItemDtoFactory.CreateFileDto());

		bool canRunUnselected = sut.DeleteCommand.CanExecute(null);

		// Act
		sut.SelectedNode = sut.Nodes.Single();

		// Assert
		canRunUnselected
			.Should()
			.BeFalse();

		sut.DeleteCommand.CanExecute(null)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: the editor disposes the rows of its tree.
	/// </summary>
	[Test]
	public void Dispose_Disposes_The_Rows_Of_The_Tree()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.AddRange([ItemDtoFactory.CreateFolderDto(), ItemDtoFactory.CreateFileDto()]);

		ExplorerNodeViewModelBase[] rows = [.. sut.Nodes];

		// Act
		sut.Dispose();

		// Assert
		rows
			.Should()
			.HaveCount(2)
			.And
			.OnlyContain(static x => x.IsDisposed);
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: the editor disposes the list of the copy history its side sheet
	/// shows.
	/// </summary>
	[Test]
	public void Dispose_Disposes_The_Shown_Copy_History_List()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.ShowCopyHistoryCommand
			.Execute(null);

		CopyHistoryViewModel copyHistoryList = sut.CopyHistoryList!;

		// Act
		sut.Dispose();

		// Assert
		copyHistoryList.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorViewModel.EditNote" />: the note of a protected object is declared sensitive to the dialog.
	/// </summary>
	[Test]
	public async Task EditNote_Declares_The_Note_Of_A_Protected_Object_Sensitive([Values] EncryptionStatus encryptionStatus)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: encryptionStatus);

		ITextInputPrompt textInputPrompt = Substitute.For<ITextInputPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			textInputPrompt
				.RequestMultilineTextAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<bool>(),
					Arg.Any<CancellationToken>())
				.Returns(new TextInputResult());

			builder.RegisterInstance(textInputPrompt);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.EditNote(file);

		// Assert
		await textInputPrompt.Received(1).RequestMultilineTextAsync(
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

		ITextInputPrompt textInputPrompt = Substitute.For<ITextInputPrompt>();

		INoteEditor noteEditor = Substitute.For<INoteEditor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			textInputPrompt
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

			builder.RegisterInstance(textInputPrompt);

			builder.RegisterInstance(noteEditor);

			builder.RegisterInstance(noteReader);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.EditNote(file);

		// Assert
		await textInputPrompt.Received(1).RequestMultilineTextAsync(
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
			ITextInputPrompt textInputPrompt = Substitute.For<ITextInputPrompt>();

			textInputPrompt
				.RequestMultilineTextAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<bool>(),
					Arg.Any<CancellationToken>())
				.Returns(new TextInputResult());

			builder.RegisterInstance(textInputPrompt);

			builder.RegisterInstance(noteEditor);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

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
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

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
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(folderProtection);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
	/// <see cref="EditorViewModel.EncryptFolder" />: a folder without files is not encrypted.
	/// </summary>
	[Test]
	public async Task EncryptFolder_Does_Nothing_If_Missing_Files()
	{
		// Arrange
		IFolderProtection folderProtection = Substitute.For<IFolderProtection>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(folderProtection);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.EncryptFolder(ItemDtoFactory.CreateFolderDto());

		// Assert
		await folderProtection
			.DidNotReceiveWithAnyArgs()
			.EncryptFolderAsync(default!, default!);
	}

	/// <summary>
	/// <see cref="EditorViewModel.EncryptFolderCommand" /> CanExecute: only a folder that belongs to no keeper, itself
	/// included, and holds no protected contents is encrypted, and not in read-only mode.
	/// </summary>
	[Test]
	[TestCase(false, false, EncryptionStatus.None, false, ExpectedResult = true)]
	[TestCase(true, false, EncryptionStatus.None, false, ExpectedResult = false)]
	[TestCase(false, true, EncryptionStatus.None, false, ExpectedResult = false)]
	[TestCase(false, false, EncryptionStatus.Encrypted, false, ExpectedResult = false)]
	[TestCase(false, false, EncryptionStatus.Decrypted, false, ExpectedResult = false)]
	[TestCase(false, false, EncryptionStatus.None, true, ExpectedResult = false)]
	public bool EncryptFolderCommand_CanExecute_Allows_Only_A_Folder_Without_Protection(
		bool isInsideKeeper,
		bool isKeeper,
		EncryptionStatus childStatus,
		bool isReadOnly)
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto child = ItemDtoFactory.CreateFileDto(encryptionStatus: childStatus);

		child.Parent = folder;

		folder
			.Children
			.Add(child);

		if (isKeeper)
		{
			folder.EncryptedDek = RandomValues.CreateBytes(10);
		}

		if (isInsideKeeper)
		{
			FolderDto keeper = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Encrypted);

			keeper.EncryptedDek = RandomValues.CreateBytes(10);

			folder.Parent = keeper;
		}

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.IsReadOnly = isReadOnly;

		// Act
		return sut
			.EncryptFolderCommand
			.CanExecute(folder);
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

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<FileExecutionScenario>()
				.As<IFileExecutionScenario>();
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
	/// <see cref="EditorViewModel.ExecuteFile" />: a protected file whose contents stay hidden, as the password was not given,
	/// is neither read nor run.
	/// </summary>
	[Test]
	public async Task ExecuteFile_Does_Not_Run_A_Locked_File_Left_Closed()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		IProtectionScenarios protectionScenarios = Substitute.For<IProtectionScenarios>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// The password is not given.
			protectionScenarios
				.ShowFileContentsAsync(dto)
				.Returns(false);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(protectionScenarios);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<FileExecutionScenario>()
				.As<IFileExecutionScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ExecuteFile(dto);

		// Assert
		await protectionScenarios
			.Received(1)
			.ShowFileContentsAsync(dto);

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

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<FileExecutionScenario>()
				.As<IFileExecutionScenario>();
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

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder.RegisterInstance(sessionKeyStore);

			// The real cipher, so the contents reach the session key whichever call opens them.
			builder
				.RegisterType<ContentCipher>()
				.As<IContentCipher>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<FileExecutionScenario>()
				.As<IFileExecutionScenario>();
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

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<FileExecutionScenario>()
				.As<IFileExecutionScenario>();
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

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<FileExecutionScenario>()
				.As<IFileExecutionScenario>();
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

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<FileExecutionScenario>()
				.As<IFileExecutionScenario>();
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
	/// <see cref="EditorViewModel.ExecuteFile" />: a file that is not protected runs with its stored contents and without a
	/// key, even under a keeper.
	/// </summary>
	[Test]
	public async Task ExecuteFile_Runs_A_File_That_Is_Not_Protected_Without_A_Key()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		dto.Parent = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		byte[] stored = RandomValues.CreateBytes(10);

		ExecuteFileParameters? parameters = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents contents = new()
			{
				Contents = stored,
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(dto.Id)
				.Returns(contents);

			IExecutionEngine engine = Substitute.For<IExecutionEngine>();

			engine
				.ExecuteAsync(Arg.Do<ExecuteFileParameters>(x => parameters = x))
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(engine);

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<FileExecutionScenario>()
				.As<IFileExecutionScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ExecuteFile(dto);

		// Assert
		parameters!.Contents
			.Should()
			.Equal(stored);

		parameters.KeeperId
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ExecuteFile" />: a file that may run a program or a script runs only once the user agrees.
	/// </summary>
	[Test]
	public async Task ExecuteFile_Runs_An_Executable_Only_When_Confirmed([Values] bool isConfirmed)
	{
		// Arrange
		IExecutionEngine engine = Substitute.For<IExecutionEngine>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>())
				.Returns(isConfirmed);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents contents = new()
			{
				Contents = RandomValues.CreateBytes(10),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(contents);

			engine
				.ExecuteAsync(Arg.Any<ExecuteFileParameters>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(engine);

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<FileExecutionScenario>()
				.As<IFileExecutionScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ExecuteFile(ItemDtoFactory.CreateNamedFileDto("setup.exe"));

		// Assert
		await engine
			.Received(isConfirmed ? 1 : 0)
			.ExecuteAsync(Arg.Any<ExecuteFileParameters>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ExecuteFile" />: the file runs in the read-only mode of the editor, which keeps its changes
	/// out of the database.
	/// </summary>
	[Test]
	public async Task ExecuteFile_Runs_The_File_In_The_Read_Only_Mode([Values] bool isReadOnly)
	{
		// Arrange
		ExecuteFileParameters? parameters = null;

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

			IExecutionEngine engine = Substitute.For<IExecutionEngine>();

			engine
				.ExecuteAsync(Arg.Do<ExecuteFileParameters>(x => parameters = x))
				.Returns(true);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(engine);

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<FileExecutionScenario>()
				.As<IFileExecutionScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.IsReadOnly = isReadOnly;

		// Act
		await sut.ExecuteFile(ItemDtoFactory.CreateFileDto());

		// Assert
		parameters!.IsReadOnly
			.Should()
			.Be(isReadOnly);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ExpandCollapseAllFoldersAsync" />: all folders are expanded or collapsed, and the selection is kept on expand and reset on collapse.
	/// </summary>
	[Test]
	public async Task ExpandCollapseAllFoldersAsync_Applies_To_All_Folders_And_Resets_Selection_On_Collapse([Values] bool isExpandAll)
	{
		// Arrange
		FolderDto selectedFolder = ItemDtoFactory.CreateFolderDto();

		FolderDto[] folders = [.. ItemDtoFactory.CreateFolderDtos(5)];

		folders = [.. folders, .. selectedFolder.ToEnumerable()];

		foreach (FolderDto folder in folders)
		{
			foreach (FolderDto child in ItemDtoFactory.CreateFolderDtos(5))
			{
				child.Parent = folder;

				folder.Children.Add(child);
			}
		}

		WorkSession session = new();

		folders
			.GetFolders()
			.ForEach(x => session.SetExpanded(x.Id, !isExpandAll));

		session.SetSelected(selectedFolder.Id, true);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(session)
			.AsImplementedInterfaces());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.AddRange(folders);

		sut.SelectedNode = sut.Nodes.Single(x => x.Model == selectedFolder);

		// Act
		await sut.ExpandCollapseAllFoldersAsync(isExpandAll);

		// Assert
		folders.GetFolders().Select(x => session.IsExpanded(x.Id))
			.Should()
			.OnlyContain(x => x == isExpandAll);

		if (isExpandAll)
		{
			sut.SelectedNode
				.Should()
				.NotBeNull();

			session.IsSelected(selectedFolder.Id)
				.Should()
				.BeTrue();
		}
		else
		{
			sut.SelectedNode
				.Should()
				.BeNull();

			session.IsSelected(selectedFolder.Id)
				.Should()
				.BeFalse();
		}
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
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(contentVisibility);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.AddRange(editingFiles.Concat(executingFiles));

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
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.AddRange([file]);

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
	/// <see cref="EditorViewModel.HideAllFileContents" />: a file open with contents that are not protected stays open, and
	/// nobody is asked to close it.
	/// </summary>
	[Test]
	public async Task HideAllFileContents_Leaves_An_Open_Plain_File_Open()
	{
		// Arrange
		FileDto plain = ItemDtoFactory.CreateFileDto(isEditing: true);

		IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(confirmationPrompt);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(plain);

		// Act
		await sut.HideAllFileContents();

		// Assert
		plain.IsEditing
			.Should()
			.BeTrue();

		await confirmationPrompt
			.DidNotReceive()
			.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.HideAllFileContents" />: hidden contents refresh the command that hides everything.
	/// </summary>
	[Test]
	public async Task HideAllFileContents_Refreshes_The_Hide_All_Command()
	{
		// Arrange
		bool isRefreshed = false;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IProtectionScenarios protectionScenarios = Substitute.For<IProtectionScenarios>();

			protectionScenarios
				.HideAllContentsAsync(Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(protectionScenarios);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.HideAllFileContentsCommand.CanExecuteChanged += (_, _) => isRefreshed = true;

		// Act
		await sut.HideAllFileContents();

		// Assert
		isRefreshed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorViewModel.HideAllFileContents" />: hidden contents leave the auto-lock nothing to wait for.
	/// </summary>
	[Test]
	public async Task HideAllFileContents_Tells_The_Auto_Lock()
	{
		// Arrange
		IBackgroundServiceControl backgroundServiceControl = Substitute.For<IBackgroundServiceControl>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(backgroundServiceControl);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.HideAllFileContents();

		// Assert
		backgroundServiceControl
			.Received(1)
			.RefreshAutoLock();
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
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(contentVisibility);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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

		IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		IMessenger messenger = new StrongReferenceMessenger();

		object recipient = new();

		messenger.Register<FlushEditorsMessage>(recipient, static (_, message) => message.Reply(false));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.HideFileContents(file);

		// Assert
		await confirmationPrompt
			.DidNotReceive()
			.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

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
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(contentVisibility);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
	/// <see cref="EditorViewModel.HierarchySummary" />: a new editor counts the objects of the tree of the work session.
	/// </summary>
	[Test]
	public void HierarchySummary_Counts_The_Objects_Of_The_Session_Tree()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy = [.. ItemDtoFactory.CreateFolderDtos(5).Concat<ExplorerItemDtoBase>(ItemDtoFactory.CreateFileDtos(5))];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			WorkSession session = new();

			session
				.Hierarchy
				.AddRange(hierarchy);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		// Act
		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Assert
		sut.HierarchySummary
			.Should()
			.Be(hierarchy.GetCount().AsString());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: an import that replaces the tree empties the copy history.
	/// </summary>
	[Test]
	public async Task Import_Clears_The_Copy_History_On_Replacement()
	{
		// Arrange
		ICopyHistoryRecorder copyHistory = Substitute.For<ICopyHistoryRecorder>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			dataExchange
				.ImportDataAsync(Arg.Any<IReadOnlyCollection<ExplorerItemDtoBase>>())
				.Returns(new ImportDataResult(LoadedHierarchy.Empty, ImportMode.Replace));

			builder.RegisterInstance(copyHistory);

			builder.RegisterInstance(dataExchange);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ImportScenario>()
				.As<IImportScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.Import();

		// Assert
		copyHistory
			.Received(1)
			.Clear();
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: an import that replaces leaves only the imported objects in the tree.
	/// </summary>
	[Test]
	public async Task Import_Clears_The_Hierarchy_On_Replacement()
	{
		// Arrange
		FolderDto imported = ItemDtoFactory.CreateFolderDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			dataExchange
				.ImportDataAsync(Arg.Any<IReadOnlyCollection<ExplorerItemDtoBase>>())
				.Returns(new ImportDataResult(
					ItemDtoFactory.CreateLoadedHierarchy([imported]),
					ImportMode.Replace));

			builder.RegisterInstance(dataExchange);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ImportScenario>()
				.As<IImportScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(ItemDtoFactory.CreateFolderDto());

		// Act
		await sut.Import();

		// Assert
		sut.Hierarchy
			.Should()
			.Equal(imported);
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: the open files are closed before the import starts.
	/// </summary>
	[Test]
	public async Task Import_Closes_The_Open_Files_First()
	{
		// Arrange
		FileDto opened = ItemDtoFactory.CreateFileDto(isEditing: true);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ImportScenario>()
				.As<IImportScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(opened);

		// Act
		await sut.Import();

		// Assert
		opened.IsEditing
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: an import that replaces the tree closes the right side sheet, and one that
	/// appends leaves it open.
	/// </summary>
	[Test]
	public async Task Import_Closes_The_Right_Side_Sheet_Only_On_Replacement([Values] bool isReplacement)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			dataExchange
				.ImportDataAsync(Arg.Any<IReadOnlyCollection<ExplorerItemDtoBase>>())
				.Returns(new ImportDataResult(
					LoadedHierarchy.Empty,
					isReplacement ? ImportMode.Replace : ImportMode.Append));

			builder.RegisterInstance(dataExchange);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ImportScenario>()
				.As<IImportScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.IsRightSideSheetOpened = true;

		// Act
		await sut.Import();

		// Assert
		sut.IsRightSideSheetOpened
			.Should()
			.Be(!isReplacement);
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: an import counts the objects of the tree again.
	/// </summary>
	[Test]
	public async Task Import_Counts_The_Objects_Again()
	{
		// Arrange
		FileDto imported = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			dataExchange
				.ImportDataAsync(Arg.Any<IReadOnlyCollection<ExplorerItemDtoBase>>())
				.Returns(new ImportDataResult(
					ItemDtoFactory.CreateLoadedHierarchy([imported]),
					ImportMode.Append));

			builder.RegisterInstance(dataExchange);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ImportScenario>()
				.As<IImportScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.Import();

		// Assert
		sut.HierarchySummary
			.Should()
			.Be(new ExplorerItemDtoBase[] { imported }.GetCount().AsString());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: an import that replaces the hierarchy drops the keys of the folders it removed.
	/// </summary>
	[Test]
	public async Task Import_Drops_The_Keys_On_Replacement()
	{
		// Arrange
		IContentKeys contentKeys = Substitute.For<IContentKeys>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			dataExchange
				.ImportDataAsync(Arg.Any<IReadOnlyCollection<ExplorerItemDtoBase>>())
				.Returns(new ImportDataResult(LoadedHierarchy.Empty, ImportMode.Replace));

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(contentKeys);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ImportScenario>()
				.As<IImportScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.Import();

		// Assert
		contentKeys
			.Received(1)
			.DiscardAllKeys();
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: an import that replaces forgets the expanded and selected objects of the tree
	/// replaced, also for an object read back with the same identifier.
	/// </summary>
	[Test]
	public async Task Import_Forgets_The_Tree_State_On_Replacement()
	{
		// Arrange
		FolderDto replaced = ItemDtoFactory.CreateFolderDto();

		FolderDto imported = ItemDtoFactory.CreateFolderDto(id: replaced.Id);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			WorkSession session = new();

			session.SetExpanded(replaced.Id, true);

			session.SetSelected(replaced.Id, true);

			dataExchange
				.ImportDataAsync(Arg.Any<IReadOnlyCollection<ExplorerItemDtoBase>>())
				.Returns(new ImportDataResult(
					ItemDtoFactory.CreateLoadedHierarchy([imported]),
					ImportMode.Replace));

			builder.RegisterInstance(dataExchange);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<ImportScenario>()
				.As<IImportScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(replaced);

		// Act
		await sut.Import();

		// Assert
		ExplorerNodeViewModelBase row = sut.Nodes.Single();

		row.Model
			.Should()
			.BeSameAs(imported);

		bool[] states = [row.IsExpanded, row.IsSelected];

		states
			.Should()
			.OnlyContain(static x => !x);
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: a refusal to close the open files leaves the import unstarted.
	/// </summary>
	[Test]
	public async Task Import_Imports_Nothing_When_The_Open_Files_Stay_Open()
	{
		// Arrange
		FileDto opened = ItemDtoFactory.CreateFileDto(isEditing: true);

		IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			// The user keeps the files open.
			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(dataExchange);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ImportScenario>()
				.As<IImportScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(opened);

		// Act
		await sut.Import();

		// Assert
		opened.IsEditing
			.Should()
			.BeTrue();

		await dataExchange
			.DidNotReceive()
			.ImportDataAsync(Arg.Any<IReadOnlyCollection<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: an import that appends puts the imported objects after those in the tree.
	/// </summary>
	[Test]
	public async Task Import_Keeps_The_Hierarchy_On_Appending()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FolderDto imported = ItemDtoFactory.CreateFolderDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			dataExchange
				.ImportDataAsync(Arg.Any<IReadOnlyCollection<ExplorerItemDtoBase>>())
				.Returns(new ImportDataResult(
					ItemDtoFactory.CreateLoadedHierarchy([imported]),
					ImportMode.Append));

			builder.RegisterInstance(dataExchange);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ImportScenario>()
				.As<IImportScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(folder);

		// Act
		await sut.Import();

		// Assert
		sut.Hierarchy
			.Should()
			.Equal(folder, imported);
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: a failed import leaves the tree as it is.
	/// </summary>
	[Test]
	public async Task Import_Keeps_The_Hierarchy_When_The_Import_Fails()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			// The import fails.
			dataExchange
				.ImportDataAsync(Arg.Any<IReadOnlyCollection<ExplorerItemDtoBase>>())
				.Returns((ImportDataResult?)null);

			builder.RegisterInstance(dataExchange);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ImportScenario>()
				.As<IImportScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(folder);

		// Act
		await sut.Import();

		// Assert
		sut.Hierarchy
			.Should()
			.Equal(folder);
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: an import that appends leaves the hierarchy in place, keys included.
	/// </summary>
	[Test]
	public async Task Import_Keeps_The_Keys_On_Appending()
	{
		// Arrange
		IContentKeys contentKeys = Substitute.For<IContentKeys>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			dataExchange
				.ImportDataAsync(Arg.Any<IReadOnlyCollection<ExplorerItemDtoBase>>())
				.Returns(new ImportDataResult(LoadedHierarchy.Empty, ImportMode.Append));

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(contentKeys);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ImportScenario>()
				.As<IImportScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.Import();

		// Assert
		contentKeys
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
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(dataExchange);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ImportScenario>()
				.As<IImportScenario>();
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
			.ImportDataAsync(Arg.Any<IReadOnlyCollection<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Import" />: the rows of the imported objects show them expanded and selected as they were
	/// stored, and that is not saved once more.
	/// </summary>
	[Test]
	public async Task Import_Shows_The_Imported_Objects_As_They_Were_Stored()
	{
		// Arrange
		FolderDto expanded = ItemDtoFactory.CreateFolderDto();

		FileDto selected = ItemDtoFactory.CreateFileDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDataExchangeService dataExchange = Substitute.For<IDataExchangeService>();

			dataExchange
				.ImportDataAsync(Arg.Any<IReadOnlyCollection<ExplorerItemDtoBase>>())
				.Returns(new ImportDataResult(
					ItemDtoFactory.CreateLoadedHierarchy(
						[expanded, selected],
						expandedIds: [expanded.Id],
						selectedIds: [selected.Id]),
					ImportMode.Append));

			builder.RegisterInstance(dataExchange);

			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ImportScenario>()
				.As<IImportScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// The window of the editor is shown, so that a change of the selection would be saved.
		sut.IsWindowShown = true;

		// Act
		await sut.Import();

		// Assert
		bool[] states =
		[
			sut.Nodes[0].IsExpanded,
			sut.Nodes[1].IsSelected,
			sut.Nodes[0].IsSelected,
			sut.Nodes[1].IsExpanded
		];

		states
			.Should()
			.Equal(true, true, false, false);

		await propertyWriter.DidNotReceive().UpdateIsExpandedAsync(
			Arg.Any<Guid>(),
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());

		await propertyWriter.DidNotReceive().UpdateIsSelectedAsync(
			Arg.Any<ExplorerItemDtoBase>(),
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Initialize" />: the navigation column and the read-only mode come from the supplied settings.
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
			WindowState = WindowShowState.Normal,
			X = positiveValue,
			Y = positiveValue
		};

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.Initialize(windowSettings);

		// Assert
		sut.NavigationColumnWidth
			.Should()
			.Be(windowSettings.NavigationColumnWidth);

		sut.IsReadOnly
			.Should()
			.Be(windowSettings.IsReadOnly);
	}

	/// <summary>
	/// <see cref="EditorViewModel.IsReadOnly" />: a change of the mode refreshes the commands that add and delete objects.
	/// </summary>
	[Test]
	public void IsReadOnly_Refreshes_The_Add_And_Delete_Commands()
	{
		// Arrange
		List<string> refreshed = [];

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.AddCommand.CanExecuteChanged += (_, _) => refreshed.Add(nameof(sut.AddCommand));

		sut.DeleteCommand.CanExecuteChanged += (_, _) => refreshed.Add(nameof(sut.DeleteCommand));

		// Act
		sut.IsReadOnly = true;

		// Assert
		refreshed
			.Should()
			.BeEquivalentTo(nameof(sut.AddCommand), nameof(sut.DeleteCommand));
	}

	/// <summary>
	/// <see cref="EditorViewModel.IsReadOnly" />: a change of the mode reaches the embedded editors.
	/// </summary>
	[Test]
	public void IsReadOnly_Tells_The_Embedded_Editors()
	{
		// Arrange
		IMessenger messenger = new StrongReferenceMessenger();

		EditorReadOnlyModeChangedMessage? sent = null;

		messenger.Register<EditorReadOnlyModeChangedMessage>(this, (_, message) => sent = message);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(messenger);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.IsReadOnly = true;

		// Assert
		sent
			.Should()
			.Be(new EditorReadOnlyModeChangedMessage(true));
	}

	/// <summary>
	/// <see cref="EditorViewModel.IsRightSideSheetOpened" />: closing the right side sheet disposes the list of the copy
	/// history it showed.
	/// </summary>
	[Test]
	public void IsRightSideSheetOpened_Disposes_The_Copy_History_List_When_The_Sheet_Closes()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.ShowCopyHistoryCommand
			.Execute(null);

		CopyHistoryViewModel copyHistoryList = sut.CopyHistoryList!;

		// Act
		sut.IsRightSideSheetOpened = false;

		// Assert
		copyHistoryList.IsDisposed
			.Should()
			.BeTrue();

		sut.CopyHistoryList
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EditorViewModel.NavigationColumnWidth" />: the width is clamped to less than the view width when the view shrinks.
	/// </summary>
	[Test]
	public void NavigationColumnWidth_Is_Clamped_To_Less_Than_The_View_Width()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		const double initialViewWidth = 1000.0;

		sut.ViewWidth = initialViewWidth;

		sut.NavigationColumnWidth = initialViewWidth / 2;

		// Act
		sut.ViewWidth = initialViewWidth / 4;

		// Assert
		sut.NavigationColumnWidth
			.Should()
			.BeLessThan(sut.ViewWidth);
	}

	/// <summary>
	/// <see cref="EditorViewModel.Nodes" />: the row of an object that leaves the tree is disposed.
	/// </summary>
	[Test]
	public void Nodes_Dispose_The_Row_Of_An_Object_That_Leaves_The_Tree()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(folder);

		ExplorerNodeViewModelBase row = sut.Nodes.Single();

		// Act
		sut.Hierarchy.Remove(folder);

		// Assert
		row.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorViewModel.Nodes" />: in read-only mode an expanded folder is not saved.
	/// </summary>
	[Test]
	public async Task Nodes_In_Read_Only_Mode_Saves_No_Expanded_Folder()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(folder);

		sut.IsReadOnly = true;

		// Act
		sut.Nodes.Single().IsExpanded = true;

		// Assert
		await propertyWriter.DidNotReceive().UpdateIsExpandedAsync(
			folder.Id,
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Nodes" />: a folder added to the tree after it was shown is saved when it is expanded.
	/// </summary>
	[Test]
	public async Task Nodes_Saves_A_Folder_Added_Later_When_It_Is_Expanded()
	{
		// Arrange
		FolderDto parent = ItemDtoFactory.CreateFolderDto();

		FolderDto child = ItemDtoFactory.CreateFolderDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(parent);

		child.Parent = parent;

		parent.Children.Add(child);

		// Act
		sut.Nodes.FindNode(child)!.IsExpanded = true;

		// Assert
		await propertyWriter.Received(1).UpdateIsExpandedAsync(
			child.Id,
			true,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Nodes" />: a folder of the tree is saved in the state it is turned to, expanded or
	/// collapsed.
	/// </summary>
	[Test]
	public async Task Nodes_Saves_A_Folder_Expanded_Or_Collapsed([Values] bool isExpanded)
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			WorkSession session = new();

			session.SetExpanded(folder.Id, !isExpanded);

			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(folder);

		// Act
		sut.Nodes.Single().IsExpanded = isExpanded;

		// Assert
		await propertyWriter.Received(1).UpdateIsExpandedAsync(
			folder.Id,
			isExpanded,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Nodes" />: a folder inside another folder is saved when it is expanded.
	/// </summary>
	[Test]
	public async Task Nodes_Saves_A_Nested_Folder_When_It_Is_Expanded()
	{
		// Arrange
		FolderDto parent = ItemDtoFactory.CreateFolderDto();

		FolderDto child = ItemDtoFactory.CreateFolderDto();

		child.Parent = parent;

		parent.Children.Add(child);

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(parent);

		// Act
		sut.Nodes.FindNode(child)!.IsExpanded = true;

		// Assert
		await propertyWriter.Received(1).UpdateIsExpandedAsync(
			child.Id,
			true,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Nodes" />: a folder that left the tree is not saved any more.
	/// </summary>
	[Test]
	public async Task Nodes_Saves_No_Folder_After_It_Left_The_Tree()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(folder);

		ExplorerNodeViewModelBase row = sut.Nodes.Single();

		sut.Hierarchy.Remove(folder);

		// Act
		row.IsExpanded = true;

		// Assert
		await propertyWriter.DidNotReceive().UpdateIsExpandedAsync(
			folder.Id,
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Nodes" />: while an action runs, an expanded folder is not saved.
	/// </summary>
	[Test]
	public async Task Nodes_While_An_Action_Runs_Saves_No_Expanded_Folder()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(folder);

		sut.Receive(new ShowProgressBarMessage(true));

		// Act
		sut.Nodes.Single().IsExpanded = true;

		// Assert
		await propertyWriter.DidNotReceive().UpdateIsExpandedAsync(
			folder.Id,
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.NotifyDecryptedContentsChanged" />: decrypted contents keep the countdown running.
	/// </summary>
	[Test]
	public void NotifyDecryptedContentsChanged_Arms_The_Countdown_For_Decrypted_Contents()
	{
		// Arrange
		IAutoLockService autoLock = Substitute.For<IAutoLockService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(autoLock);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.AddRange([ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)]);

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

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(autoLock);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.AddRange([ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted)]);

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
	/// <see cref="EditorViewModel.Receive(CloseEditingFileMessage)" />: a file the editor is asked to close loses its tab.
	/// </summary>
	[Test]
	public void Receive_CloseEditingFile_Closes_The_Tab()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.EditingFiles.OpenInEditor(file);

		// Act
		sut.Receive(new CloseEditingFileMessage(file));

		// Assert
		sut.EditingFiles.Items
			.Should()
			.BeEmpty();

		file.IsEditing
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EditorViewModel.Receive(ConfirmUpdateMessage)" />: the question goes to the dialog, and its answer is
	/// the reply.
	/// </summary>
	[Test]
	public async Task Receive_ConfirmUpdate_Replies_With_The_Answer_Of_The_Dialog()
	{
		// Arrange
		const string text = "Version 0.2.0 is available.";

		IMessenger messenger = new StrongReferenceMessenger();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesNoAsync(text, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(messenger);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		_ = mock.Create<EditorViewModel>();

		// Act
		bool answer = await messenger.Send(new ConfirmUpdateMessage(text, CancellationToken.None));

		// Assert
		answer
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorViewModel.Receive(SessionAutoLockedMessage)" />: the expiry closes every open file, the plain ones
	/// too.
	/// </summary>
	[Test]
	public async Task Receive_SessionAutoLocked_Closes_Every_Open_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(isEditing: true);

		IMessenger messenger = new WeakReferenceMessenger();

		List<Task> scheduled = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler
				.When(static x => x.Watch(Arg.Any<Task>()))
				.Do(callInfo => scheduled.Add(callInfo.Arg<Task>()));

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance(exceptionHandler);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.AddRange([file]);

		// Act
		messenger.Send(new SessionAutoLockedMessage());

		await Task.WhenAll([.. scheduled]);

		// Assert
		file.IsEditing
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EditorViewModel.Receive(SessionAutoLockedMessage)" />: the expiry closes the open files and hides every content.
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

		WorkSession session = new();

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

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.AddRange([editingFile, executingFile]);

		session.EditorTabs = new()
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

		session.EditorTabs
			.Should()
			.BeNull();

		contentVisibility
			.Received(1)
			.HideAllContents(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.Receive(SessionAutoLockedMessage)" />: an editor that cannot persist its changes
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

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.AddRange([file]);

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
	/// <see cref="EditorViewModel.Receive(SessionAutoLockedMessage)" />: once the contents are hidden, the command that hides
	/// them all is asked again whether it can run.
	/// </summary>
	[Test]
	public async Task Receive_SessionAutoLocked_Refreshes_The_Hide_All_Command()
	{
		// Arrange
		bool isRefreshed = false;

		IMessenger messenger = new WeakReferenceMessenger();

		List<Task> scheduled = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler
				.When(static x => x.Watch(Arg.Any<Task>()))
				.Do(callInfo => scheduled.Add(callInfo.Arg<Task>()));

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance(exceptionHandler);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.HideAllFileContentsCommand.CanExecuteChanged += (_, _) => isRefreshed = true;

		// Act
		messenger.Send(new SessionAutoLockedMessage());

		await Task.WhenAll([.. scheduled]);

		// Assert
		isRefreshed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorViewModel.Receive(SessionAutoLockedMessage)" />: the countdown is stopped once, after the contents are
	/// hidden.
	/// </summary>
	[Test]
	public async Task Receive_SessionAutoLocked_Stops_The_Countdown()
	{
		// Arrange
		IAutoLockService autoLock = Substitute.For<IAutoLockService>();

		IMessenger messenger = new WeakReferenceMessenger();

		List<Task> scheduled = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler
				.When(static x => x.Watch(Arg.Any<Task>()))
				.Do(callInfo => scheduled.Add(callInfo.Arg<Task>()));

			builder.RegisterInstance(autoLock);

			builder.RegisterInstance(messenger).As<IMessenger>();

			builder.RegisterInstance(exceptionHandler);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<ContentVisibility>()
				.As<IContentVisibility>();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.Hierarchy
			.AddRange([ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)]);

		// Act
		messenger.Send(new SessionAutoLockedMessage());

		await Task.WhenAll([.. scheduled]);

		// Assert
		autoLock
			.Received(1)
			.Stop();

		autoLock
			.DidNotReceive()
			.Arm();
	}

	/// <summary>
	/// <see cref="EditorViewModel.Receive(ShowInEditorMessage)" />: the folders that hold the object are expanded.
	/// </summary>
	[Test]
	public void Receive_ShowInEditor_Expands_The_Folders_Of_The_Object()
	{
		// Arrange
		FolderDto outer = ItemDtoFactory.CreateFolderDto();

		FolderDto inner = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		inner.Parent = outer;

		outer.Children.Add(inner);

		file.Parent = inner;

		inner.Children.Add(file);

		WorkSession session = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(session)
			.AsImplementedInterfaces());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(outer);

		// Act
		sut.Receive(new ShowInEditorMessage(file.Id));

		// Assert
		FolderDto[] folders = [outer, inner];

		folders.Select(x => session.IsExpanded(x.Id))
			.Should()
			.OnlyContain(static x => x);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ResetSelectedObject" />: SelectedNode is cleared and the row's IsSelected is reset.
	/// </summary>
	[Test]
	public void ResetSelectedObject_Resets_IsSelected_Property_And_SelectedNode()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		WorkSession session = new();

		session.SetSelected(dto.Id, true);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(session)
			.AsImplementedInterfaces());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(dto);

		sut.SelectedNode = sut.Nodes.Single();

		// Act
		sut.ResetSelectedObject();

		// Assert
		sut.SelectedNode
			.Should()
			.BeNull();

		session.IsSelected(dto.Id)
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

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

			WorkSession session = new();

			session.SetSelected(selected.Id, true);

			propertyWriter
				.When(x => x.UpdateIsSelectedAsync(
					Arg.Any<ExplorerItemDtoBase>(),
					Arg.Any<bool>(),
					Arg.Any<CancellationToken>()))
				.Do(call => saved.Add((call.ArgAt<ExplorerItemDtoBase>(0).Id, call.ArgAt<bool>(1))));

			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// The window of the editor is shown, as in the application.
		sut.IsWindowShown = true;

		sut.Hierarchy.Add(selected);

		sut.SelectedNode = sut.Nodes.Single();

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
	/// <see cref="EditorViewModel.ResetSelectedObjectCommand" />: the command runs only while a row of the tree is selected.
	/// </summary>
	[Test]
	public void ResetSelectedObjectCommand_Needs_A_Selected_Row()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(ItemDtoFactory.CreateFileDto());

		bool canRunUnselected = sut.ResetSelectedObjectCommand.CanExecute(null);

		// Act
		sut.SelectedNode = sut.Nodes.Single();

		// Assert
		canRunUnselected
			.Should()
			.BeFalse();

		sut.ResetSelectedObjectCommand.CanExecute(null)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorViewModel.RestartApplication" />: the application is asked to start again once it stops.
	/// </summary>
	[Test]
	public void RestartApplication_Asks_For_A_Restart()
	{
		// Arrange
		List<RestartAppMessage> sent = [];

		IMessenger messenger = new StrongReferenceMessenger();

		messenger.Register<RestartAppMessage>(this, (_, message) => sent.Add(message));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(messenger);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.RestartApplication();

		// Assert
		sent
			.Should()
			.ContainSingle();
	}

	/// <summary>
	/// <see cref="EditorViewModel.RestartAutoLockCommand" />: the countdown starts over from the delay in the settings.
	/// </summary>
	[Test]
	public void RestartAutoLockCommand_Starts_The_Countdown_Over()
	{
		// Arrange
		IAutoLockService autoLock = Substitute.For<IAutoLockService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(autoLock);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

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
	/// <see cref="EditorViewModel.RestoreSelection" />: the row of the object the work session keeps selected is selected
	/// again without being saved once more.
	/// </summary>
	[Test]
	public async Task RestoreSelection_Selects_The_Row_Of_The_Object_Kept_Selected()
	{
		// Arrange
		FileDto selected = ItemDtoFactory.CreateFileDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			WorkSession session = new();

			session.SetSelected(selected.Id, true);

			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// The window of the editor is shown, so that a change of the selection would be saved.
		sut.IsWindowShown = true;

		sut.Hierarchy.AddRange([ItemDtoFactory.CreateFileDto(), selected]);

		// Act
		sut.RestoreSelection();

		// Assert
		sut.SelectedNode!.Model
			.Should()
			.BeSameAs(selected);

		await propertyWriter.DidNotReceive().UpdateIsSelectedAsync(
			Arg.Any<ExplorerItemDtoBase>(),
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.SelectedNode" />: the row left is cleared and its object saved as not selected, also when
	/// the tree never showed the row and so did not clear it.
	/// </summary>
	[Test]
	public void SelectedNode_Clears_The_Row_Left_That_The_Tree_Did_Not()
	{
		// Arrange
		List<(Guid Id, bool IsSelected)> saved = [];

		FileDto left = ItemDtoFactory.CreateFileDto();

		FileDto chosen = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

			WorkSession session = new();

			session.SetSelected(left.Id, true);

			propertyWriter
				.When(x => x.UpdateIsSelectedAsync(
					Arg.Any<ExplorerItemDtoBase>(),
					Arg.Any<bool>(),
					Arg.Any<CancellationToken>()))
				.Do(call => saved.Add((call.ArgAt<ExplorerItemDtoBase>(0).Id, call.ArgAt<bool>(1))));

			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// The window of the editor is shown, as in the application.
		sut.IsWindowShown = true;

		sut.Hierarchy.AddRange([left, chosen]);

		ExplorerNodeViewModelBase leftRow = sut.Nodes[0];

		ExplorerNodeViewModelBase chosenRow = sut.Nodes[1];

		sut.SelectedNode = leftRow;

		// Act
		// The tree marks the row chosen, while the row left was never shown.
		chosenRow.IsSelected = true;

		sut.SelectedNode = chosenRow;

		// Assert
		leftRow.IsSelected
			.Should()
			.BeFalse();

		saved
			.Should()
			.Equal((chosen.Id, true), (left.Id, false));
	}

	/// <summary>
	/// <see cref="EditorViewModel.SelectedNode" />: in read-only mode a selection in the tree is not saved.
	/// </summary>
	[Test]
	public async Task SelectedNode_In_Read_Only_Mode_Saves_No_Selection()
	{
		// Arrange
		FileDto chosen = ItemDtoFactory.CreateFileDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// The window of the editor is shown, as in the application.
		sut.IsWindowShown = true;

		sut.Hierarchy.Add(chosen);

		sut.IsReadOnly = true;

		ExplorerNodeViewModelBase row = sut.Nodes.Single();

		// Act
		row.IsSelected = true;

		sut.SelectedNode = row;

		// Assert
		await propertyWriter.DidNotReceive().UpdateIsSelectedAsync(
			Arg.Any<ExplorerItemDtoBase>(),
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.SelectedNode" />: the selection of an object removed from the tree is not saved, as the
	/// object is no longer in the database.
	/// </summary>
	[Test]
	public async Task SelectedNode_Saves_Nothing_For_An_Object_Removed_From_The_Tree()
	{
		// Arrange
		FileDto removed = ItemDtoFactory.CreateFileDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			WorkSession session = new();

			session.SetSelected(removed.Id, true);

			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// The window of the editor is shown, as in the application.
		sut.IsWindowShown = true;

		sut.Hierarchy.Add(removed);

		sut.SelectedNode = sut.Nodes.Single();

		sut.Hierarchy.Remove(removed);

		// Act
		// The tree drops the selection of an object it no longer holds.
		sut.SelectedNode = null;

		// Assert
		await propertyWriter.DidNotReceive().UpdateIsSelectedAsync(
			Arg.Any<ExplorerItemDtoBase>(),
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.SelectedNode" />: a selection moved in the tree saves the object left and the object
	/// chosen, each in its new state.
	/// </summary>
	[Test]
	public void SelectedNode_Saves_The_Object_Left_And_The_Object_Chosen()
	{
		// Arrange
		List<(Guid Id, bool IsSelected)> saved = [];

		FileDto left = ItemDtoFactory.CreateFileDto();

		FileDto chosen = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

			WorkSession session = new();

			session.SetSelected(left.Id, true);

			propertyWriter
				.When(x => x.UpdateIsSelectedAsync(
					Arg.Any<ExplorerItemDtoBase>(),
					Arg.Any<bool>(),
					Arg.Any<CancellationToken>()))
				.Do(call => saved.Add((call.ArgAt<ExplorerItemDtoBase>(0).Id, call.ArgAt<bool>(1))));

			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// The window of the editor is shown, as in the application.
		sut.IsWindowShown = true;

		sut.Hierarchy.AddRange([left, chosen]);

		ExplorerNodeViewModelBase leftRow = sut.Nodes[0];

		ExplorerNodeViewModelBase chosenRow = sut.Nodes[1];

		sut.SelectedNode = leftRow;

		// Only what the move of the selection saves counts.
		saved.Clear();

		// Act
		// The tree clears the row left and marks the row chosen before it reports the new selection.
		leftRow.IsSelected = false;

		chosenRow.IsSelected = true;

		sut.SelectedNode = chosenRow;

		// Assert
		saved
			.Should()
			.Equal((left.Id, false), (chosen.Id, true));
	}

	/// <summary>
	/// <see cref="EditorViewModel.SelectedNode" />: while an action runs, a selection in the tree is not saved.
	/// </summary>
	[Test]
	public async Task SelectedNode_While_An_Action_Runs_Saves_No_Selection()
	{
		// Arrange
		FileDto chosen = ItemDtoFactory.CreateFileDto();

		IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// The window of the editor is shown, as in the application.
		sut.IsWindowShown = true;

		sut.Hierarchy.Add(chosen);

		sut.Receive(new ShowProgressBarMessage(true));

		ExplorerNodeViewModelBase row = sut.Nodes.Single();

		// Act
		row.IsSelected = true;

		sut.SelectedNode = row;

		// Assert
		await propertyWriter.DidNotReceive().UpdateIsSelectedAsync(
			Arg.Any<ExplorerItemDtoBase>(),
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.SelectObjectCommand" />: the row selected before is saved as not selected, so one object
	/// at most stays selected in the database.
	/// </summary>
	[Test]
	public void SelectObjectCommand_Saves_The_Object_Left_As_Not_Selected()
	{
		// Arrange
		List<(Guid Id, bool IsSelected)> saved = [];

		FileDto left = ItemDtoFactory.CreateFileDto();

		FileDto chosen = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityPropertyWriter propertyWriter = Substitute.For<IEntityPropertyWriter>();

			WorkSession session = new();

			session.SetSelected(left.Id, true);

			propertyWriter
				.When(x => x.UpdateIsSelectedAsync(
					Arg.Any<ExplorerItemDtoBase>(),
					Arg.Any<bool>(),
					Arg.Any<CancellationToken>()))
				.Do(call => saved.Add((call.ArgAt<ExplorerItemDtoBase>(0).Id, call.ArgAt<bool>(1))));

			builder.RegisterInstance(propertyWriter);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// The window of the editor is shown, as in the application.
		sut.IsWindowShown = true;

		sut.Hierarchy.AddRange([left, chosen]);

		sut.SelectedNode = sut.Nodes[0];

		// Act
		sut
			.SelectObjectCommand
			.Execute(sut.Nodes[1]);

		// Assert
		saved
			.Should()
			.Equal((chosen.Id, true), (left.Id, false));
	}

	/// <summary>
	/// <see cref="EditorViewModel.SelectObjectCommand" />: the row is marked selected and becomes the SelectedNode.
	/// </summary>
	[Test]
	public void SelectObjectCommand_Selects_The_Row()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		WorkSession session = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(session)
			.AsImplementedInterfaces());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(dto);

		ExplorerNodeViewModelBase row = sut.Nodes.Single();

		// Act
		sut
			.SelectObjectCommand
			.Execute(row);

		// Assert
		session.IsSelected(dto.Id)
			.Should()
			.BeTrue();

		sut.SelectedNode
			.Should()
			.BeSameAs(row);
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

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

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
	/// <see cref="EditorViewModel.ShowFavorites" />: the switch to the favorites closes the editors of the open tabs.
	/// </summary>
	[AvaloniaTest]
	public void ShowFavorites_Closes_The_Editors()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(file);

		sut.EditingFiles.OpenInEditor(file);

		// Act
		sut.ShowFavorites();

		// Assert
		sut.EditingFiles.Items
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowFavorites" />: the switch to the favorites disposes the list of the shown copy
	/// history.
	/// </summary>
	[AvaloniaTest]
	public void ShowFavorites_Disposes_The_Shown_Copy_History_List()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut
			.ShowCopyHistoryCommand
			.Execute(null);

		CopyHistoryViewModel copyHistoryList = sut.CopyHistoryList!;

		// Act
		sut.ShowFavorites();

		// Assert
		copyHistoryList.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowFavorites" />: the work session keeps the tabs of the editor with the selected one
	/// and the previous one.
	/// </summary>
	[AvaloniaTest]
	public void ShowFavorites_Keeps_The_Editor_Tabs_In_The_Session()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(count: 3)];

		WorkSession session = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		EditingFilesViewModel editingFiles = sut.EditingFiles;

		sut.Hierarchy.AddRange(files);

		files.ForEach(editingFiles.OpenInEditor);

		editingFiles.SelectedIndex = 1;

		editingFiles.PreviousFile = files[2];

		// Act
		sut.ShowFavorites();

		// Assert
		EditorTabsState? kept = session.EditorTabs;

		kept!.Files
			.Should()
			.Equal(files);

		kept.SelectedFile
			.Should()
			.BeSameAs(files[1]);

		kept.PreviousFile
			.Should()
			.BeSameAs(files[2]);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowFavorites" />: the window gives way to the favorites window.
	/// </summary>
	[Test]
	public void ShowFavorites_Switches_To_The_Favorites()
	{
		// Arrange
		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		sut.ShowFavorites();

		// Assert
		mainWindowLauncher
			.Received(1)
			.SwitchToFavorites();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowFileContentsCommand" />: the auto-lock hears of the shown contents, and only when they
	/// show.
	/// </summary>
	[Test]
	public async Task ShowFileContentsCommand_Tells_The_Auto_Lock_Only_When_The_Contents_Show([Values] bool isShown)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		IBackgroundServiceControl backgroundServiceControl = Substitute.For<IBackgroundServiceControl>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

			contentVisibility
				.ShowFileContentsAsync(file, Arg.Any<CancellationToken>())
				.Returns(isShown);

			builder.RegisterInstance(backgroundServiceControl);

			builder.RegisterInstance(contentVisibility);

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut
			.ShowFileContentsCommand
			.ExecuteAsync(file);

		// Assert
		backgroundServiceControl
			.Received(isShown ? 1 : 0)
			.RefreshAutoLock();
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
			IConfirmationPrompt confirmationPrompt = Substitute.For<IConfirmationPrompt>();

			confirmationPrompt
				.RequestYesCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(confirmationPrompt);

			builder.RegisterInstance(contentVisibility);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<ExecutingFiles>()
				.As<IExecutingFiles>()
				.SingleInstance();

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
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
	/// <see cref="EditorViewModel.ShowFolderContents" />: a folder without files shows nothing.
	/// </summary>
	[Test]
	public async Task ShowFolderContents_Does_Nothing_If_Missing_Files()
	{
		// Arrange
		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(contentVisibility);

			builder
				.RegisterType<OpenFilesCloser>()
				.As<IOpenFilesCloser>();

			builder
				.RegisterType<ProtectionScenarios>()
				.As<IProtectionScenarios>();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut.ShowFolderContents(ItemDtoFactory.CreateFolderDto());

		// Assert
		await contentVisibility
			.DidNotReceiveWithAnyArgs()
			.ShowFolderContentsAsync(default!);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowHotkeysEditorCommand" />: the hotkey tracking pauses while the hotkey editor is open and
	/// starts again after it.
	/// </summary>
	[Test]
	public async Task ShowHotkeysEditorCommand_Pauses_The_Hotkey_Tracking_While_The_Editor_Is_Open()
	{
		// Arrange
		IHotkeysPrompt hotkeysPrompt = Substitute.For<IHotkeysPrompt>();

		IKeyboardInputHook hook = Substitute.For<IKeyboardInputHook>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			hotkeysPrompt
				.EditHotkeysAsync(Arg.Any<IEnumerable<KeyStroke>>())
				.Returns(new EditingHotkeysResult(IsSaved: false, NewHotkeys: []));

			hook
				.IsRunning
				.Returns(true);

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings(trackHotkeys: true));

			builder.RegisterInstance(hotkeysPrompt);

			// The hook is a lazy singleton that an earlier window has already used.
			Lazy<IKeyboardInputHook> lazyHook = new(() => hook);

			_ = lazyHook.Value;

			builder.RegisterInstance(lazyHook);

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<HotkeysEditingScenario>()
				.As<IHotkeysEditingScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut
			.ShowHotkeysEditorCommand
			.ExecuteAsync(ItemDtoFactory.CreateFileDto());

		// Assert
		Received.InOrder(() =>
		{
			hook.StopTrackingAsync(Arg.Any<CancellationToken>());

			hotkeysPrompt.EditHotkeysAsync(Arg.Any<IEnumerable<KeyStroke>>());

			hook.StartTrackingAsync(Arg.Any<CancellationToken>());
		});
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowHotkeysEditorCommand" />: the hotkey editor starts from the hotkeys of the file, and
	/// only the hotkeys it saves are written.
	/// </summary>
	[Test]
	public async Task ShowHotkeysEditorCommand_Writes_The_Saved_Hotkeys([Values] bool isSaved)
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		dto.Hotkeys.AddRange(HotkeyFactory.CreateHotkeyDtos(2));

		KeyStroke[] current = [.. dto.Hotkeys.ToKeyStrokes()];

		KeyStroke[] newHotkeys = [.. HotkeyFactory.CreateHotkeyDtos(1).ToKeyStrokes()];

		IHotkeysPrompt hotkeysPrompt = Substitute.For<IHotkeysPrompt>();

		IFileHotkeyEditor fileHotkeyEditor = Substitute.For<IFileHotkeyEditor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			hotkeysPrompt
				.EditHotkeysAsync(Arg.Any<IEnumerable<KeyStroke>>())
				.Returns(new EditingHotkeysResult(isSaved, newHotkeys));

			builder.RegisterInstance(hotkeysPrompt);

			builder.RegisterInstance(fileHotkeyEditor);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<HotkeysEditingScenario>()
				.As<IHotkeysEditingScenario>();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// Act
		await sut
			.ShowHotkeysEditorCommand
			.ExecuteAsync(dto);

		// Assert
		await hotkeysPrompt
			.Received(1)
			.EditHotkeysAsync(Arg.Is<IEnumerable<KeyStroke>>(x => x.SequenceEqual(current)));

		await fileHotkeyEditor.Received(isSaved ? 1 : 0).OverwriteAsync(
			dto,
			newHotkeys,
			sut.Hierarchy,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowObject" />: the object is selected only once the window has loaded, as
	/// a selection made before would not be saved.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowObject_Selects_The_Object_Once_The_Window_Has_Loaded()
	{
		// Arrange
		List<bool> isLoadedAtSelection = [];

		FileDto shown = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<WorkSession>()
			.AsImplementedInterfaces()
			.SingleInstance());

		EditorViewModel sut = mock.Create<EditorViewModel>();

		sut.Hierarchy.Add(shown);

		EditorWindow window = new(sut);

		using IDisposable selections = sut
			.WhenPropertyChanged(x => x.SelectedNode, notifyOnInitialValue: false)
			.Subscribe(_ => isLoadedAtSelection.Add(window.IsLoaded));

		// The window lays out its tree at once and loads only when the dispatcher gets to it.
		window.Show();

		// Act
		sut.ShowObject(shown.Id);

		await WaitUntilAsync(() => sut.SelectedNode?.Model == shown);

		window.Close();

		// Assert
		isLoadedAtSelection
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowSettingsCommand" />: saved settings that turn the clipboard history on make it
	/// available in the window.
	/// </summary>
	[Test]
	public async Task ShowSettingsCommand_Enables_The_Clipboard_History_Turned_On()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// The dialog is not shown here, so the session it would hold is edited by hand.
		ISettingsDraft draft = mock.Create<ISettingsDraft>();

		draft.Begin().TrackClipboardHistory = true;

		draft.Save();

		// Act
		await sut
			.ShowSettingsCommand
			.ExecuteAsync(null);

		// Assert
		sut.IsClipboardHistoryEnabled
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowSettingsCommand" />: settings closed without saving roll the previewed theme back
	/// and write nothing.
	/// </summary>
	[Test]
	public async Task ShowSettingsCommand_Rolls_The_Theme_Back_When_Not_Saved()
	{
		// Arrange
		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		IAppThemeService themeService = Substitute.For<IAppThemeService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(themeService);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// The dialog is not shown here, so the session it would hold is started by hand.
		mock
			.Create<ISettingsDraft>()
			.Begin();

		// Act
		await sut
			.ShowSettingsCommand
			.ExecuteAsync(null);

		// Assert
		themeService
			.Received(1)
			.ApplyFromSettings();

		settingsStore
			.DidNotReceive()
			.Save();
	}

	/// <summary>
	/// <see cref="EditorViewModel.ShowSettingsCommand" />: saved settings are written to the store.
	/// </summary>
	[Test]
	public async Task ShowSettingsCommand_Writes_The_Saved_Settings()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings(trackHotkeys: true);

		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<BackgroundServices>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<SettingsSession>()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		EditorViewModel sut = mock.Create<EditorViewModel>();

		// The dialog is not shown here, so the session it would hold is edited by hand.
		ISettingsDraft draft = mock.Create<ISettingsDraft>();

		draft.Begin().TrackHotkeys = true;

		draft.Save();

		// Act
		await sut
			.ShowSettingsCommand
			.ExecuteAsync(null);

		// Assert
		settingsStore
			.Received(1)
			.Overwrite(settings);

		settingsStore
			.Received(1)
			.Save();
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
