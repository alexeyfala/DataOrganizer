using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Services.Encryption;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Repository.DTO;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(EncryptedContentWriter)}"" type")]
internal class EncryptedContentWriterTests
{
	#region Methods
	/// <summary>
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: returns FailedToSaveContentsInDb and restores the backup and erases the file on failure.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Cannot_Save_Contents_In_Database()
	{
		// Arrange
		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestUtils.CreateRandomFileName(10),
			Contents = [],
			EncryptedDek = null,
			Files = [],
			Folder = TestUtils.CreateFolderDto(),
			NewStatus = default,
			Notes = []
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose();

		EncryptedContentWriter sut = mock.Create<EncryptedContentWriter>(TypedParameter.From(dbAccess));

		// Act
		UpdateDatabaseResult result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseResult.FailedToSaveContentsInDb);

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());
	}


	/// <summary>
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: returns FailedToSaveFolderPropertiesInDb and restores the backup when the notes of the folders cannot be saved.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Cannot_Save_Folder_Notes_In_Database()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestUtils.CreateRandomFileName(10),
			Contents = [],
			EncryptedDek = null,
			Files = [],
			Folder = folder,
			NewStatus = default,
			Notes = [new NoteUpdate(folder.Id, EntityType.Folder, TestUtils.CreateRandomBytes(10))]
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFilePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>())
				.Returns(true);

			dbAccess
				.UpdateFolderPropertiesAsync(Arg.Any<Guid>(), Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);
		});

		EncryptedContentWriter sut = mock.Create<EncryptedContentWriter>();

		// Act
		UpdateDatabaseResult result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseResult.FailedToSaveFolderPropertiesInDb);

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());
	}


	/// <summary>
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: returns FailedToSaveFolderPropertiesInDb and restores the backup and erases the file on failure.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Cannot_Save_Folder_Properties_In_Database()
	{
		// Arrange
		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestUtils.CreateRandomFileName(10),
			Contents = [],
			EncryptedDek = null,
			Files = [],
			Folder = TestUtils.CreateFolderDto(),
			NewStatus = default,
			Notes = []
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFilePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);
		});

		EncryptedContentWriter sut = mock.Create<EncryptedContentWriter>();

		// Act
		UpdateDatabaseResult result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseResult.FailedToSaveFolderPropertiesInDb);

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(Arg.Any<string>());
	}


	/// <summary>
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: returns Done and applies the new status to the folder and all files.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Does_Work([Values] EncryptionStatus newStatus)
	{
		// Arrange
		EncryptionStatus randomStatus = TestUtils.GetRandomEnumValueExcept(newStatus);

		FolderModelDto folder = TestUtils.CreateFolderDto(encryptionStatus: randomStatus);

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5, encryptionStatus: randomStatus)];

		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestUtils.CreateRandomFileName(10),
			Contents = [],
			EncryptedDek = TestUtils.CreateRandomBytes(10),
			Files = files,
			Folder = folder,
			NewStatus = newStatus,
			Notes = []
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFilePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>())
				.Returns(true);

			dbAccess
				.UpdateFolderPropertiesAsync(Arg.Any<Guid>(), Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);
		});

		EncryptedContentWriter sut = mock.Create<EncryptedContentWriter>();

		// Act
		UpdateDatabaseResult result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseResult.Done);

		parameters.Folder.EncryptionStatus
			.Should()
			.Be(newStatus);

		parameters.Files.Select(x => x.EncryptionStatus)
			.Should()
			.OnlyContain(x => x == newStatus);
	}


	/// <summary>
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: the processed notes are persisted and applied to the objects.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Saves_Notes()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		FolderModelDto subfolder = TestUtils.CreateFolderDto();

		folder
			.Children
			.Add(subfolder);

		FileModelDto file = TestUtils.CreateFileDto();

		byte[] folderNote = TestUtils.CreateRandomBytes(10);

		byte[] subfolderNote = TestUtils.CreateRandomBytes(10);

		byte[] fileNote = TestUtils.CreateRandomBytes(10);

		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestUtils.CreateRandomFileName(10),
			Contents =
			[
				new ContentsIsValidPair
				{
					Contents = TestUtils.CreateRandomBytes(10),
					Id = file.Id,
					IsValid = true
				}
			],
			EncryptedDek = TestUtils.CreateRandomBytes(10),
			Files = [file],
			Folder = folder,
			NewStatus = EncryptionStatus.Encrypted,
			Notes =
			[
				new NoteUpdate(folder.Id, EntityType.Folder, folderNote),
				new NoteUpdate(subfolder.Id, EntityType.Folder, subfolderNote),
				new NoteUpdate(file.Id, EntityType.File, fileNote)
			]
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFilePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>())
				.Returns(true);

			dbAccess
				.UpdateFolderPropertiesAsync(Arg.Any<Guid>(), Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>())
				.Returns(true);

			dbAccess
				.UpdateFolderPropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FolderModel>>[]>>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);
		});

		EncryptedContentWriter sut = mock.Create<EncryptedContentWriter>();

		// Act
		UpdateDatabaseResult result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseResult.Done);

		await dbAccess
			.Received(1)
			.UpdateFolderPropertiesAsync(Arg.Is<IDictionary<Guid, Action<UpdateSettersBuilder<FolderModel>>[]>>(x =>
				x != null && x.ContainsKey(folder.Id) && x.ContainsKey(subfolder.Id) && !x.ContainsKey(file.Id)));

		folder.Note
			.Should()
			.BeSameAs(folderNote);

		subfolder.Note
			.Should()
			.BeSameAs(subfolderNote);

		file.Note
			.Should()
			.BeSameAs(fileNote);
	}
	#endregion
}
