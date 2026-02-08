using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Helpers;
using DataOrganizer.Services;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EncryptionService)}"" type")]
internal class EncryptionServiceTests
{
	#region Methods
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

		// Act
		sut.Encrypt(
			input,
			TextHelper.Utf8Encoding.GetBytes("SomePassword"),
			out byte[] encrypted);

		bool isDecrypted = sut.Decrypt(
			encrypted,
			TextHelper.Utf8Encoding.GetBytes("WrongPassword"),
			out byte[] decrypted);

		// Assert
		isDecrypted
			.Should()
			.BeFalse();
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

		// Act
		bool isEncrypted = sut.Encrypt(
			input,
			password,
			out byte[] encrypted);

		bool isDecrypted = sut.Decrypt(
			encrypted,
			password,
			out byte[] decrypted);

		// Assert
		isEncrypted
			.Should()
			.BeTrue();

		isDecrypted
			.Should()
			.BeTrue();

		TextHelper.Utf8Encoding.GetString(encrypted)
			.Should()
			.NotBe(TextHelper.LoremIpsum);

		TextHelper.Utf8Encoding.GetString(decrypted)
			.Should()
			.Be(TextHelper.LoremIpsum);
	}

	/// <summary>
	/// Test of <see cref="EncryptionService.EnhancedHashPassword" />.
	/// </summary>
	[Test]
	public void EnhancedHashPassword_Returns_Hash_That_Different_From_Original_Password()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		const string password = "SomePassword";

		// Act
		string passwordHash = sut.EnhancedHashPassword(password);

		// Assert
		passwordHash
			.Should()
			.NotBeNull()
			.Should()
			.NotBeSameAs(password);
	}

	/// <summary>
	/// Test of <see cref="EncryptionService.EnhancedVerify" />.
	/// </summary>
	[Test]
	public void EnhancedVerify_Verified_Same_Passwords_With_Different_Hashes()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		const string password = "SomePassword";

		// Act
		string hash1 = sut.EnhancedHashPassword(password);

		string hash2 = sut.EnhancedHashPassword(password);

		bool result1 = sut.EnhancedVerify(password, hash1);

		bool result2 = sut.EnhancedVerify(password, hash2);

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

	/// <summary>
	/// Test of <see cref="EncryptionService.GetSessionId" />.
	/// </summary>
	[Test]
	public async Task GetSessionId_Returns_Same_Value_Every_Time()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		// Act
		byte[] value = sut.GetSessionId();

		// Assert
		for (int i = 0; i < 10; i++)
		{
			await Task
				.Delay(100)
				.ConfigureAwait(false);

			sut
				.GetSessionId()
				.Should()
				.BeEquivalentTo(value);
		}
	}
	#endregion
}
