using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Encryption;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Services.Encryption;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using Repository.Dto;
using Repository.Interfaces.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestSupport;

namespace DataOrganizer.UnitTests.Services.Encryption;

[TestFixture(Description = $@"Tests of ""{nameof(EncryptedContentWriter)}"" type")]
internal class EncryptedContentWriterTests
{
	#region Methods
	/// <summary>
	/// <see cref="EncryptedContentWriter.UpdateDatabaseAsync" />: returns SaveFailed and restores the backup when the conversion cannot be saved.
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
		UpdateDatabaseOutcome result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseOutcome.SaveFailed);

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

		FolderDto folder = TestData.CreateFolderDto(encryptionStatus: randomStatus);

		FileDto[] files = [.. TestData.CreateFilesDto(5, encryptionStatus: randomStatus)];

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
					Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]>>(),
					Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]>>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);
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
					Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]>>(),
					Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]>>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);
		});

		EncryptedContentWriter sut = mock.Create<EncryptedContentWriter>();

		// Act
		UpdateDatabaseOutcome result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseOutcome.ExceptionThrown);

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
		FolderDto folder = TestData.CreateFolderDto();

		FolderDto subfolder = TestData.CreateFolderDto();

		folder
			.Children
			.Add(subfolder);

		FileDto file = TestData.CreateFileDto();

		byte[] folderNote = TestData.CreateRandomBytes(10);

		byte[] subfolderNote = TestData.CreateRandomBytes(10);

		byte[] fileNote = TestData.CreateRandomBytes(10);

		UpdateDatabaseParameters parameters = new()
		{
			BackupFilePath = TestData.CreateRandomFileName(10),
			Contents =
			[
				new ValidatedContents
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
				new NoteUpdate(folder.Id, EntityKind.Folder, folderNote),
				new NoteUpdate(subfolder.Id, EntityKind.Folder, subfolderNote),
				new NoteUpdate(file.Id, EntityKind.File, fileNote)
			]
		};

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFileAndFolderPropertiesAsync(
					Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]>>(),
					Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]>>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);
		});

		EncryptedContentWriter sut = mock.Create<EncryptedContentWriter>();

		// Act
		UpdateDatabaseOutcome result = await sut.UpdateDatabaseAsync(parameters);

		// Assert
		result
			.Should()
			.Be(UpdateDatabaseOutcome.Saved);

		await dbAccess.Received(1).UpdateFileAndFolderPropertiesAsync(
			Arg.Is<IDictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]>>(x =>
				x != null && x.ContainsKey(file.Id)),
			Arg.Is<IDictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]>>(x =>
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
