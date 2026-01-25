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
	#endregion
}
