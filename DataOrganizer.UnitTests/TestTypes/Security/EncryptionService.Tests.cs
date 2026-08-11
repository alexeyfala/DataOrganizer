using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Services.Encryption;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(EncryptionService)}"" type")]
internal class EncryptionServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="EncryptionService.Decrypt" />: returns null when decrypting with a wrong password.
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

		byte[]? result = sut.Decrypt(
			encrypted,
			TextHelper.Utf8Encoding.GetBytes("WrongPassword"),
			[]);

		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EncryptionService.Decrypt" />: returns null for input produced by the session-based path.
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

		byte[]? result = sut.Decrypt(encrypted, sessionId, []);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EncryptionService.Decrypt" />: returns null on malformed input.
	/// </summary>
	[Test]
	public void Decrypt_Returns_Null_On_Malformed_Input()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] password = TextHelper
			.Utf8Encoding
			.GetBytes("SomePassword");

		// Act
		byte[]? result = sut.Decrypt([1, 2, 3], password, []);

		// Assert
		result
			.Should()
			.BeNull();
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
		sut.DecryptWithDek(encrypted, dek, TestUtils.CreateRandomBytes(37))
			.Should()
			.BeNull();

		sut.DecryptWithDek(encrypted, dek, [])
			.Should()
			.BeNull();

		sut.DecryptWithDek(encrypted, dek, associatedData)
			.Should()
			.Equal(input);
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithDek" />: returns null when decrypting with a wrong DEK.
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

		byte[]? result = sut.DecryptWithDek(encrypted, wrongDek, []);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithDek" />: returns null on malformed input.
	/// </summary>
	[Test]
	public void DecryptWithDek_Returns_Null_On_Malformed_Input()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] dek = sut.CreateRandomDek();

		// Act
		byte[]? result = sut.DecryptWithDek([1, 2, 3], dek, []);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithSessionId" />: returns null when decrypting with a wrong session identifier.
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

		byte[]? result = sut.DecryptWithSessionId(encrypted, wrongSessionId, []);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithSessionId" />: returns null for input produced by the password-based path.
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

		byte[]? result = sut.DecryptWithSessionId(encrypted, password, []);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithSessionId" />: returns null on malformed input.
	/// </summary>
	[Test]
	public void DecryptWithSessionId_Returns_Null_On_Malformed_Input()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] sessionId = TestUtils.CreateRandomBytes(32);

		// Act
		byte[]? result = sut.DecryptWithSessionId([1, 2, 3], sessionId, []);

		// Assert
		result
			.Should()
			.BeNull();
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
