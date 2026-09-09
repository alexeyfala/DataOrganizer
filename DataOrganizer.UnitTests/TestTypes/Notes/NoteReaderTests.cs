using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces.Notes;
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
		FileDto file = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		file.Note = TestData.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INoteCipher noteCipher = Substitute.For<INoteCipher>();

			noteCipher
				.Decode(file)
				.Returns((string?)null);

			builder.RegisterInstance(noteCipher);
		});

		NoteReader sut = mock.Create<NoteReader>();

		// Act
		string? result = sut.ReadNote(file);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="NoteReader.ReadNote" />: returns the note of an object as plain text.
	/// </summary>

	[Test]
	public void ReadNote_Returns_Decoded_Note()
	{
		// Arrange
		string text = RandomString.Create(20);

		FileDto file = TestData.CreateFileDto();

		file.Note = TestData.CreateRandomBytes(10);

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
		FileDto file = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		file.Note = TestData.CreateRandomBytes(10);

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
			.Decode(Arg.Any<ExplorerItemDtoBase>());
	}

	/// <summary>
	/// <see cref="NoteReader.ReadNote" />: a missing object is ignored.
	/// </summary>

	[Test]
	public void ReadNote_Returns_Null_When_Item_Is_Null()
	{
		// Arrange
		INoteCipher noteCipher = Substitute.For<INoteCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(noteCipher));

		NoteReader sut = mock.Create<NoteReader>();

		// Act
		string? result = sut.ReadNote(null);

		// Assert
		result
			.Should()
			.BeNull();

		noteCipher
			.DidNotReceive()
			.Decode(Arg.Any<ExplorerItemDtoBase>());
	}

	/// <summary>
	/// <see cref="NoteReader.ReadNote" />: objects without a note are ignored.
	/// </summary>

	[Test]
	public void ReadNote_Returns_Null_When_Note_Is_Absent([Values] bool isEmpty)
	{
		// Arrange
		FileDto file = TestData.CreateFileDto();

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
			.Decode(Arg.Any<ExplorerItemDtoBase>());
	}
	#endregion
}
