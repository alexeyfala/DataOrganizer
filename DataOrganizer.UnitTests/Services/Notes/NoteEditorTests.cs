using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Services.Notes;
using Diagnostics.Interfaces;
using Entities.Enums;
using Entities.Models;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Enums;
using Repository.Interfaces;
using Repository.Interfaces.Database;
using Repository.Services;
using Repository.Services.Database;
using Shared.Common;
using System;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Database;
using TestSupport.Models;

namespace DataOrganizer.UnitTests.Services.Notes;

[TestFixture(Description = $@"Tests of ""{nameof(NoteEditor)}"" type")]
internal class NoteEditorTests
{
	#region Methods
	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: blank text removes the note, in the database too.
	/// </summary>
	[Test]
	public async Task EditAsync_Deletes_Note_When_Text_Is_Blank([Values(null, "", "   ")] string? note)
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity entity = EntityFactory.CreateFile(note: RandomValues.CreateBytes(10));

		FileDto file = ItemDtoFactory.CreateFileDto(id: entity.Id);

		file.Note = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Encode(file, Arg.Any<string>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(database.Context);

			builder.RegisterInstance(noteCipher);

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
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFilesAsync([entity]);

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		bool result = await sut.EditAsync(
			file,
			note);

		// Assert
		result
			.Should()
			.BeTrue();

		file.Note
			.Should()
			.BeNull();

		FileEntity[] stored = await dbAccess.GetAllFilesAsync(OptionalFileProperties.None);

		stored
			.Should()
			.ContainSingle()
			.Which
			.Note
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: a write the database fails goes to the reporter of database failures, and the
	/// note is not applied.
	/// </summary>
	[Test]
	public async Task EditAsync_Hands_A_Failed_Write_To_The_Failure_Reporter()
	{
		// Arrange
		InvalidOperationException failure = new();

		FileDto file = ItemDtoFactory.CreateFileDto();

		IDbFailureReporter failureReporter = Substitute.For<IDbFailureReporter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] encoded = RandomValues.CreateBytes(10);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Encode(file, Arg.Any<string>())
				.Returns(encoded);

			dbAccess
				.UpdateNoteAsync(file.Kind, file.Id, encoded, Arg.Any<DateTime>())
				.ThrowsAsync(failure);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(failureReporter);

			builder.RegisterInstance(noteCipher);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		bool result = await sut.EditAsync(
			file,
			RandomString.Create(20));

		// Assert
		result
			.Should()
			.BeFalse();

		file.Note
			.Should()
			.BeNull();

		failureReporter
			.Received(1)
			.Report(failure, Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: a note rejected by the database is reported and not applied.
	/// </summary>
	[Test]
	public async Task EditAsync_Reports_Failure_When_Database_Update_Fails()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		byte[] encoded = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Encode(file, Arg.Any<string>())
				.Returns(encoded);

			builder.RegisterInstance(noteCipher);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		bool result = await sut.EditAsync(
			file,
			RandomString.Create(20));

		// Assert
		result
			.Should()
			.BeFalse();

		file.Note
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: a note that cannot be converted is reported and never reaches the database.
	/// </summary>
	[Test]
	public async Task EditAsync_Reports_Failure_When_Encoding_Fails()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Encode(file, Arg.Any<string>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(noteCipher);
		});

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		bool result = await sut.EditAsync(
			file,
			RandomString.Create(20));

		// Assert
		result
			.Should()
			.BeFalse();

		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateNoteAsync(default, default, default, default, default);
	}

	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: an exception of the conversion is reported and never reaches the database.
	/// </summary>
	[Test]
	public async Task EditAsync_Reports_Failure_When_Encoding_Throws()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.When(x => x.Encode(file, Arg.Any<string>()))
				.Throw(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(noteCipher);
		});

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		bool result = await sut.EditAsync(
			file,
			RandomString.Create(20));

		// Assert
		result
			.Should()
			.BeFalse();

		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateNoteAsync(default, default, default, default, default);
	}

	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: the note of a file and of a dataset is stored in its file, with the date of the
	/// change.
	/// </summary>
	[Test]
	public async Task EditAsync_Saves_Note_Of_A_File([Values(EntityKind.File, EntityKind.Dataset)] EntityKind kind)
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity entity = EntityFactory.CreateFile();

		FileDto file = ItemDtoFactory.CreateFileDto(id: entity.Id, kind: kind);

		byte[] encoded = RandomValues.CreateBytes(10);

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Encode(file, Arg.Any<string>())
				.Returns(encoded);

			builder.RegisterInstance(database.Context);

			builder.RegisterInstance(noteCipher);

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
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFilesAsync([entity]);

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		bool result = await sut.EditAsync(
			file,
			RandomString.Create(20));

		// Assert
		result
			.Should()
			.BeTrue();

		file.Note
			.Should()
			.BeSameAs(encoded);

		file.UpdatedAt
			.Should()
			.Be(time.GetLocalNow().LocalDateTime);

		FileEntity[] stored = await dbAccess.GetAllFilesAsync(OptionalFileProperties.None);

		stored
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeEquivalentTo(new
			{
				Note = encoded,
				UpdatedAt = time.GetLocalNow().LocalDateTime
			});
	}

	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: the note of a folder is stored in the folder, with the date of the change.
	/// </summary>
	[Test]
	public async Task EditAsync_Saves_Note_Of_A_Folder()
	{
		// Arrange
		using TestDatabase database = new();

		FolderEntity entity = EntityFactory.CreateFolder();

		FolderDto folder = ItemDtoFactory.CreateFolderDto(id: entity.Id);

		byte[] encoded = RandomValues.CreateBytes(10);

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Encode(folder, Arg.Any<string>())
				.Returns(encoded);

			builder.RegisterInstance(database.Context);

			builder.RegisterInstance(noteCipher);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFoldersAsync([entity]);

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		bool result = await sut.EditAsync(
			folder,
			RandomString.Create(20));

		// Assert
		result
			.Should()
			.BeTrue();

		folder.Note
			.Should()
			.BeSameAs(encoded);

		FolderEntity[] stored = await dbAccess.GetAllFoldersAsync();

		stored
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeEquivalentTo(new
			{
				Note = encoded,
				UpdatedAt = time.GetLocalNow().LocalDateTime
			});
	}

	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: the buffer of the replaced note is wiped.
	/// </summary>
	[Test]
	public async Task EditAsync_Zeroes_The_Replaced_Note()
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity entity = EntityFactory.CreateFile();

		FileDto file = ItemDtoFactory.CreateFileDto(id: entity.Id);

		byte[] replaced = RandomValues.CreateBytes(10);

		file.Note = replaced;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Encode(file, Arg.Any<string>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(database.Context);

			builder.RegisterInstance(noteCipher);

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
		});

		await mock
			.Create<IDbAccess>()
			.AddFilesAsync([entity]);

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		await sut.EditAsync(
			file,
			RandomString.Create(20));

		// Assert
		replaced.Should().AllSatisfy(x => x
			.Should()
			.Be(0));
	}
	#endregion
}
