using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Notes;
using NSubstitute;
using Shared.Common;
using System.Text;

namespace DataOrganizer.UnitTests.TestTypes.Notes;

[TestFixture(Description = $@"Tests of ""{nameof(NoteCipher)}"" type")]
internal class NoteCipherTests
{
	#region Methods
	/// <summary>
	/// <see cref="NoteCipher.Decode" />: a password keeper protects its own note as well.
	/// </summary>
	[Test]
	public void Decode_Decrypts_The_Note_Of_The_Keeper_Itself()
	{
		// Arrange
		string text = AppUtils.CreateRandomString(20);

		FolderModelDto keeper = CreateKeeper(isUnlocked: true);

		keeper.Note = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

			encryption
				.DecryptSessionContents(keeper.Note, keeper.SessionEncryptedDek!)
				.Returns(Encoding.UTF8.GetBytes(text));

			builder.RegisterInstance(encryption);
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
		string text = AppUtils.CreateRandomString(20);

		FolderModelDto keeper = CreateKeeper(isUnlocked: true);

		FolderModelDto nested = TestUtils.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper
			.Children
			.Add(nested);

		nested.Parent = keeper;

		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		nested
			.Children
			.Add(file);

		file.Parent = nested;

		file.Note = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

			encryption
				.DecryptSessionContents(file.Note, keeper.SessionEncryptedDek!)
				.Returns(Encoding.UTF8.GetBytes(text));

			builder.RegisterInstance(encryption);
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
		string text = AppUtils.CreateRandomString(20);

		FolderModelDto keeper = CreateKeeper(isUnlocked: true);

		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper
			.Children
			.Add(file);

		file.Parent = keeper;

		file.Note = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

			encryption
				.DecryptSessionContents(file.Note, keeper.SessionEncryptedDek!)
				.Returns(Encoding.UTF8.GetBytes(text));

			builder.RegisterInstance(encryption);
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
	/// <see cref="NoteCipher.Decode" />: returns <c>null</c> when the decryption fails.
	/// </summary>
	[Test]
	public void Decode_Returns_Null_When_Decryption_Fails()
	{
		// Arrange
		FolderModelDto keeper = CreateKeeper(isUnlocked: true);

		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper
			.Children
			.Add(file);

		file.Parent = keeper;

		file.Note = TestUtils.CreateRandomBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

			encryption
				.DecryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(encryption);
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
	/// <see cref="NoteCipher.Decode" />: returns <c>null</c> without touching the encryption when the password keeper is locked.
	/// </summary>
	[Test]
	public void Decode_Returns_Null_When_Keeper_Is_Locked()
	{
		// Arrange
		FolderModelDto keeper = CreateKeeper(isUnlocked: false);

		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		keeper
			.Children
			.Add(file);

		file.Parent = keeper;

		file.Note = TestUtils.CreateRandomBytes(10);

		IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(encryption));

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		string? result = sut.Decode(file);

		// Assert
		result
			.Should()
			.BeNull();

		encryption
			.DidNotReceive()
			.DecryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NoteCipher.Decode" />: returns <c>null</c> when the object is marked as protected but has no password keeper.
	/// </summary>
	[Test]
	public void Decode_Returns_Null_When_Keeper_Is_Missing()
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		file.Note = TestUtils.CreateRandomBytes(10);

		IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(encryption));

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		string? result = sut.Decode(file);

		// Assert
		result
			.Should()
			.BeNull();

		encryption
			.DidNotReceive()
			.DecryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NoteCipher.Decode" />: returns <c>null</c> when the note is missing or empty.
	/// </summary>
	[Test]
	public void Decode_Returns_Null_When_Note_Is_Absent([Values] bool isEmpty)
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto();

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
		string text = AppUtils.CreateRandomString(20);

		FileModelDto file = TestUtils.CreateFileDto();

		file.Note = Encoding.UTF8.GetBytes(text);

		IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(encryption));

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		string? result = sut.Decode(file);

		// Assert
		result
			.Should()
			.Be(text);

		encryption
			.DidNotReceive()
			.DecryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NoteCipher.Encode" />: encrypts the note of an object that belongs to an unlocked password keeper.
	/// </summary>
	[Test]
	public void Encode_Encrypts_When_Protected()
	{
		// Arrange
		string text = AppUtils.CreateRandomString(20);

		FolderModelDto keeper = CreateKeeper(isUnlocked: true);

		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		keeper
			.Children
			.Add(file);

		file.Parent = keeper;

		byte[] encrypted = TestUtils.CreateRandomBytes(10);

		byte[]? passedText = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

			encryption
				.EncryptSessionContents(Arg.Any<byte[]>(), keeper.SessionEncryptedDek!)
				.Returns(x =>
				{
					// A copy is required: the source buffer is zeroed right after the call.
					passedText = [.. x.ArgAt<byte[]>(0)];

					return encrypted;
				});

			builder.RegisterInstance(encryption);
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
	/// <see cref="NoteCipher.Encode" />: returns <c>null</c> without touching the encryption when the password keeper is locked.
	/// </summary>
	[Test]
	public void Encode_Returns_Null_When_Keeper_Is_Locked()
	{
		// Arrange
		FolderModelDto keeper = CreateKeeper(isUnlocked: false);

		FileModelDto file = TestUtils.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		keeper
			.Children
			.Add(file);

		file.Parent = keeper;

		IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(encryption));

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		byte[]? result = sut.Encode(file, AppUtils.CreateRandomString(20));

		// Assert
		result
			.Should()
			.BeNull();

		encryption
			.DidNotReceive()
			.EncryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NoteCipher.Encode" />: blank text is stored as <c>null</c>.
	/// </summary>
	[Test]
	public void Encode_Returns_Null_When_Text_Is_Blank([Values(null, "", "   ")] string? text)
	{
		// Arrange
		FileModelDto file = TestUtils.CreateFileDto();

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
		string text = AppUtils.CreateRandomString(20);

		FileModelDto file = TestUtils.CreateFileDto();

		IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(encryption));

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		byte[]? result = sut.Encode(file, text);

		// Assert
		result
			.Should()
			.Equal(Encoding.UTF8.GetBytes(text));

		encryption
			.DidNotReceive()
			.EncryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NoteCipher.Encode" />: the plain text buffer is zeroed once the encrypted form is produced.
	/// </summary>
	[Test]
	public void Encode_Zeroes_The_Plain_Text_Buffer()
	{
		// Arrange
		FolderModelDto keeper = CreateKeeper(isUnlocked: true);

		byte[]? passedText = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

			encryption
				.EncryptSessionContents(Arg.Any<byte[]>(), keeper.SessionEncryptedDek!)
				.Returns(x =>
				{
					passedText = x.ArgAt<byte[]>(0);

					return TestUtils.CreateRandomBytes(10);
				});

			builder.RegisterInstance(encryption);
		});

		NoteCipher sut = mock.Create<NoteCipher>();

		// Act
		_ = sut.Encode(keeper, AppUtils.CreateRandomString(20));

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
	/// Creates a password keeper folder; the session DEK is present only when it is unlocked.
	/// </summary>
	private static FolderModelDto CreateKeeper(bool isUnlocked)
	{
		FolderModelDto keeper = TestUtils.CreateFolderDto(
			encryptionStatus: isUnlocked ? EncryptionStatus.Decrypted : EncryptionStatus.Encrypted);

		keeper.EncryptedDek = TestUtils.CreateRandomBytes(10);

		keeper.PasswordHash = AppUtils.CreateRandomString(10);

		if (isUnlocked)
		{
			keeper.SessionEncryptedDek = TestUtils.CreateRandomBytes(10);
		}

		return keeper;
	}
	#endregion
}
