using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Services.Encryption;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EncryptionService)}"" type")]
internal class EncryptionServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="EncryptionService.CreateRandomDek" />: returns a non-empty DEK.
	/// </summary>
	[Test]
	public void CreateRandomDek_Does_Work()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		// Act
		byte[] result = sut.CreateRandomDek();

		// Assert
		result
			.Should()
			.NotBeEmpty();
	}

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
			TextHelper.Utf8Encoding.GetBytes("SomePassword"));

		encrypted
			.Should()
			.NotBeNull();

		byte[]? result = sut.Decrypt(
			encrypted,
			TextHelper.Utf8Encoding.GetBytes("WrongPassword"));

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
		byte[]? result = sut.Decrypt([1, 2, 3], password);

		// Assert
		result
			.Should()
			.BeNull();
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
		byte[]? encrypted = sut.EncryptWithDek(input, dek);

		encrypted
			.Should()
			.NotBeNull();

		byte[]? result = sut.DecryptWithDek(encrypted, wrongDek);

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
		byte[]? result = sut.DecryptWithDek([1, 2, 3], dek);

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
		byte[]? encrypted = sut.Encrypt(input, password);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		byte[]? decrypted = sut.Decrypt(encrypted, password);

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
		byte[]? encrypted = sut.EncryptWithDek(input, dek);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		byte[]? decrypted = sut.DecryptWithDek(encrypted, dek);

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
	/// <see cref="EncryptionService.HashPassword" />: returns a hash that differs from the original password.
	/// </summary>
	[Test]
	public void HashPassword_Returns_Hash_That_Different_From_Original_Password()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		const string password = "SomePassword";

		// Act
		string passwordHash = sut.HashPassword(password.ToCharArray());

		// Assert
		passwordHash
			.Should()
			.NotBeNull()
			.Should()
			.NotBeSameAs(password);
	}

	/// <summary>
	/// <see cref="EncryptionService.VerifyPassword" />: verifies the same password against two distinct hashes.
	/// </summary>
	[Test]
	public void VerifyPassword_Verified_Same_Passwords_With_Different_Hashes()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		char[] password = "SomePassword".ToCharArray();

		// Act
		string hash1 = sut.HashPassword(password);

		string hash2 = sut.HashPassword(password);

		bool result1 = sut.VerifyPassword(password, hash1);

		bool result2 = sut.VerifyPassword(password, hash2);

		// Assert
		hash1
			.Should()
			.NotBe(hash2);

		result1
			.Should()
			.BeTrue();

		result2
			.Should()
			.BeTrue();
	}
	#endregion
}
