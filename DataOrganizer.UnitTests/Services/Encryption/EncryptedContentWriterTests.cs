using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Encryption;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Services.Encryption;
using Entities.Enums;
using Entities.Models;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.UnitTests.Factories;
using Microsoft.Extensions.Time.Testing;
using Notifications.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using Repository.Dto;
using Repository.Enums;
using Repository.Exceptions;
using Repository.Interfaces;
using Repository.Interfaces.Database;
using Repository.Services;
using Repository.Services.Database;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Database;
using TestSupport.Models;

namespace DataOrganizer.UnitTests.Services.Encryption;

[TestFixture(Description = $@"Tests of ""{nameof(EncryptedContentWriter)}"" type")]
internal class EncryptedContentWriterTests
{
	#region Methods
	/// <summary>
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: a write that throws answers ExceptionThrown and shows an error.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Answers_ExceptionThrown_When_The_Write_Throws()
	{
		// Arrange
		UpdateDatabaseParameters parameters = new()
		{
			Contents = [],
			EncryptedDek = RandomValues.CreateBytes(10),
			Files = [],
			Folder = ItemDtoFactory.CreateFolderDto(),
			NewStatus = default,
			Notes = []
		};

		INotificationService notification = Substitute.For<INotificationService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateProtectionAsync(Arg.Any<ProtectionUpdate>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(notification);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		EncryptedContentWriter sut = mock.Create<EncryptedContentWriter>();

		// Act
		UpdateDatabaseOutcome result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseOutcome.ExceptionThrown);

		notification
			.Received(1)
			.ShowErrorSnackbar(Arg.Any<string>(), Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: returns Done and applies the new status to the folder and all files.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Applies_The_New_Status_To_Folder_And_Files([Values] EncryptionStatus newStatus)
	{
		// Arrange
		using TestDatabase database = new();

		EncryptionStatus randomStatus = RandomValues.CreateEnumValueExcept(newStatus);

		FolderDto folder = ItemDtoFactory.CreateFolderDto(encryptionStatus: randomStatus);

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(5, encryptionStatus: randomStatus)];

		UpdateDatabaseParameters parameters = new()
		{
			Contents = [],
			EncryptedDek = RandomValues.CreateBytes(10),
			Files = files,
			Folder = folder,
			NewStatus = newStatus,
			Notes = []
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();
		});

		EncryptedContentWriter sut = mock.Create<EncryptedContentWriter>();

		// Act
		UpdateDatabaseOutcome result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseOutcome.Saved);

		parameters.Folder.EncryptionStatus
			.Should()
			.Be(newStatus);

		parameters.Files.Select(x => x.EncryptionStatus)
			.Should()
			.OnlyContain(x => x == newStatus);
	}

	/// <summary>
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: a write the database turns down answers SaveFailed and shows
	/// an error.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Cannot_Save_In_Database()
	{
		// Arrange
		UpdateDatabaseParameters parameters = new()
		{
			Contents = [],
			EncryptedDek = null,
			Files = [],
			Folder = ItemDtoFactory.CreateFolderDto(),
			NewStatus = default,
			Notes = []
		};

		INotificationService notification = Substitute.For<INotificationService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateProtectionAsync(Arg.Any<ProtectionUpdate>())
				.ThrowsAsync(new DatabaseNotWritableException(DbConnectionStatus.FileUnreadable, nameof(IDbAccess)));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(notification);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		EncryptedContentWriter sut = mock.Create<EncryptedContentWriter>();

		// Act
		UpdateDatabaseOutcome result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseOutcome.SaveFailed);

		notification
			.Received(1)
			.ShowErrorSnackbar(Arg.Any<string>(), Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: the processed notes are persisted and applied to the objects.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Saves_Notes()
	{
		// Arrange
		using TestDatabase database = new();

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FolderDto subfolder = ItemDtoFactory.CreateFolderDto();

		folder
			.Children
			.Add(subfolder);

		FileDto file = ItemDtoFactory.CreateFileDto();

		byte[] folderNote = RandomValues.CreateBytes(10);

		byte[] subfolderNote = RandomValues.CreateBytes(10);

		byte[] fileNote = RandomValues.CreateBytes(10);

		UpdateDatabaseParameters parameters = new()
		{
			Contents =
			[
				new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(10),
					Id = file.Id,
					IsValid = true
				}
			],
			EncryptedDek = RandomValues.CreateBytes(10),
			Files = [file],
			Folder = folder,
			NewStatus = EncryptionStatus.Encrypted,
			Notes =
			[
				new NoteUpdate(folder.Id, EntityKind.Folder, folderNote),
				new NoteUpdate(subfolder.Id, EntityKind.Folder, subfolderNote),
				new NoteUpdate(file.Id, EntityKind.File, fileNote)
			]
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFoldersAsync(
		[
			EntityFactory.CreateFolder(id: folder.Id),
			EntityFactory.CreateFolder(id: subfolder.Id, parentId: folder.Id)
		]);

		await dbAccess.AddFilesAsync([EntityFactory.CreateFile(id: file.Id, parentId: folder.Id)]);

		EncryptedContentWriter sut = mock.Create<EncryptedContentWriter>();

		// Act
		UpdateDatabaseOutcome result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseOutcome.Saved);

		folder.Note
			.Should()
			.BeSameAs(folderNote);

		subfolder.Note
			.Should()
			.BeSameAs(subfolderNote);

		file.Note
			.Should()
			.BeSameAs(fileNote);

		FolderEntity[] folders = await dbAccess.GetAllFoldersAsync();

		FileEntity[] files = await dbAccess.GetAllFilesAsync(OptionalFileProperties.None);

		ExplorerItemBase[] stored = [.. folders, .. files];

		stored.ToDictionary(static x => x.Id, static x => x.Note)
			.Should()
			.BeEquivalentTo(new Dictionary<Guid, byte[]?>
			{
				[file.Id] = fileNote,
				[folder.Id] = folderNote,
				[subfolder.Id] = subfolderNote
			});
	}

	/// <summary>
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: the new contents, the notes and the wrapped key reach the
	/// database, and what changes gets the date of the write. A file note goes in only with the contents of its file.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Writes_The_Contents_The_Key_And_The_Dates()
	{
		// Arrange
		using TestDatabase database = new();

		// The database keeps dates to the second.
		DateTime before = new(2020, 1, 2, 3, 4, 5);

		byte[] oldContents = RandomValues.CreateBytes(10);

		byte[] oldNote = RandomValues.CreateBytes(10);

		FolderEntity keeper = EntityFactory.CreateFolder(encryptedDek: RandomValues.CreateBytes(10), note: oldNote);

		FolderEntity subfolder = EntityFactory.CreateFolder(parentId: keeper.Id, note: oldNote);

		FileEntity noted = EntityFactory.CreateFile(contents: oldContents, parentId: keeper.Id, note: oldNote);

		FileEntity unnoted = EntityFactory.CreateFile(contents: oldContents, parentId: subfolder.Id, note: oldNote);

		FileEntity untouched = EntityFactory.CreateFile(contents: oldContents, parentId: subfolder.Id, note: oldNote);

		ExplorerItemBase[] entities = [keeper, subfolder, noted, unnoted, untouched];

		entities.ForEach(x => x.UpdatedAt = before);

		FolderDto keeperDto = ItemDtoFactory.CreateFolderDto(id: keeper.Id);

		keeperDto
			.Children
			.Add(ItemDtoFactory.CreateFolderDto(id: subfolder.Id));

		byte[] encryptedDek = RandomValues.CreateBytes(10);

		byte[] notedContents = RandomValues.CreateBytes(10);

		byte[] unnotedContents = RandomValues.CreateBytes(10);

		byte[] subfolderNote = RandomValues.CreateBytes(10);

		byte[] notedNote = RandomValues.CreateBytes(10);

		UpdateDatabaseParameters parameters = new()
		{
			Contents =
			[
				new ValidatedContents
				{
					Contents = notedContents,
					Id = noted.Id,
					IsValid = true
				},
				new ValidatedContents
				{
					Contents = unnotedContents,
					Id = unnoted.Id,
					IsValid = true
				}
			],
			EncryptedDek = encryptedDek,
			Files =
			[
				ItemDtoFactory.CreateFileDto(id: noted.Id),
				ItemDtoFactory.CreateFileDto(id: unnoted.Id),
				ItemDtoFactory.CreateFileDto(id: untouched.Id)
			],
			Folder = keeperDto,
			NewStatus = EncryptionStatus.Encrypted,
			Notes =
			[
				new NoteUpdate(subfolder.Id, EntityKind.Folder, subfolderNote),
				new NoteUpdate(noted.Id, EntityKind.File, notedNote),
				new NoteUpdate(untouched.Id, EntityKind.File, RandomValues.CreateBytes(10))
			]
		};

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFoldersAsync([keeper, subfolder]);

		await dbAccess.AddFilesAsync([noted, unnoted, untouched]);

		EncryptedContentWriter sut = mock.Create<EncryptedContentWriter>();

		// Act
		UpdateDatabaseOutcome result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseOutcome.Saved);

		DateTime now = time
			.GetLocalNow()
			.LocalDateTime;

		FolderEntity[] folders = await dbAccess.GetAllFoldersAsync();

		folders
			.Should()
			.BeEquivalentTo(
			[
				new
				{
					EncryptedDek = (byte[]?)encryptedDek,
					keeper.Id,
					Note = oldNote,
					UpdatedAt = before
				},
				new
				{
					EncryptedDek = (byte[]?)null,
					subfolder.Id,
					Note = subfolderNote,
					UpdatedAt = now
				}
			]);

		FileEntity[] files = await dbAccess.GetAllFilesAsync(OptionalFileProperties.Contents);

		files
			.Should()
			.BeEquivalentTo(
			[
				new
				{
					Contents = notedContents,
					noted.Id,
					Note = notedNote,
					UpdatedAt = now
				},
				new
				{
					Contents = unnotedContents,
					unnoted.Id,
					Note = oldNote,
					UpdatedAt = now
				},
				new
				{
					Contents = oldContents,
					untouched.Id,
					Note = oldNote,
					UpdatedAt = before
				}
			]);
	}
	#endregion
}
