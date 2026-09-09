using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Dto.Encryption;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Services.Encryption;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using Repository.Dto;
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
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: returns FailedToSaveInDb and restores the backup when the conversion cannot be saved.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Cannot_Save_In_Database()
	{
		// Arrange
		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestData.CreateRandomFileName(10),
			Contents = [],
			EncryptedDek = null,
			Files = [],
			Folder = TestData.CreateFolderDto(),
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
			.Be(UpdateDatabaseResult.FailedToSaveInDb);

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
		EncryptionStatus randomStatus = TestData.GetRandomEnumValueExcept(newStatus);

		FolderModelDto folder = TestData.CreateFolderDto(encryptionStatus: randomStatus);

		FileModelDto[] files = [.. TestData.CreateFilesDto(5, encryptionStatus: randomStatus)];

		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestData.CreateRandomFileName(10),
			Contents = [],
			EncryptedDek = TestData.CreateRandomBytes(10),
			Files = files,
			Folder = folder,
			NewStatus = newStatus,
			Notes = []
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFileAndFolderPropertiesAsync(
					Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>(),
					Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FolderModel>>[]>>())
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
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: returns ExceptionThrown and restores the backup when the write throws.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Restores_Backup_When_The_Write_Throws()
	{
		// Arrange
		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestData.CreateRandomFileName(10),
			Contents = [],
			EncryptedDek = TestData.CreateRandomBytes(10),
			Files = [],
			Folder = TestData.CreateFolderDto(),
			NewStatus = default,
			Notes = []
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFileAndFolderPropertiesAsync(
					Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>(),
					Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FolderModel>>[]>>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);
		});

		EncryptedContentWriter sut = mock.Create<EncryptedContentWriter>();

		// Act
		UpdateDatabaseResult result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseResult.ExceptionThrown);

		await dbAccess
			.Received()
			.RestoreFromBackupAsync(parameters.BackupFilePath);
	}


	/// <summary>
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: the processed notes are persisted and applied to the objects.
	/// </summary>
	[Test]
	public async Task UpdateDatabaseAsync_Saves_Notes()
	{
		// Arrange
		FolderModelDto folder = TestData.CreateFolderDto();

		FolderModelDto subfolder = TestData.CreateFolderDto();

		folder
			.Children
			.Add(subfolder);

		FileModelDto file = TestData.CreateFileDto();

		byte[] folderNote = TestData.CreateRandomBytes(10);

		byte[] subfolderNote = TestData.CreateRandomBytes(10);

		byte[] fileNote = TestData.CreateRandomBytes(10);

		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestData.CreateRandomFileName(10),
			Contents =
			[
				new ContentsIsValidPair
				{
					Contents = TestData.CreateRandomBytes(10),
					Id = file.Id,
					IsValid = true
				}
			],
			EncryptedDek = TestData.CreateRandomBytes(10),
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
				.UpdateFileAndFolderPropertiesAsync(
					Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>(),
					Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FolderModel>>[]>>())
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

		await dbAccess.Received(1).UpdateFileAndFolderPropertiesAsync(
			Arg.Is<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>(x =>
				x != null && x.ContainsKey(file.Id)),
			Arg.Is<IDictionary<Guid, Action<UpdateSettersBuilder<FolderModel>>[]>>(x =>
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
