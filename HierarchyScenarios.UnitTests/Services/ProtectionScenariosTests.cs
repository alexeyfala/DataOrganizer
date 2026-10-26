using AppInstance.Interfaces;
using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataProtection.Interfaces;
using EditorMessages.Messages;
using FileExecution.Interfaces;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.UnitTests.Factories;
using HierarchyScenarios.Services;
using NSubstitute;
using Shared.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HierarchyScenarios.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(ProtectionScenarios)}"" type")]
internal class ProtectionScenariosTests
{
	#region Methods
	/// <summary>
	/// <see cref="ProtectionScenarios.ChangePasswordAsync" />: the open files of the folder are closed, then the password
	/// is changed.
	/// </summary>
	[Test]
	public async Task ChangePasswordAsync_Changes_The_Password_Once_The_Open_Files_Close()
	{
		// Arrange
		FileDto openFile = ItemDtoFactory.CreateFileDto(isEditing: true);

		FolderDto folder = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		folder
			.Children
			.AddRange([openFile, ItemDtoFactory.CreateFileDto()]);

		IFolderProtection folderProtection = Substitute.For<IFolderProtection>();

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(folderProtection);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		await sut.ChangePasswordAsync(folder);

		// Assert
		await openFilesCloser.Received(1).TryCloseAsync(
			Arg.Is<IReadOnlyCollection<FileDto>>(x => x.Count == 1 && x.Contains(openFile)),
			Arg.Any<CancellationToken>());

		await folderProtection
			.Received(1)
			.ChangePasswordAsync(folder, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.ChangePasswordAsync" />: a file that stays open keeps the password.
	/// </summary>
	[Test]
	public async Task ChangePasswordAsync_Keeps_The_Password_When_A_File_Stays_Open()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		folder
			.Children
			.Add(ItemDtoFactory.CreateFileDto(isEditing: true));

		IFolderProtection folderProtection = Substitute.For<IFolderProtection>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			// The file stays open
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(folderProtection);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		await sut.ChangePasswordAsync(folder);

		// Assert
		await folderProtection
			.DidNotReceive()
			.ChangePasswordAsync(Arg.Any<FolderDto>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.DecryptFolderAsync" />: the open files of the folder are closed, then every file
	/// of the folder is decrypted.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Decrypts_The_Files_Once_The_Open_Files_Close()
	{
		// Arrange
		FileDto openFile = ItemDtoFactory.CreateFileDto(isExecuting: true);

		FileDto closedFile = ItemDtoFactory.CreateFileDto();

		FolderDto folder = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		folder
			.Children
			.AddRange([openFile, closedFile]);

		FileDto[] expected = [openFile, closedFile];

		IFolderProtection folderProtection = Substitute.For<IFolderProtection>();

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(folderProtection);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		await sut.DecryptFolderAsync(folder);

		// Assert
		await openFilesCloser.Received(1).TryCloseAsync(
			Arg.Is<IReadOnlyCollection<FileDto>>(x => x.Count == 1 && x.Contains(openFile)),
			Arg.Any<CancellationToken>());

		await folderProtection.Received(1).DecryptFolderAsync(
			folder,
			Arg.Is<FileDto[]>(x => x.ToHashSet().SetEquals(expected)),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.DecryptFolderAsync" />: a file that stays open keeps the folder encrypted.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Keeps_The_Folder_When_A_File_Stays_Open()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		folder
			.Children
			.Add(ItemDtoFactory.CreateFileDto(isEditing: true));

		IFolderProtection folderProtection = Substitute.For<IFolderProtection>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			// The file stays open
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(folderProtection);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		await sut.DecryptFolderAsync(folder);

		// Assert
		await folderProtection.DidNotReceive().DecryptFolderAsync(
			Arg.Any<FolderDto>(),
			Arg.Any<FileDto[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.DecryptFolderAsync" />: a folder without files is left as it is.
	/// </summary>
	[Test]
	public async Task DecryptFolderAsync_Keeps_The_Folder_Without_Files()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		IFolderProtection folderProtection = Substitute.For<IFolderProtection>();

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(folderProtection);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		await sut.DecryptFolderAsync(folder);

		// Assert
		await openFilesCloser.DidNotReceive().TryCloseAsync(
			Arg.Any<IReadOnlyCollection<FileDto>>(),
			Arg.Any<CancellationToken>());

		await folderProtection.DidNotReceive().DecryptFolderAsync(
			Arg.Any<FolderDto>(),
			Arg.Any<FileDto[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.EncryptFolderAsync" />: the open files of the folder are closed, then every file
	/// of the folder is encrypted.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Encrypts_The_Files_Once_The_Open_Files_Close()
	{
		// Arrange
		FileDto openFile = ItemDtoFactory.CreateFileDto(isEditing: true);

		FileDto closedFile = ItemDtoFactory.CreateFileDto();

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder
			.Children
			.AddRange([openFile, closedFile]);

		FileDto[] expected = [openFile, closedFile];

		IFolderProtection folderProtection = Substitute.For<IFolderProtection>();

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(folderProtection);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		await sut.EncryptFolderAsync(folder);

		// Assert
		await openFilesCloser.Received(1).TryCloseAsync(
			Arg.Is<IReadOnlyCollection<FileDto>>(x => x.Count == 1 && x.Contains(openFile)),
			Arg.Any<CancellationToken>());

		await folderProtection.Received(1).EncryptFolderAsync(
			folder,
			Arg.Is<FileDto[]>(x => x.ToHashSet().SetEquals(expected)),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.EncryptFolderAsync" />: a file that stays open keeps the folder plain.
	/// </summary>
	[Test]
	public async Task EncryptFolderAsync_Keeps_The_Folder_When_A_File_Stays_Open()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder
			.Children
			.Add(ItemDtoFactory.CreateFileDto(isEditing: true));

		IFolderProtection folderProtection = Substitute.For<IFolderProtection>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			// The file stays open
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(folderProtection);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		await sut.EncryptFolderAsync(folder);

		// Assert
		await folderProtection.DidNotReceive().EncryptFolderAsync(
			Arg.Any<FolderDto>(),
			Arg.Any<FileDto[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.HideAllContentsAsync" />: of the open files only the decrypted ones are closed.
	/// </summary>
	[Test]
	public async Task HideAllContentsAsync_Closes_Only_The_Open_Decrypted_Files()
	{
		// Arrange
		FileDto openDecrypted = ItemDtoFactory.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ObservableCollection<ExplorerItemDtoBase> hierarchy =
			[
				openDecrypted,
				ItemDtoFactory.CreateFileDto(isEditing: true),
				ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)
			];

			ISessionHierarchy sessionHierarchy = Substitute.For<ISessionHierarchy>();

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			sessionHierarchy
				.Hierarchy
				.Returns(hierarchy);

			builder.RegisterInstance(openFilesCloser);

			builder.RegisterInstance(sessionHierarchy);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		await sut.HideAllContentsAsync();

		// Assert
		await openFilesCloser.Received(1).TryCloseAsync(
			Arg.Is<IReadOnlyCollection<FileDto>>(x => x.Count == 1 && x.Contains(openDecrypted)),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.HideAllContentsAsync" />: every contents of the tree is hidden and the auto-lock
	/// follows.
	/// </summary>
	[Test]
	public async Task HideAllContentsAsync_Hides_Every_Contents_And_Refreshes_The_Auto_Lock()
	{
		// Arrange
		ObservableCollection<ExplorerItemDtoBase> hierarchy =
		[
			ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)
		];

		IAutoLockControl autoLockControl = Substitute.For<IAutoLockControl>();

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			ISessionHierarchy sessionHierarchy = Substitute.For<ISessionHierarchy>();

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			sessionHierarchy
				.Hierarchy
				.Returns(hierarchy);

			builder.RegisterInstance(autoLockControl);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(openFilesCloser);

			builder.RegisterInstance(sessionHierarchy);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		bool result = await sut.HideAllContentsAsync();

		// Assert
		result
			.Should()
			.BeTrue();

		contentVisibility
			.Received(1)
			.HideAllContents(hierarchy);

		autoLockControl
			.Received()
			.RefreshAutoLock();
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.HideAllContentsAsync" />: a file that stays open keeps the contents shown.
	/// </summary>
	[Test]
	public async Task HideAllContentsAsync_Keeps_The_Contents_When_A_File_Stays_Open()
	{
		// Arrange
		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ObservableCollection<ExplorerItemDtoBase> hierarchy =
			[
				ItemDtoFactory.CreateFileDto(
					isEditing: true,
					encryptionStatus: EncryptionStatus.Decrypted)
			];

			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			ISessionHierarchy sessionHierarchy = Substitute.For<ISessionHierarchy>();

			// The file stays open
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(false);

			sessionHierarchy
				.Hierarchy
				.Returns(hierarchy);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(openFilesCloser);

			builder.RegisterInstance(sessionHierarchy);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		bool result = await sut.HideAllContentsAsync();

		// Assert
		result
			.Should()
			.BeFalse();

		contentVisibility
			.DidNotReceive()
			.HideAllContents(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.HideAllContentsAsync" />: an editor that cannot persist its changes keeps the
	/// contents shown and the files open.
	/// </summary>
	[Test]
	public async Task HideAllContentsAsync_Keeps_The_Contents_When_An_Editor_Cannot_Save()
	{
		// Arrange
		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMessenger messenger = new StrongReferenceMessenger();

			// An open editor fails to persist its changes
			messenger.Register<FlushEditorsMessage>(new object(), static (_, message) => message.Reply(false));

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		bool result = await sut.HideAllContentsAsync();

		// Assert
		result
			.Should()
			.BeFalse();

		await openFilesCloser.DidNotReceive().TryCloseAsync(
			Arg.Any<IReadOnlyCollection<FileDto>>(),
			Arg.Any<CancellationToken>());

		contentVisibility
			.DidNotReceive()
			.HideAllContents(Arg.Any<IEnumerable<ExplorerItemDtoBase>>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.HideFileContentsAsync" />: the file is closed only when it is open.
	/// </summary>
	[Test]
	public async Task HideFileContentsAsync_Closes_The_File_Only_When_It_Is_Open([Values] bool isEditing)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(
			isEditing: isEditing,
			encryptionStatus: EncryptionStatus.Decrypted);

		FileDto[] expected = isEditing ? [file] : [];

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		await sut.HideFileContentsAsync(file);

		// Assert
		await openFilesCloser.Received(1).TryCloseAsync(
			Arg.Is<IReadOnlyCollection<FileDto>>(x => x.SequenceEqual(expected)),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.HideFileContentsAsync" />: the contents of the file are hidden and the auto-lock
	/// follows.
	/// </summary>
	[Test]
	public async Task HideFileContentsAsync_Hides_The_Contents_And_Refreshes_The_Auto_Lock()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		IAutoLockControl autoLockControl = Substitute.For<IAutoLockControl>();

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(autoLockControl);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		bool result = await sut.HideFileContentsAsync(file);

		// Assert
		result
			.Should()
			.BeTrue();

		contentVisibility
			.Received(1)
			.HideFileContents(file);

		autoLockControl
			.Received()
			.RefreshAutoLock();
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.HideFileContentsAsync" />: an editor that cannot persist its changes keeps the
	/// contents shown and the file open.
	/// </summary>
	[Test]
	public async Task HideFileContentsAsync_Keeps_The_Contents_When_An_Editor_Cannot_Save()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMessenger messenger = new StrongReferenceMessenger();

			// An open editor fails to persist its changes
			messenger.Register<FlushEditorsMessage>(new object(), static (_, message) => message.Reply(false));

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		bool result = await sut.HideFileContentsAsync(file);

		// Assert
		result
			.Should()
			.BeFalse();

		await openFilesCloser.DidNotReceive().TryCloseAsync(
			Arg.Any<IReadOnlyCollection<FileDto>>(),
			Arg.Any<CancellationToken>());

		contentVisibility
			.DidNotReceive()
			.HideFileContents(Arg.Any<FileDto>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.HideFileContentsAsync" />: a file that stays open keeps its contents shown.
	/// </summary>
	[Test]
	public async Task HideFileContentsAsync_Keeps_The_Contents_When_The_File_Stays_Open()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			// The file stays open
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		bool result = await sut.HideFileContentsAsync(file);

		// Assert
		result
			.Should()
			.BeFalse();

		contentVisibility
			.DidNotReceive()
			.HideFileContents(Arg.Any<FileDto>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.HideFolderContentsAsync" />: the open files of the folder are closed, then its
	/// contents are hidden and the auto-lock follows.
	/// </summary>
	[Test]
	public async Task HideFolderContentsAsync_Hides_The_Contents_Once_The_Open_Files_Close()
	{
		// Arrange
		FileDto openFile = ItemDtoFactory.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		FolderDto folder = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		folder
			.Children
			.AddRange([openFile, ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)]);

		IAutoLockControl autoLockControl = Substitute.For<IAutoLockControl>();

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(autoLockControl);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		bool result = await sut.HideFolderContentsAsync(folder);

		// Assert
		result
			.Should()
			.BeTrue();

		await openFilesCloser.Received(1).TryCloseAsync(
			Arg.Is<IReadOnlyCollection<FileDto>>(x => x.Count == 1 && x.Contains(openFile)),
			Arg.Any<CancellationToken>());

		contentVisibility
			.Received(1)
			.HideFolderContents(folder);

		autoLockControl
			.Received()
			.RefreshAutoLock();
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.HideFolderContentsAsync" />: a file that stays open keeps the contents shown.
	/// </summary>
	[Test]
	public async Task HideFolderContentsAsync_Keeps_The_Contents_When_A_File_Stays_Open()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		folder
			.Children
			.Add(ItemDtoFactory.CreateFileDto(
				isEditing: true,
				encryptionStatus: EncryptionStatus.Decrypted));

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			// The file stays open
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		bool result = await sut.HideFolderContentsAsync(folder);

		// Assert
		result
			.Should()
			.BeFalse();

		contentVisibility
			.DidNotReceive()
			.HideFolderContents(Arg.Any<FolderDto>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.HideFolderContentsAsync" />: an editor that cannot persist its changes keeps the
	/// contents shown and the files open.
	/// </summary>
	[Test]
	public async Task HideFolderContentsAsync_Keeps_The_Contents_When_An_Editor_Cannot_Save()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		folder
			.Children
			.Add(ItemDtoFactory.CreateFileDto(
				isEditing: true,
				encryptionStatus: EncryptionStatus.Decrypted));

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMessenger messenger = new StrongReferenceMessenger();

			// An open editor fails to persist its changes
			messenger.Register<FlushEditorsMessage>(new object(), static (_, message) => message.Reply(false));

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		bool result = await sut.HideFolderContentsAsync(folder);

		// Assert
		result
			.Should()
			.BeFalse();

		await openFilesCloser.DidNotReceive().TryCloseAsync(
			Arg.Any<IReadOnlyCollection<FileDto>>(),
			Arg.Any<CancellationToken>());

		contentVisibility
			.DidNotReceive()
			.HideFolderContents(Arg.Any<FolderDto>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.LockAsync" />: every open file is closed without a question, decrypted or not.
	/// </summary>
	[Test]
	public async Task LockAsync_Closes_Every_Open_File()
	{
		// Arrange
		FileDto openDecrypted = ItemDtoFactory.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		FileDto openPlain = ItemDtoFactory.CreateFileDto(isExecuting: true);

		FileDto[] expected = [openDecrypted, openPlain];

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ObservableCollection<ExplorerItemDtoBase> hierarchy =
			[
				openDecrypted,
				openPlain,
				ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted)
			];

			ISessionHierarchy sessionHierarchy = Substitute.For<ISessionHierarchy>();

			sessionHierarchy
				.Hierarchy
				.Returns(hierarchy);

			builder.RegisterInstance(openFilesCloser);

			builder.RegisterInstance(sessionHierarchy);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		await sut.LockAsync();

		// Assert
		openFilesCloser
			.Received(1)
			.Close(Arg.Is<IReadOnlyCollection<FileDto>>(x => x.ToHashSet().SetEquals(expected)));
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.LockAsync" />: the editor tabs that wait in the work session are dropped with the
	/// marks of their files.
	/// </summary>
	[Test]
	public async Task LockAsync_Drops_The_Editor_Tabs_And_Unmarks_Their_Files()
	{
		// Arrange
		FileDto tabFile = ItemDtoFactory.CreateFileDto(
			isEditing: true,
			encryptionStatus: EncryptionStatus.Decrypted);

		ISessionEditorTabs sessionEditorTabs = Substitute.For<ISessionEditorTabs>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ISessionHierarchy sessionHierarchy = Substitute.For<ISessionHierarchy>();

			sessionEditorTabs.EditorTabs = new()
			{
				Files = [tabFile],
				PreviousFile = null,
				SelectedFile = tabFile
			};

			sessionHierarchy
				.Hierarchy
				.Returns([]);

			builder.RegisterInstance(sessionEditorTabs);

			builder.RegisterInstance(sessionHierarchy);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		await sut.LockAsync();

		// Assert
		tabFile.IsEditing
			.Should()
			.BeFalse();

		sessionEditorTabs.EditorTabs
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.LockAsync" />: an editor that cannot persist its changes does not keep the
	/// contents shown, and the auto-lock follows.
	/// </summary>
	[Test]
	public async Task LockAsync_Hides_Every_Contents_Even_When_An_Editor_Cannot_Save()
	{
		// Arrange
		ObservableCollection<ExplorerItemDtoBase> hierarchy =
		[
			ItemDtoFactory.CreateFileDto(
				isEditing: true,
				encryptionStatus: EncryptionStatus.Decrypted)
		];

		IAutoLockControl autoLockControl = Substitute.For<IAutoLockControl>();

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMessenger messenger = new StrongReferenceMessenger();

			ISessionHierarchy sessionHierarchy = Substitute.For<ISessionHierarchy>();

			// An open editor fails to persist its changes
			messenger.Register<FlushEditorsMessage>(new object(), static (_, message) => message.Reply(false));

			sessionHierarchy
				.Hierarchy
				.Returns(hierarchy);

			builder.RegisterInstance(autoLockControl);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(messenger);

			builder.RegisterInstance(sessionHierarchy);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		await sut.LockAsync();

		// Assert
		contentVisibility
			.Received(1)
			.HideAllContents(hierarchy);

		autoLockControl
			.Received()
			.RefreshAutoLock();
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.ShowFileContentsAsync" />: the auto-lock follows only contents that are shown.
	/// </summary>
	[Test]
	public async Task ShowFileContentsAsync_Refreshes_The_Auto_Lock_Only_When_The_Contents_Show([Values] bool isShown)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		IAutoLockControl autoLockControl = Substitute.For<IAutoLockControl>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

			contentVisibility
				.ShowFileContentsAsync(file, Arg.Any<CancellationToken>())
				.Returns(isShown);

			builder.RegisterInstance(autoLockControl);

			builder.RegisterInstance(contentVisibility);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		bool result = await sut.ShowFileContentsAsync(file);

		// Assert
		result
			.Should()
			.Be(isShown);

		autoLockControl
			.Received(isShown ? 1 : 0)
			.RefreshAutoLock();
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.ShowFolderContentsAsync" />: a file that stays open keeps the contents hidden.
	/// </summary>
	[Test]
	public async Task ShowFolderContentsAsync_Keeps_The_Contents_Hidden_When_A_File_Stays_Open()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		folder
			.Children
			.Add(ItemDtoFactory.CreateFileDto(isExecuting: true));

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

			// The file stays open
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		bool result = await sut.ShowFolderContentsAsync(folder);

		// Assert
		result
			.Should()
			.BeFalse();

		await contentVisibility
			.DidNotReceive()
			.ShowFolderContentsAsync(Arg.Any<FolderDto>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ProtectionScenarios.ShowFolderContentsAsync" />: the open files of the folder are closed, then its
	/// contents are shown and the auto-lock follows.
	/// </summary>
	[Test]
	public async Task ShowFolderContentsAsync_Shows_The_Contents_Once_The_Open_Files_Close()
	{
		// Arrange
		FileDto openFile = ItemDtoFactory.CreateFileDto(isExecuting: true);

		FolderDto folder = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		folder
			.Children
			.AddRange([openFile, ItemDtoFactory.CreateFileDto()]);

		IAutoLockControl autoLockControl = Substitute.For<IAutoLockControl>();

		IContentVisibility contentVisibility = Substitute.For<IContentVisibility>();

		IOpenFilesCloser openFilesCloser = Substitute.For<IOpenFilesCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			openFilesCloser
				.TryCloseAsync(Arg.Any<IReadOnlyCollection<FileDto>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(autoLockControl);

			builder.RegisterInstance(contentVisibility);

			builder.RegisterInstance(openFilesCloser);
		});

		ProtectionScenarios sut = mock.Create<ProtectionScenarios>();

		// Act
		bool result = await sut.ShowFolderContentsAsync(folder);

		// Assert
		result
			.Should()
			.BeTrue();

		await openFilesCloser.Received(1).TryCloseAsync(
			Arg.Is<IReadOnlyCollection<FileDto>>(x => x.Count == 1 && x.Contains(openFile)),
			Arg.Any<CancellationToken>());

		await contentVisibility
			.Received(1)
			.ShowFolderContentsAsync(folder, Arg.Any<CancellationToken>());

		autoLockControl
			.Received()
			.RefreshAutoLock();
	}
	#endregion
}
