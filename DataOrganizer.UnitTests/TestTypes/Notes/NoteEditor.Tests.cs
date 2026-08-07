using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Messages;
using DataOrganizer.Services.Notes;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using Repository.Interfaces;
using Shared.Common;
using Shared.Properties;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.Notes;

[TestFixture(Description = $@"Tests of ""{nameof(NoteEditor)}"" type")]
internal class NoteEditorTests
{
	#region Methods
	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: blank text removes the note.
	/// </summary>
	[Test]
	public async Task EditAsync_Deletes_Note_When_Text_Is_Blank([Values(null, "", "   ")] string? note)
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto();

		file.Note = TestUtils.CreateRandomBytes(10);

		StrongReferenceMessenger messenger = new();

		ShowSnackbarMessage? receivedSnackbar = null;

		object recipient = new();

		messenger.Register<ShowSnackbarMessage>(recipient, (_, message) => receivedSnackbar = message);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFilePropertiesAsync(
					file.Id,
					Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Encode(file, Arg.Any<string>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(noteCipher);

			builder.RegisterInstance(messenger).As<IMessenger>();
		});

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		bool result = await sut.EditAsync(
			file,
			note,
			DateTime.Now);

		// Assert
		result
			.Should()
			.BeTrue();

		file.Note
			.Should()
			.BeNull();

		receivedSnackbar?.Text
			.Should()
			.Be(Strings.NoteHasBeenDeleted);
	}

	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: a note rejected by the database is reported and not applied.
	/// </summary>
	[Test]
	public async Task EditAsync_Reports_Failure_When_Database_Update_Fails()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto();

		byte[] encoded = TestUtils.CreateRandomBytes(10);

		StrongReferenceMessenger messenger = new();

		ShowSnackbarMessage? receivedSnackbar = null;

		object recipient = new();

		messenger.Register<ShowSnackbarMessage>(recipient, (_, message) => receivedSnackbar = message);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Encode(file, Arg.Any<string>())
				.Returns(encoded);

			builder.RegisterInstance(noteCipher);

			builder.RegisterInstance(messenger).As<IMessenger>();
		});

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		bool result = await sut.EditAsync(
			file,
			AppUtils.CreateRandomString(20),
			DateTime.Now);

		// Assert
		result
			.Should()
			.BeFalse();

		file.Note
			.Should()
			.BeNull();

		receivedSnackbar?.Text
			.Should()
			.Be(Strings.FailedToSaveNote);
	}

	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: a note that cannot be converted is reported and never reaches the database.
	/// </summary>
	[Test]
	public async Task EditAsync_Reports_Failure_When_Encoding_Fails()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		StrongReferenceMessenger messenger = new();

		ShowSnackbarMessage? receivedSnackbar = null;

		object recipient = new();

		messenger.Register<ShowSnackbarMessage>(recipient, (_, message) => receivedSnackbar = message);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Encode(file, Arg.Any<string>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(noteCipher);

			builder.RegisterInstance(messenger).As<IMessenger>();
		});

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		bool result = await sut.EditAsync(
			file,
			AppUtils.CreateRandomString(20),
			DateTime.Now);

		// Assert
		result
			.Should()
			.BeFalse();

		receivedSnackbar?.Text
			.Should()
			.Be(Strings.FailedToSaveNote);

		await dbAccess.DidNotReceive().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: an exception of the conversion is reported and never reaches the database.
	/// </summary>
	[Test]
	public async Task EditAsync_Reports_Failure_When_Encoding_Throws()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto();

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		StrongReferenceMessenger messenger = new();

		ShowSnackbarMessage? receivedSnackbar = null;

		object recipient = new();

		messenger.Register<ShowSnackbarMessage>(recipient, (_, message) => receivedSnackbar = message);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.When(x => x.Encode(file, Arg.Any<string>()))
				.Throw(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(noteCipher);

			builder.RegisterInstance(messenger).As<IMessenger>();
		});

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		bool result = await sut.EditAsync(
			file,
			AppUtils.CreateRandomString(20),
			DateTime.Now);

		// Assert
		result
			.Should()
			.BeFalse();

		receivedSnackbar?.Text
			.Should()
			.Be(Strings.FailedToSaveNote);

		await dbAccess.DidNotReceive().UpdateFilePropertiesAsync(
			Arg.Any<Guid>(),
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: the note of a file and of a dataset is stored through the file update.
	/// </summary>
	[Test]
	public async Task EditAsync_Saves_Note_Of_A_File([Values(EntityType.File, EntityType.DataSet)] EntityType entityType)
	{
		// Arrange
		FileModelDto file = CreateFile(entityType);

		byte[] encoded = TestUtils.CreateRandomBytes(10);

		DateTime updatedDate = DateTime.Now.AddDays(1);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		StrongReferenceMessenger messenger = new();

		ShowSnackbarMessage? receivedSnackbar = null;

		object recipient = new();

		messenger.Register<ShowSnackbarMessage>(recipient, (_, message) => receivedSnackbar = message);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFilePropertiesAsync(
					file.Id,
					Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Encode(file, Arg.Any<string>())
				.Returns(encoded);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(noteCipher);

			builder.RegisterInstance(messenger).As<IMessenger>();
		});

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		bool result = await sut.EditAsync(
			file,
			AppUtils.CreateRandomString(20),
			updatedDate);

		// Assert
		result
			.Should()
			.BeTrue();

		file.Note
			.Should()
			.BeSameAs(encoded);

		file.UpdatedDate
			.Should()
			.Be(updatedDate);

		receivedSnackbar?.Text
			.Should()
			.Be(Strings.NoteHasBeenSaved);

		await dbAccess.Received(1).UpdateFilePropertiesAsync(
			file.Id,
			Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: the note of a folder is stored through the folder update.
	/// </summary>
	[Test]
	public async Task EditAsync_Saves_Note_Of_A_Folder()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		byte[] encoded = TestUtils.CreateRandomBytes(10);

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.UpdateFolderPropertiesAsync(
					folder.Id,
					Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Encode(folder, Arg.Any<string>())
				.Returns(encoded);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(noteCipher);
		});

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		bool result = await sut.EditAsync(
			folder,
			AppUtils.CreateRandomString(20),
			DateTime.Now);

		// Assert
		result
			.Should()
			.BeTrue();

		folder.Note
			.Should()
			.BeSameAs(encoded);

		await dbAccess.Received(1).UpdateFolderPropertiesAsync(
			folder.Id,
			Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="NoteEditor.EditAsync" />: the buffer of the replaced note is wiped.
	/// </summary>
	[Test]
	public async Task EditAsync_Zeroes_The_Replaced_Note()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto();

		byte[] replaced = TestUtils.CreateRandomBytes(10);

		file.Note = replaced;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.UpdateFilePropertiesAsync(
					file.Id,
					Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Encode(file, Arg.Any<string>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(noteCipher);
		});

		NoteEditor sut = mock.Create<NoteEditor>();

		// Act
		await sut.EditAsync(
			file,
			AppUtils.CreateRandomString(20),
			DateTime.Now);

		// Assert
		replaced.Should().AllSatisfy(x => x
			.Should()
			.Be(0));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a file or a dataset.
	/// </summary>
	private static FileModelDto CreateFile(EntityType entityType) => new()
	{
		CreatedDate = DateTime.Now,
		EntityType = entityType,
		Id = Guid.NewGuid(),
		Index = 0,
		Name = AppUtils.CreateRandomString(10),
		UpdatedDate = DateTime.Now
	};
	#endregion
}
