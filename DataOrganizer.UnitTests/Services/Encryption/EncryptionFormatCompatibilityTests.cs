using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Services.Encryption;
using DataOrganizer.UnitTests.Factories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UnitTests.Services.Encryption;

[TestFixture(Description = "Guards that blobs written by an earlier build still open, and that nothing in them can be altered unnoticed")]
internal class EncryptionFormatCompatibilityTests
{
	#region Data
	/// <summary>
	/// Plaintext of <see cref="DekBlob" />.
	/// </summary>
	private const string Contents = "Golden contents";

	/// <summary>
	/// The decomposed spelling of the password <see cref="NormalizedPasswordBlob" /> was written under;
	/// the two spellings differ in bytes and agree only once normalized.
	/// </summary>
	private const string DecomposedPassword = "Pässwörd";

	/// <summary>
	/// A blob of the format 0x02, written under <see cref="Secret" /> for <see cref="ContentPurpose.Contents" />.
	/// </summary>
	private const string DekBlob =
		"02DDDEA261D4C6FCAA57CA7E6E88DFBA51405294D6E67049672B4BC1E767B7B63ABAA99698F3BB4499CDA904F3973A222643B7BD3624FAC5";

	/// <summary>
	/// Plaintext of <see cref="PasswordBlob" /> and of <see cref="SessionBlob" />.
	/// </summary>
	private const string KeyMaterial = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F";

	/// <summary>
	/// A blob of the format 0x01, written for <see cref="ContentPurpose.Dek" /> under the composed
	/// spelling of <see cref="DecomposedPassword" />, at the lowest cost the derivation supports.
	/// </summary>
	private const string NormalizedPasswordBlob =
		"0100200000010195AA506E8FB5E801E503B5D72AB2D8252ADE6D4E0977A602DDE31B6F9BE6B929961183D0253F21E24BD5777BFE5BE1990ABCFFC303783F06C2DA7A38D9FEC5A3747AC086958789018B55E0002054F6C7DE3A83B81F69336B070C847698842C16B0A48876146CB59F";

	/// <summary>
	/// Password the blob of the format 0x01 was written with.
	/// </summary>
	private const string Password = "GoldenPassword";

	/// <summary>
	/// A blob of the format 0x01, written for <see cref="ContentPurpose.Dek" /> at the lowest cost
	/// the derivation supports, so opening it stays cheap.
	/// </summary>
	private const string PasswordBlob =
		"0100200000010168F35632C1A05E64787064AC9FA1A449287724E542F8F55D00047EEB4FCF0DA6EAEDE5E1A88BDFF0B1AD2C4B0D8C707D3B7E747DE0DA0DC0873A8606947C4104B149DB8FDAB49322EBE9A72EA154576EE1959A1D281515611F8A53F22349FD204DF8F7DC7456C3B3";

	/// <summary>
	/// Secret <see cref="DekBlob" /> was written under.
	/// </summary>
	private const string Secret = "404142434445464748494A4B4C4D4E4F505152535455565758595A5B5C5D5E5F";

	/// <summary>
	/// A blob of the format 0x03, written under <see cref="SessionSecret" /> for <see cref="ContentPurpose.Dek" />.
	/// </summary>
	private const string SessionBlob =
		"032122EEF3251CFDEFF17EAF23A6638DFD5258B9186A71ABCD2178F6B1E2CDDEE9EAEE3B659460E83EE656B17ED4C2903FDEC8B2A45E588764CA62D07AF01C9F5220D083C9C124555B81BDDE0FBFB1CE89F636DBDB39EE120C";

	/// <summary>
	/// Secret <see cref="SessionBlob" /> was written under.
	/// </summary>
	private const string SessionSecret = "808182838485868788898A8B8C8D8E8F909192939495969798999A9B9C9D9E9F";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="EncryptionService.Decrypt" />: a blob of the format 0x01 written before this build
	/// still yields its key, so the layout, the cost header and the associated data are all unchanged.
	/// </summary>
	[Test]
	public void Decrypt_Opens_A_Recorded_Blob_Of_The_Password_Format()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		using PinnedBuffer password = SecretFactory.CreatePassword(Password);

		// Act
		using PinnedBuffer plaintext = sut.Decrypt(
			Convert.FromHexString(PasswordBlob),
			password,
			ContentIdentity.Dek);

		// Assert
		plaintext
			.AsReadOnlySpan()
			.ToArray()
			.Should()
			.Equal(Convert.FromHexString(KeyMaterial));
	}

	/// <summary>
	/// <see cref="EncryptionService.Decrypt" />: a blob written under the composed spelling of a
	/// password opens under the decomposed one, so the secret still reaches the derivation normalized.
	/// </summary>
	[Test]
	public void Decrypt_Opens_A_Recorded_Blob_Written_With_A_Normalized_Password()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		using PinnedSecret secret = SecretFactory.CreateSecret(DecomposedPassword);

		using PinnedBuffer password = secret.ToUtf8Buffer();

		// Act
		using PinnedBuffer plaintext = sut.Decrypt(
			Convert.FromHexString(NormalizedPasswordBlob),
			password,
			ContentIdentity.Dek);

		// Assert
		plaintext
			.AsReadOnlySpan()
			.ToArray()
			.Should()
			.Equal(Convert.FromHexString(KeyMaterial));
	}

	/// <summary>
	/// <see cref="EncryptionService.Decrypt" />: the format 0x01 leaves no region a flipped bit
	/// can pass through, the recorded cost and the check value included.
	/// </summary>
	[Test]
	public void Decrypt_Rejects_A_Flip_In_Every_Region_Of_The_Password_Format()
	{
		// Arrange
		const int checkSize = 16;

		const int nonceSize = 24;

		const int saltSize = 16;

		const int saltOffset = 1 + Argon2Settings.HeaderSize;

		const int checkOffset = saltOffset + saltSize;

		const int nonceOffset = checkOffset + checkSize;

		const int cipherOffset = nonceOffset + nonceSize;

		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		using PinnedBuffer password = SecretFactory.CreatePassword(Password);

		byte[] blob = Convert.FromHexString(PasswordBlob);

		// The header is hit on the lowest bit of the memory size, which keeps the derivation cheap.
		int[] offsets = [0, 1, saltOffset, checkOffset, nonceOffset, cipherOffset, blob.Length - 1];

		// Act
		List<int> accepted = FindAcceptedFlips(
			blob,
			offsets,
			tampered => sut.Decrypt(tampered, password, ContentIdentity.Dek));

		// Assert
		accepted
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithDek" />: a blob of the format 0x02 written before this
	/// build still yields its contents.
	/// </summary>
	[Test]
	public void DecryptWithDek_Opens_A_Recorded_Blob_Of_The_Dek_Format()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		using PinnedBuffer secret = new(Convert.FromHexString(Secret));

		// Act
		byte[] contents = sut.DecryptWithDek(
			Convert.FromHexString(DekBlob),
			secret,
			ContentIdentity.Contents);

		// Assert
		TextDefaults.Encoding
			.GetString(contents)
			.Should()
			.Be(Contents);
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithDek" />: the format 0x02 carries no byte outside its
	/// authentication, the version and the nonce included.
	/// </summary>
	[Test]
	public void DecryptWithDek_Rejects_A_Flip_Of_Any_Byte()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		using PinnedBuffer secret = new(Convert.FromHexString(Secret));

		byte[] blob = Convert.FromHexString(DekBlob);

		// Act
		List<int> accepted = FindAcceptedFlips(
			blob,
			Enumerable.Range(0, blob.Length),
			tampered => sut.DecryptWithDek(tampered, secret, ContentIdentity.Contents));

		// Assert
		accepted
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithSessionId" />: a blob of the format 0x03 written before
	/// this build still yields its key, so the salt, the nonce and the derivation label are unchanged.
	/// </summary>
	[Test]
	public void DecryptWithSessionId_Opens_A_Recorded_Blob_Of_The_Session_Format()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		using PinnedBuffer sessionId = new(Convert.FromHexString(SessionSecret));

		// Act
		using PinnedBuffer plaintext = sut.DecryptWithSessionId(
			Convert.FromHexString(SessionBlob),
			sessionId,
			ContentIdentity.Dek);

		// Assert
		plaintext
			.AsReadOnlySpan()
			.ToArray()
			.Should()
			.Equal(Convert.FromHexString(KeyMaterial));
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithSessionId" />: the format 0x03 carries no byte outside
	/// its authentication, although it proves nothing before the tag.
	/// </summary>
	[Test]
	public void DecryptWithSessionId_Rejects_A_Flip_Of_Any_Byte()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		using PinnedBuffer sessionId = new(Convert.FromHexString(SessionSecret));

		byte[] blob = Convert.FromHexString(SessionBlob);

		// Act
		List<int> accepted = FindAcceptedFlips(
			blob,
			Enumerable.Range(0, blob.Length),
			tampered => sut.DecryptWithSessionId(tampered, sessionId, ContentIdentity.Dek));

		// Assert
		accepted
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ContentIdentity.ToAssociatedData" />: the label and the numbering of the purposes are
	/// what every stored blob was authenticated with, so neither may drift.
	/// </summary>
	[TestCase(ContentPurpose.Contents, "446174614F7267616E697A65722E4161642E763101")]
	[TestCase(ContentPurpose.Note, "446174614F7267616E697A65722E4161642E763102")]
	[TestCase(ContentPurpose.Dek, "446174614F7267616E697A65722E4161642E763103")]
	[TestCase(ContentPurpose.ClipboardDek, "446174614F7267616E697A65722E4161642E763104")]
	[TestCase(ContentPurpose.ClipboardLog, "446174614F7267616E697A65722E4161642E763105")]
	public void ToAssociatedData_Keeps_The_Recorded_Bytes(ContentPurpose purpose, string expected)
	{
		// Arrange
		ContentIdentity identity = new(purpose);

		// Act
		byte[] associatedData = identity.ToAssociatedData();

		// Assert
		Convert
			.ToHexString(associatedData)
			.Should()
			.Be(expected);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Offsets at which a flipped bit left the blob readable, or failed in an unexpected way.
	/// </summary>
	private static List<int> FindAcceptedFlips(byte[] blob, IEnumerable<int> offsets, Action<byte[]> open)
	{
		List<int> result = [];

		foreach (int offset in offsets)
		{
			byte[] tampered = [.. blob];

			tampered[offset] ^= 0x01;

			if (!IsRejected(() => open(tampered)))
			{
				result.Add(offset);
			}
		}

		return result;
	}

	/// <summary>
	/// <c>True</c> when opening a blob failed the way damaged data is expected to fail.
	/// </summary>
	private static bool IsRejected(Action open)
	{
		try
		{
			open();

			return false;
		}
		catch (Exception ex)
		{
			return EncryptionFailures.IsCryptographic(ex);
		}
	}
	#endregion
}
