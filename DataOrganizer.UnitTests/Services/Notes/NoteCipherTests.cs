using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Notes;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared.Common;
using System;
using System.Security.Cryptography;
using System.Text;
using TestSupport;

namespace DataOrganizer.UnitTests.Services.Notes;

[TestFixture(Description = $@"Tests of ""{nameof(NoteCipher)}"" type")]
internal class NoteCipherTests
{
	#region Methods
	/// <summary>
	/// <see cref="NoteCipher.Decode" />: the note is read as the note of its own object,
	/// so the ciphertext is bound to the object and to the field.
	/// </summary>
	[Test]
	public void Decode_Binds_The_Note_To_Its_Object()
	{
		// Arrange
		FolderDto keeper = CreateKeeper(isUnlocked: true);

		FileDto file = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper
			.Children
			.Add(file);

		file.Parent = keeper;

		file.Note = TestData.CreateRandomBytes(10);

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(contentCipher));

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		sut.Decode(file);

		// Assert
		contentCipher
			.Received(1)
			.TryDecrypt(keeper.Id, ContentIdentity.ForNote(file.Id), file.Note);
	}

	/// <summary>
	/// <see cref="NoteCipher.Decode" />: a password keeper protects its own note as well.
	/// </summary>
	[Test]
	public void Decode_Decrypts_The_Note_Of_The_Keeper_Itself()
	{
		// Arrange
		string text = RandomString.Create(20);

		FolderDto keeper = CreateKeeper(isUnlocked: true);

		keeper.Note = TestData.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(keeper.Id, Arg.Any<ContentIdentity>(), keeper.Note)
				.Returns(Encoding.UTF8.GetBytes(text));

			builder.RegisterInstance(contentCipher);
		});

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		string? result = sut.Decode(keeper);

		// Assert
		result
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="NoteCipher.Decode" />: the password keeper is looked up through the whole chain of parents.
	/// </summary>
	[Test]
	public void Decode_Decrypts_When_Keeper_Is_A_Distant_Ancestor()
	{
		// Arrange
		string text = RandomString.Create(20);

		FolderDto keeper = CreateKeeper(isUnlocked: true);

		FolderDto nested = TestData.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper
			.Children
			.Add(nested);

		nested.Parent = keeper;

		FileDto file = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		nested
			.Children
			.Add(file);

		file.Parent = nested;

		file.Note = TestData.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(keeper.Id, Arg.Any<ContentIdentity>(), file.Note)
				.Returns(Encoding.UTF8.GetBytes(text));

			builder.RegisterInstance(contentCipher);
		});

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		string? result = sut.Decode(file);

		// Assert
		result
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="NoteCipher.Decode" />: decrypts the note of an object that belongs to an unlocked password keeper.
	/// </summary>
	[Test]
	public void Decode_Decrypts_When_Protected()
	{
		// Arrange
		string text = RandomString.Create(20);

		FolderDto keeper = CreateKeeper(isUnlocked: true);

		FileDto file = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper
			.Children
			.Add(file);

		file.Parent = keeper;

		file.Note = TestData.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(keeper.Id, Arg.Any<ContentIdentity>(), file.Note)
				.Returns(Encoding.UTF8.GetBytes(text));

			builder.RegisterInstance(contentCipher);
		});

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		string? result = sut.Decode(file);

		// Assert
		result
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="NoteCipher.Decode" />: a note that fails authentication is reported as unreadable,
	/// so the failure never reaches the interface being rendered.
	/// </summary>
	[Test]
	public void Decode_Does_Not_Propagate_A_Failure()
	{
		// Arrange
		FolderDto keeper = CreateKeeper(isUnlocked: true);

		keeper.Note = TestData.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			// The cipher swallows the cryptographic failure and answers with a refusal.
			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(contentCipher);
		});

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		string? result = sut.Decode(keeper);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="NoteCipher.Decode" />: returns <c>null</c> when the decryption fails.
	/// </summary>
	[Test]
	public void Decode_Returns_Null_When_Decryption_Fails()
	{
		// Arrange
		FolderDto keeper = CreateKeeper(isUnlocked: true);

		FileDto file = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper
			.Children
			.Add(file);

		file.Parent = keeper;

		file.Note = TestData.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			// The cipher swallows the cryptographic failure and answers with a refusal.
			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(contentCipher);
		});

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		string? result = sut.Decode(file);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="NoteCipher.Decode" />: a locked password keeper is never asked for its key.
	/// </summary>
	[Test]
	public void Decode_Returns_Null_When_Keeper_Is_Locked()
	{
		// Arrange
		FolderDto keeper = CreateKeeper(isUnlocked: false);

		FileDto file = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		keeper
			.Children
			.Add(file);

		file.Parent = keeper;

		file.Note = TestData.CreateRandomBytes(10);

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose();

		NoteCipher sut = mock.Create<NoteCipher>(TypedParameter.From(contentCipher));

		// Act
		string? result = sut.Decode(file);

		// Assert
		result
			.Should()
			.BeNull();

		contentCipher
			.DidNotReceive()
			.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NoteCipher.Decode" />: returns <c>null</c> when the object is marked as protected but has no password keeper.
	/// </summary>
	[Test]
	public void Decode_Returns_Null_When_Keeper_Is_Missing()
	{
		// Arrange
		FileDto file = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		file.Note = TestData.CreateRandomBytes(10);

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose();

		NoteCipher sut = mock.Create<NoteCipher>(TypedParameter.From(contentCipher));

		// Act
		string? result = sut.Decode(file);

		// Assert
		result
			.Should()
			.BeNull();

		contentCipher
			.DidNotReceive()
			.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NoteCipher.Decode" />: returns <c>null</c> when the note is missing or empty.
	/// </summary>
	[Test]
	public void Decode_Returns_Null_When_Note_Is_Absent([Values] bool isEmpty)
	{
		// Arrange
		FileDto file = TestData.CreateFileDto();

		file.Note = isEmpty ? [] : null;

		using AutoMock mock = AutoMock.GetLoose();

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		string? result = sut.Decode(file);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="NoteCipher.Decode" />: reads the note as plain text when the object is not protected.
	/// </summary>
	[Test]
	public void Decode_Returns_Text_When_Not_Protected()
	{
		// Arrange
		string text = RandomString.Create(20);

		FileDto file = TestData.CreateFileDto();

		file.Note = Encoding.UTF8.GetBytes(text);

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose();

		NoteCipher sut = mock.Create<NoteCipher>(TypedParameter.From(contentCipher));

		// Act
		string? result = sut.Decode(file);

		// Assert
		result
			.Should()
			.Be(text);

		contentCipher
			.DidNotReceive()
			.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NoteCipher.Encode" />: encrypts the note of an object that belongs to an unlocked password keeper.
	/// </summary>
	[Test]
	public void Encode_Encrypts_When_Protected()
	{
		// Arrange
		string text = RandomString.Create(20);

		FolderDto keeper = CreateKeeper(isUnlocked: true);

		FileDto file = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper
			.Children
			.Add(file);

		file.Parent = keeper;

		byte[] encrypted = TestData.CreateRandomBytes(10);

		byte[]? passedText = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryEncrypt(keeper.Id, Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x =>
				{
					// A copy is required: the source buffer is zeroed right after the call.
					passedText = [.. x.ArgAt<byte[]>(2)];

					return encrypted;
				});

			builder.RegisterInstance(contentCipher);
		});

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		byte[]? result = sut.Encode(file, text);

		// Assert
		result
			.Should()
			.BeSameAs(encrypted);

		passedText
			.Should()
			.Equal(Encoding.UTF8.GetBytes(text));
	}

	/// <summary>
	/// <see cref="NoteCipher.Encode" />: a locked password keeper is never asked for its key.
	/// </summary>
	[Test]
	public void Encode_Returns_Null_When_Keeper_Is_Locked()
	{
		// Arrange
		FolderDto keeper = CreateKeeper(isUnlocked: false);

		FileDto file = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		keeper
			.Children
			.Add(file);

		file.Parent = keeper;

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose();

		NoteCipher sut = mock.Create<NoteCipher>(TypedParameter.From(contentCipher));

		// Act
		byte[]? result = sut.Encode(file, RandomString.Create(20));

		// Assert
		result
			.Should()
			.BeNull();

		contentCipher
			.DidNotReceive()
			.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NoteCipher.Encode" />: blank text is stored as <c>null</c>.
	/// </summary>
	[Test]
	public void Encode_Returns_Null_When_Text_Is_Blank([Values(null, "", "   ")] string? text)
	{
		// Arrange
		FileDto file = TestData.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose();

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		byte[]? result = sut.Encode(file, text);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="NoteCipher.Encode" />: stores UTF-8 bytes as they are when the object is not protected.
	/// </summary>
	[Test]
	public void Encode_Returns_Utf8_When_Not_Protected()
	{
		// Arrange
		string text = RandomString.Create(20);

		FileDto file = TestData.CreateFileDto();

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		using AutoMock mock = AutoMock.GetLoose();

		NoteCipher sut = mock.Create<NoteCipher>(TypedParameter.From(contentCipher));

		// Act
		byte[]? result = sut.Encode(file, text);

		// Assert
		result
			.Should()
			.Equal(Encoding.UTF8.GetBytes(text));

		contentCipher
			.DidNotReceive()
			.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NoteCipher.Encode" />: the plain text buffer is zeroed once the encrypted form is produced.
	/// </summary>
	[Test]
	public void Encode_Zeroes_The_Plain_Text_Buffer()
	{
		// Arrange
		FolderDto keeper = CreateKeeper(isUnlocked: true);

		byte[]? passedText = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryEncrypt(keeper.Id, Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x =>
				{
					passedText = x.ArgAt<byte[]>(2);

					return TestData.CreateRandomBytes(10);
				});

			builder.RegisterInstance(contentCipher);
		});

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		_ = sut.Encode(keeper, RandomString.Create(20));

		// Assert
		passedText
			.Should()
			.AllSatisfy(x => x
				.Should()
				.Be(0));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a password keeper folder; whether its key is available is decided by the key store.
	/// </summary>
	private static FolderDto CreateKeeper(bool isUnlocked)
	{
		FolderDto keeper = TestData.CreateFolderDto(
			encryptionStatus: isUnlocked ? EncryptionStatus.Decrypted : EncryptionStatus.Encrypted);

		keeper.EncryptedDek = TestData.CreateRandomBytes(10);

		return keeper;
	}
	#endregion
}
