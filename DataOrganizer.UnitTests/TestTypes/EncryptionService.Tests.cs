using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Helpers;
using DataOrganizer.Services;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EncryptionService)}"" type")]
internal class EncryptionServiceTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="EncryptionService.CreateRandomDek" />.
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
	/// Test of <see cref="EncryptionService.Decrypt" />.
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
	/// Test of <see cref="EncryptionService.Encrypt" />, <see cref="EncryptionService.Decrypt" />.
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
	/// Test of <see cref="EncryptionService.HashPassword" />.
	/// </summary>
	[Test]
	public void HashPassword_Returns_Hash_That_Different_From_Original_Password()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		const string password = "SomePassword";

		// Act
		string passwordHash = sut.HashPassword(password);

		// Assert
		passwordHash
			.Should()
			.NotBeNull()
			.Should()
			.NotBeSameAs(password);
	}

	/// <summary>
	/// Test of <see cref="EncryptionService.VerifyPassword" />.
	/// </summary>
	[Test]
	public void VerifyPassword_Verified_Same_Passwords_With_Different_Hashes()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		const string password = "SomePassword";

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
