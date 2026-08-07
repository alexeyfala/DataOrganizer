using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces.Notes;
using DataOrganizer.Messages;
using DataOrganizer.Services.Notes;
using NSubstitute;
using Shared.Common;

namespace DataOrganizer.UnitTests.TestTypes.Notes;

[TestFixture(Description = $@"Tests of ""{nameof(NoteReader)}"" type")]
internal class NoteReaderTests
{
	#region Methods
	/// <summary>
	/// <see cref="NoteReader.ReadNote" />: reports a snackbar when the note cannot be decoded.
	/// </summary>
	[Test]
	public void ReadNote_Reports_Failure_When_Decoding_Fails()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		file.Note = TestUtils.CreateRandomBytes(10);

		StrongReferenceMessenger messenger = new();

		ShowSnackbarMessage? receivedSnackbar = null;

		object recipient = new();

		messenger.Register<ShowSnackbarMessage>(recipient, (_, message) => receivedSnackbar = message);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Decode(file)
				.Returns((string?)null);

			builder.RegisterInstance(noteCipher);

			builder.RegisterInstance(messenger).As<IMessenger>();
		});

		NoteReader sut = mock.Create<NoteReader>();

		// Act
		string? result = sut.ReadNote(file);

		// Assert
		result
			.Should()
			.BeNull();

		receivedSnackbar
			.Should()
			.NotBeNull();

		receivedSnackbar.Level
			.Should()
			.Be(SnackbarMessageLevel.Error);
	}

	/// <summary>
	/// <see cref="NoteReader.ReadNote" />: returns the note of an object as plain text.
	/// </summary>
	[Test]
	public void ReadNote_Returns_Decoded_Note()
	{
		// Arrange
		string text = AppUtils.CreateRandomString(20);

		FileModelDto file = TestUtils.CreateFileDto();

		file.Note = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Decode(file)
				.Returns(text);

			builder.RegisterInstance(noteCipher);
		});

		NoteReader sut = mock.Create<NoteReader>();

		// Act
		string? result = sut.ReadNote(file);

		// Assert
		result
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="NoteReader.ReadNote" />: an encrypted note is not a failure, so it is skipped silently.
	/// </summary>
	[Test]
	public void ReadNote_Returns_Null_When_Encrypted()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		file.Note = TestUtils.CreateRandomBytes(10);

		INoteCipher noteCipher = Substitute.For<INoteCipher>();

		StrongReferenceMessenger messenger = new();

		ShowSnackbarMessage? receivedSnackbar = null;

		object recipient = new();

		messenger.Register<ShowSnackbarMessage>(recipient, (_, message) => receivedSnackbar = message);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(noteCipher);

			builder.RegisterInstance(messenger).As<IMessenger>();
		});

		NoteReader sut = mock.Create<NoteReader>();

		// Act
		string? result = sut.ReadNote(file);

		// Assert
		result
			.Should()
			.BeNull();

		receivedSnackbar
			.Should()
			.BeNull();

		noteCipher
			.DidNotReceive()
			.Decode(Arg.Any<ExplorerModelBaseDto>());
	}

	/// <summary>
	/// <see cref="NoteReader.ReadNote" />: unsupported objects are ignored.
	/// </summary>
	[Test]
	public void ReadNote_Returns_Null_When_Item_Is_Not_An_Explorer_Object()
	{
		// Arrange
		INoteCipher noteCipher = Substitute.For<INoteCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(noteCipher));

		NoteReader sut = mock.Create<NoteReader>();

		// Act
		string? result = sut.ReadNote(new object());

		// Assert
		result
			.Should()
			.BeNull();

		noteCipher
			.DidNotReceive()
			.Decode(Arg.Any<ExplorerModelBaseDto>());
	}

	/// <summary>
	/// <see cref="NoteReader.ReadNote" />: objects without a note are ignored.
	/// </summary>
	[Test]
	public void ReadNote_Returns_Null_When_Note_Is_Absent([Values] bool isEmpty)
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto();

		file.Note = isEmpty ? [] : null;

		INoteCipher noteCipher = Substitute.For<INoteCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(noteCipher));

		NoteReader sut = mock.Create<NoteReader>();

		// Act
		string? result = sut.ReadNote(file);

		// Assert
		result
			.Should()
			.BeNull();

		noteCipher
			.DidNotReceive()
			.Decode(Arg.Any<ExplorerModelBaseDto>());
	}
	#endregion
}
