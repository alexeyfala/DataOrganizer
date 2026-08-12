using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Services.Encryption;
using System;
using System.Security.Authentication;
using System.Security.Cryptography;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(EncryptionService)}"" type")]
internal class EncryptionServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="EncryptionService.Decrypt" />: a wrong password is reported as rejected credentials.
	/// </summary>
	[Test]
	public void Decrypt_Cannot_Decrypt_With_Wrong_Password()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		// Act, Assert
		byte[]? encrypted = sut.Encrypt(
			input,
			TextHelper.Utf8Encoding.GetBytes("SomePassword"),
			[]);

		encrypted
			.Should()
			.NotBeNull();

		Action act = () => sut.Decrypt(
			encrypted,
			TextHelper.Utf8Encoding.GetBytes("WrongPassword"),
			[]);

		act
			.Should()
			.ThrowExactly<InvalidCredentialException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.Decrypt" />: input produced by the session-based path is refused as a foreign format.
	/// </summary>
	[Test]
	public void Decrypt_Rejects_Session_Encrypted_Input()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		byte[] sessionId = TestUtils.CreateRandomBytes(32);

		// Act
		byte[]? encrypted = sut.EncryptWithSessionId(input, sessionId, []);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		Action act = () => sut.Decrypt(encrypted, sessionId, []);

		// Assert
		act
			.Should()
			.ThrowExactly<CryptographicException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.Decrypt" />: malformed input is refused as damaged data.
	/// </summary>
	[Test]
	public void Decrypt_Throws_On_Malformed_Input()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] password = TextHelper
			.Utf8Encoding
			.GetBytes("SomePassword");

		// Act
		Action act = () => sut.Decrypt([1, 2, 3], password, []);

		// Assert
		act
			.Should()
			.ThrowExactly<CryptographicException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithDek" />: the associated data is authenticated, so
	/// neither a different value nor an absent one opens the ciphertext.
	/// </summary>
	[Test]
	public void DecryptWithDek_Cannot_Decrypt_With_Wrong_Associated_Data()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		byte[] dek = sut.CreateRandomDek();

		byte[] associatedData = TestUtils.CreateRandomBytes(37);

		// Act
		byte[]? encrypted = sut.EncryptWithDek(input, dek, associatedData);

		encrypted
			.Should()
			.NotBeNull();

		// Assert
		Action withOtherData = () => sut.DecryptWithDek(encrypted, dek, TestUtils.CreateRandomBytes(37));

		withOtherData
			.Should()
			.ThrowExactly<AuthenticationTagMismatchException>();

		Action withoutData = () => sut.DecryptWithDek(encrypted, dek, []);

		withoutData
			.Should()
			.ThrowExactly<AuthenticationTagMismatchException>();

		sut.DecryptWithDek(encrypted, dek, associatedData)
			.Should()
			.Equal(input);
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithDek" />: a wrong DEK is reported as a tag mismatch.
	/// </summary>
	[Test]
	public void DecryptWithDek_Cannot_Decrypt_With_Wrong_Dek()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		byte[] dek = sut.CreateRandomDek();

		byte[] wrongDek = sut.CreateRandomDek();

		// Act
		byte[]? encrypted = sut.EncryptWithDek(input, dek, []);

		encrypted
			.Should()
			.NotBeNull();

		Action act = () => sut.DecryptWithDek(encrypted, wrongDek, []);

		// Assert
		act
			.Should()
			.ThrowExactly<AuthenticationTagMismatchException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithDek" />: malformed input is refused as damaged data.
	/// </summary>
	[Test]
	public void DecryptWithDek_Throws_On_Malformed_Input()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] dek = sut.CreateRandomDek();

		// Act
		Action act = () => sut.DecryptWithDek([1, 2, 3], dek, []);

		// Assert
		act
			.Should()
			.ThrowExactly<CryptographicException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithSessionId" />: a wrong session identifier is reported as a tag mismatch.
	/// </summary>
	[Test]
	public void DecryptWithSessionId_Cannot_Decrypt_With_Wrong_Session_Id()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		byte[] sessionId = TestUtils.CreateRandomBytes(32);

		byte[] wrongSessionId = TestUtils.CreateRandomBytes(32);

		// Act
		byte[]? encrypted = sut.EncryptWithSessionId(input, sessionId, []);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		Action act = () => sut.DecryptWithSessionId(encrypted, wrongSessionId, []);

		// Assert
		act
			.Should()
			.ThrowExactly<AuthenticationTagMismatchException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithSessionId" />: input produced by the password-based path is refused as a foreign format.
	/// </summary>
	[Test]
	public void DecryptWithSessionId_Rejects_Password_Encrypted_Input()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		byte[] password = TextHelper
			.Utf8Encoding
			.GetBytes("SomePassword");

		// Act
		byte[]? encrypted = sut.Encrypt(input, password, []);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		Action act = () => sut.DecryptWithSessionId(encrypted, password, []);

		// Assert
		act
			.Should()
			.ThrowExactly<CryptographicException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithSessionId" />: malformed input is refused as damaged data.
	/// </summary>
	[Test]
	public void DecryptWithSessionId_Throws_On_Malformed_Input()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] sessionId = TestUtils.CreateRandomBytes(32);

		// Act
		Action act = () => sut.DecryptWithSessionId([1, 2, 3], sessionId, []);

		// Assert
		act
			.Should()
			.ThrowExactly<CryptographicException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.Encrypt" />, <see cref="EncryptionService.Decrypt" />: a round-trip restores the original plaintext while the ciphertext differs from it.
	/// </summary>
	[Test]
	public void Encrypt_Decrypt_Checking_Functionality()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		byte[] password = TextHelper
			.Utf8Encoding
			.GetBytes("SomePassword");

		// Act, Assert
		byte[]? encrypted = sut.Encrypt(input, password, []);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		byte[]? decrypted = sut.Decrypt(encrypted, password, []);

		decrypted
			.Should()
			.NotBeNullOrEmpty();

		TextHelper.Utf8Encoding.GetString(encrypted)
			.Should()
			.NotBe(TextHelper.LoremIpsum);

		TextHelper.Utf8Encoding.GetString(decrypted)
			.Should()
			.Be(TextHelper.LoremIpsum);
	}

	/// <summary>
	/// Every path keeps its own version byte and its own on-the-wire layout:
	/// the DEK one carries no salt, the other two do.
	/// </summary>
	[Test]
	public void EncryptedBlobs_Keep_Their_Layout()
	{
		// Arrange
		const int NonceSize = 24;

		const int SaltSize = 16;

		const int TagSize = 16;

		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		byte[] secret = TestUtils.CreateRandomBytes(32);

		// Act
		byte[]? password = sut.Encrypt(input, secret, []);

		byte[]? dek = sut.EncryptWithDek(input, sut.CreateRandomDek(), []);

		byte[]? session = sut.EncryptWithSessionId(input, secret, []);

		// Assert
		password
			.Should()
			.NotBeNull()
			.And
			.HaveElementAt(0, 0x01)
			.And
			.HaveCount(1 + SaltSize + NonceSize + input.Length + TagSize);

		dek
			.Should()
			.NotBeNull()
			.And
			.HaveElementAt(0, 0x02)
			.And
			.HaveCount(1 + NonceSize + input.Length + TagSize);

		session
			.Should()
			.NotBeNull()
			.And
			.HaveElementAt(0, 0x03)
			.And
			.HaveCount(1 + SaltSize + NonceSize + input.Length + TagSize);
	}

	/// <summary>
	/// <see cref="EncryptionService.EncryptWithDek" />, <see cref="EncryptionService.DecryptWithDek" />: a DEK round-trip restores the original plaintext while the ciphertext differs from it.
	/// </summary>
	[Test]
	public void EncryptWithDek_DecryptWithDek_Checking_Functionality()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		byte[] dek = sut.CreateRandomDek();

		// Act, Assert
		byte[]? encrypted = sut.EncryptWithDek(input, dek, []);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		byte[]? decrypted = sut.DecryptWithDek(encrypted, dek, []);

		decrypted
			.Should()
			.NotBeNullOrEmpty();

		TextHelper.Utf8Encoding.GetString(encrypted)
			.Should()
			.NotBe(TextHelper.LoremIpsum);

		TextHelper.Utf8Encoding.GetString(decrypted)
			.Should()
			.Be(TextHelper.LoremIpsum);
	}

	/// <summary>
	/// <see cref="EncryptionService.EncryptWithSessionId" />, <see cref="EncryptionService.DecryptWithSessionId" />: a session round-trip restores the original plaintext while the ciphertext differs from it.
	/// </summary>
	[Test]
	public void EncryptWithSessionId_DecryptWithSessionId_Checking_Functionality()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		byte[] sessionId = TestUtils.CreateRandomBytes(32);

		// Act, Assert
		byte[]? encrypted = sut.EncryptWithSessionId(input, sessionId, []);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		byte[]? decrypted = sut.DecryptWithSessionId(encrypted, sessionId, []);

		decrypted
			.Should()
			.NotBeNullOrEmpty();

		TextHelper.Utf8Encoding.GetString(encrypted)
			.Should()
			.NotBe(TextHelper.LoremIpsum);

		TextHelper.Utf8Encoding.GetString(decrypted)
			.Should()
			.Be(TextHelper.LoremIpsum);
	}
	#endregion
}
