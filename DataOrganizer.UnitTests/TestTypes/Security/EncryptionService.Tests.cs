using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Services.Encryption;
using NSec.Cryptography;
using Repository.DTO;
using System;
using System.Buffers.Binary;
using System.Security.Authentication;
using System.Security.Cryptography;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(EncryptionService)}"" type")]
internal class EncryptionServiceTests
{
	#region Data
	/// <summary>
	/// Purpose every round-trip of the fixture is bound to.
	/// </summary>
	private static readonly ContentIdentity _identity = ContentIdentity.ForContents(Guid.NewGuid());
	#endregion

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
		using PinnedBuffer password = new(TextHelper.Utf8Encoding.GetBytes("SomePassword"));

		using PinnedBuffer wrongPassword = new(TextHelper.Utf8Encoding.GetBytes("WrongPassword"));

		byte[]? encrypted = sut.Encrypt(
			input,
			password,
			_identity);

		encrypted
			.Should()
			.NotBeNull();

		Action act = () => sut.Decrypt(
			encrypted,
			wrongPassword,
			_identity);

		act
			.Should()
			.ThrowExactly<InvalidCredentialException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.Decrypt" />: a blob carries the cost it was written with,
	/// so a cost other than the current one still opens.
	/// </summary>
	[Test]
	public void Decrypt_Opens_A_Blob_Written_With_Another_Cost()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		using PinnedBuffer password = new(TextHelper.Utf8Encoding.GetBytes("SomePassword"));

		Argon2Settings settings = new(
			MemorySize: 8192,
			NumberOfPasses: 1,
			DegreeOfParallelism: 1);

		settings
			.Should()
			.NotBe(Argon2Settings.Current);

		// Act
		byte[] encrypted = WriteWithCost(input, password, settings);

		// Assert
		sut.Decrypt(encrypted, password, _identity)
			.Should()
			.Equal(input);
	}

	/// <summary>
	/// <see cref="EncryptionService.Decrypt" />: a cost outside the supported range is refused
	/// before it can steer an allocation.
	/// </summary>
	[Test]
	public void Decrypt_Rejects_An_Unsupported_Derivation_Cost()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		using PinnedBuffer password = new(TextHelper.Utf8Encoding.GetBytes("SomePassword"));

		byte[]? encrypted = sut.Encrypt(
			input,
			password,
			_identity);

		encrypted
			.Should()
			.NotBeNull();

		// Act
		BinaryPrimitives.WriteUInt32LittleEndian(encrypted.AsSpan(1), uint.MaxValue);

		Action act = () => sut.Decrypt(
			encrypted,
			password,
			_identity);

		// Assert
		act
			.Should()
			.ThrowExactly<CryptographicException>();
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
		byte[]? encrypted = sut.EncryptWithSessionId(input, sessionId, _identity);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		using PinnedBuffer secret = new(sessionId);

		Action act = () => sut.Decrypt(encrypted, secret, _identity);

		// Assert
		act
			.Should()
			.ThrowExactly<CryptographicException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.Decrypt" />: malformed input is refused as damaged data,
	/// an empty one included.
	/// </summary>
	[Test]
	[TestCase(new byte[] { 1, 2, 3 })]
	[TestCase(new byte[0])]
	public void Decrypt_Throws_On_Malformed_Input(byte[] input)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		using PinnedBuffer password = new(TextHelper.Utf8Encoding.GetBytes("SomePassword"));

		// Act
		Action act = () => sut.Decrypt(input, password, _identity);

		// Assert
		act
			.Should()
			.ThrowExactly<CryptographicException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptContents" />: empty content comes back untouched, so a file
	/// stored without encryption does not block the conversion of a folder.
	/// </summary>
	[Test]
	public void DecryptContents_Hands_Empty_Contents_Back()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] dek = sut.CreateRandomDek();

		ContentsIsValidPair[] contents =
		[
			new()
			{
				Contents = [],
				Id = Guid.NewGuid(),
				IsValid = true
			}
		];

		// Act
		ContentsIsValidPair[] result = [.. sut.DecryptContents(contents, dek)];

		// Assert
		result
			.Should()
			.HaveCount(1);

		result[0]
			.IsValid
			.Should()
			.BeTrue();

		result[0]
			.Contents
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptContents" />: a content that cannot be opened is marked invalid
	/// and the rest of the sequence is still converted.
	/// </summary>
	[Test]
	public void DecryptContents_Marks_A_Damaged_Content_Invalid()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] dek = sut.CreateRandomDek();

		Guid openableId = Guid.NewGuid();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		byte[] damaged = TestUtils.CreateRandomBytes(50);

		ContentsIsValidPair[] contents =
		[
			new()
			{
				Contents = sut.EncryptWithDek(input, dek, ContentIdentity.ForContents(openableId)),
				Id = openableId,
				IsValid = true
			},
			new()
			{
				Contents = damaged,
				Id = Guid.NewGuid(),
				IsValid = true
			}
		];

		// Act
		ContentsIsValidPair[] result = [.. sut.DecryptContents(contents, dek)];

		// Assert
		result[0]
			.IsValid
			.Should()
			.BeTrue();

		TextHelper.Utf8Encoding.GetString(result[0].Contents)
			.Should()
			.Be(TextHelper.LoremIpsum);

		result[1]
			.IsValid
			.Should()
			.BeFalse();

		result[1]
			.Contents
			.Should()
			.BeSameAs(damaged);
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithDek" />: the purpose is authenticated, so a ciphertext
	/// of one purpose does not open as another even under the same key.
	/// </summary>
	[Test]
	public void DecryptWithDek_Cannot_Decrypt_With_Another_Purpose()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		byte[] dek = sut.CreateRandomDek();

		Guid id = Guid.NewGuid();

		// Act
		byte[]? encrypted = sut.EncryptWithDek(input, dek, ContentIdentity.ForContents(id));

		encrypted
			.Should()
			.NotBeNull();

		// Assert
		Action asNote = () => sut.DecryptWithDek(encrypted, dek, ContentIdentity.ForNote(id));

		asNote
			.Should()
			.ThrowExactly<AuthenticationTagMismatchException>();

		Action asDek = () => sut.DecryptWithDek(encrypted, dek, ContentIdentity.ForDek(id));

		asDek
			.Should()
			.ThrowExactly<AuthenticationTagMismatchException>();

		sut.DecryptWithDek(encrypted, dek, ContentIdentity.ForContents(id))
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
		byte[]? encrypted = sut.EncryptWithDek(input, dek, _identity);

		encrypted
			.Should()
			.NotBeNull();

		Action act = () => sut.DecryptWithDek(encrypted, wrongDek, _identity);

		// Assert
		act
			.Should()
			.ThrowExactly<AuthenticationTagMismatchException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithDek" />: the identifier is not authenticated, so contents
	/// of one object open as contents of another under the same key.
	/// </summary>
	/// <remarks>
	/// A known limitation of the current blob format, kept here so a change of it is noticed.
	/// </remarks>
	[Test]
	public void DecryptWithDek_Cannot_Tell_Two_Objects_Of_One_Purpose_Apart()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		byte[] dek = sut.CreateRandomDek();

		// Act
		byte[] encrypted = sut.EncryptWithDek(input, dek, ContentIdentity.ForContents(Guid.NewGuid()));

		// Assert
		sut.DecryptWithDek(encrypted, dek, ContentIdentity.ForContents(Guid.NewGuid()))
			.Should()
			.Equal(input);
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithDek" />: a DEK of the wrong size is reported as unusable
	/// key material, not as damaged data, and the real cause is kept.
	/// </summary>
	[Test]
	public void DecryptWithDek_Reports_Unusable_Key_Material()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		byte[] encrypted = sut.EncryptWithDek(input, sut.CreateRandomDek(), _identity);

		// Act
		Action act = () => sut.DecryptWithDek(encrypted, TestUtils.CreateRandomBytes(16), _identity);

		// Assert
		act
			.Should()
			.ThrowExactly<CryptographicException>()
			.WithMessage("The key material*")
			.And
			.InnerException
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithDek" />: malformed input is refused as damaged data,
	/// an empty one included.
	/// </summary>
	[Test]
	[TestCase(new byte[] { 1, 2, 3 })]
	[TestCase(new byte[0])]
	public void DecryptWithDek_Throws_On_Malformed_Input(byte[] input)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] dek = sut.CreateRandomDek();

		// Act
		Action act = () => sut.DecryptWithDek(input, dek, _identity);

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
		byte[]? encrypted = sut.EncryptWithSessionId(input, sessionId, _identity);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		Action act = () => sut.DecryptWithSessionId(encrypted, wrongSessionId, _identity);

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

		using PinnedBuffer password = new(TextHelper.Utf8Encoding.GetBytes("SomePassword"));

		// Act
		byte[]? encrypted = sut.Encrypt(input, password, _identity);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		Action act = () => sut.DecryptWithSessionId(encrypted, password.AsReadOnlySpan().ToArray(), _identity);

		// Assert
		act
			.Should()
			.ThrowExactly<CryptographicException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.DecryptWithSessionId" />: malformed input is refused as damaged data,
	/// an empty one included.
	/// </summary>
	[Test]
	[TestCase(new byte[] { 1, 2, 3 })]
	[TestCase(new byte[0])]
	public void DecryptWithSessionId_Throws_On_Malformed_Input(byte[] input)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] sessionId = TestUtils.CreateRandomBytes(32);

		// Act
		Action act = () => sut.DecryptWithSessionId(input, sessionId, _identity);

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

		using PinnedBuffer password = new(TextHelper.Utf8Encoding.GetBytes("SomePassword"));

		// Act, Assert
		byte[]? encrypted = sut.Encrypt(input, password, _identity);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		byte[]? decrypted = sut.Decrypt(encrypted, password, _identity);

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
	/// <see cref="EncryptionService.Encrypt" />: the cost of the derivation is written into the blob.
	/// </summary>
	[Test]
	public void Encrypt_Records_The_Derivation_Cost()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		using PinnedBuffer password = new(TextHelper.Utf8Encoding.GetBytes("SomePassword"));

		// Act
		byte[]? encrypted = sut.Encrypt(
			input,
			password,
			_identity);

		encrypted
			.Should()
			.NotBeNull();

		// Assert
		Argon2Settings
			.Read(encrypted.AsSpan(1, Argon2Settings.HeaderSize))
			.Should()
			.Be(Argon2Settings.Current);
	}

	/// <summary>
	/// <see cref="EncryptionService.Encrypt" />: an absent input is a caller mistake and is reported as such.
	/// </summary>
	[Test]
	public void Encrypt_Throws_On_Null_Input()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		using PinnedBuffer password = new(TextHelper.Utf8Encoding.GetBytes("SomePassword"));

		// Act
		Action act = () => sut.Encrypt(null!, password, _identity);

		// Assert
		act
			.Should()
			.ThrowExactly<ArgumentNullException>();
	}

	/// <summary>
	/// <see cref="EncryptionService.EncryptContents" />, <see cref="EncryptionService.DecryptContents" />:
	/// an empty content stays unencrypted and survives a folder round-trip next to a normal one.
	/// </summary>
	[Test]
	public void EncryptContents_DecryptContents_Checking_Functionality()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] dek = sut.CreateRandomDek();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		ContentsIsValidPair[] contents =
		[
			new()
			{
				Contents = [],
				Id = Guid.NewGuid(),
				IsValid = true
			},
			new()
			{
				Contents = input,
				Id = Guid.NewGuid(),
				IsValid = true
			}
		];

		// Act
		ContentsIsValidPair[] encrypted = [.. sut.EncryptContents(contents, dek)];

		ContentsIsValidPair[] decrypted = [.. sut.DecryptContents(encrypted, dek)];

		// Assert
		encrypted[0]
			.Contents
			.Should()
			.BeEmpty();

		TextHelper.Utf8Encoding.GetString(encrypted[1].Contents)
			.Should()
			.NotBe(TextHelper.LoremIpsum);

		decrypted
			.Should()
			.OnlyContain(x => x.IsValid);

		decrypted[0]
			.Contents
			.Should()
			.BeEmpty();

		TextHelper.Utf8Encoding.GetString(decrypted[1].Contents)
			.Should()
			.Be(TextHelper.LoremIpsum);
	}

	/// <summary>
	/// Every path keeps its own version byte and its own on-the-wire layout: the DEK one carries
	/// no salt, the other two do, and the password one carries the cost of the derivation as well.
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

		using PinnedBuffer secretBuffer = new(secret);

		// Act
		byte[]? password = sut.Encrypt(input, secretBuffer, _identity);

		byte[]? dek = sut.EncryptWithDek(input, sut.CreateRandomDek(), _identity);

		byte[]? session = sut.EncryptWithSessionId(input, secret, _identity);

		// Assert
		password
			.Should()
			.NotBeNull()
			.And
			.HaveElementAt(0, 0x01)
			.And
			.HaveCount(1 + Argon2Settings.HeaderSize + SaltSize + NonceSize + input.Length + TagSize);

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
		byte[]? encrypted = sut.EncryptWithDek(input, dek, _identity);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		byte[]? decrypted = sut.DecryptWithDek(encrypted, dek, _identity);

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
	/// <see cref="EncryptionService.EncryptWithDek" />: a DEK of the wrong size is reported as unusable
	/// key material, not as a failed encryption, and the real cause is kept.
	/// </summary>
	[Test]
	public void EncryptWithDek_Reports_Unusable_Key_Material()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EncryptionService sut = mock.Create<EncryptionService>();

		byte[] input = TextHelper
			.Utf8Encoding
			.GetBytes(TextHelper.LoremIpsum);

		// Act
		Action act = () => sut.EncryptWithDek(input, TestUtils.CreateRandomBytes(16), _identity);

		// Assert
		act
			.Should()
			.ThrowExactly<CryptographicException>()
			.WithMessage("The key material*")
			.And
			.InnerException
			.Should()
			.NotBeNull();
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
		byte[]? encrypted = sut.EncryptWithSessionId(input, sessionId, _identity);

		encrypted
			.Should()
			.NotBeNullOrEmpty();

		byte[]? decrypted = sut.DecryptWithSessionId(encrypted, sessionId, _identity);

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

	#region Helpers
	/// <summary>
	/// Writes a password based blob with the given derivation cost, following the layout of the format.
	/// </summary>
	private static byte[] WriteWithCost(
		byte[] input,
		PinnedBuffer password,
		Argon2Settings settings)
	{
		const int SaltSize = 16;

		AeadAlgorithm algorithm = AeadAlgorithm.XChaCha20Poly1305;

		const int saltOffset = 1 + Argon2Settings.HeaderSize;

		int nonceOffset = saltOffset + SaltSize;

		int prefixSize = nonceOffset + algorithm.NonceSize;

		byte[] result = new byte[prefixSize + input.Length + algorithm.TagSize];

		result[0] = 0x01;

		settings.Write(result.AsSpan(1, Argon2Settings.HeaderSize));

		Span<byte> salt = result.AsSpan(saltOffset, SaltSize);

		RandomNumberGenerator.Fill(salt);

		Span<byte> nonce = result.AsSpan(nonceOffset, algorithm.NonceSize);

		RandomNumberGenerator.Fill(nonce);

		Argon2id kdf = PasswordBasedKeyDerivationAlgorithm.Argon2id(new()
		{
			MemorySize = settings.MemorySize,
			NumberOfPasses = settings.NumberOfPasses,
			DegreeOfParallelism = settings.DegreeOfParallelism
		});

		byte[] blob = kdf.DeriveBytes(
			password: password.AsReadOnlySpan(),
			salt: salt,
			count: algorithm.KeySize);

		using Key key = Key.Import(
			algorithm: algorithm,
			blob: blob,
			format: KeyBlobFormat.RawSymmetricKey);

		algorithm.Encrypt(
			key: key,
			nonce: nonce,
			associatedData: _identity.ToAssociatedData(),
			plaintext: input,
			ciphertext: result.AsSpan(prefixSize));

		return result;
	}
	#endregion
}
