using DataOrganizer.DTO.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using NSec.Cryptography;
using Repository.DTO;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography;

namespace DataOrganizer.Services.Encryption;

public sealed class EncryptionService : IEncryptionService
{
	#region Types
	/// <summary>
	/// Produces the AEAD key of a format from its secret, its header and the per-message salt.
	/// </summary>
	private delegate Key KeyFactory(
		ReadOnlySpan<byte> secret,
		ReadOnlySpan<byte> header,
		ReadOnlySpan<byte> salt);
	#endregion

	#region Data
	/// <summary>
	/// Salt size.
	/// </summary>
	private const int SaltSize = 16;

	/// <summary>
	/// The encryption algorithm used.
	/// </summary>
	private static readonly AeadAlgorithm _algorithm = AeadAlgorithm.XChaCha20Poly1305;

	/// <summary>
	/// The DEK-based format: the secret is the key itself, so neither a header nor a salt is needed.
	/// </summary>
	private static readonly BlobFormat _dekFormat = new(
		Version: 0x02,
		HeaderSize: 0,
		SaltSize: 0,
		_algorithm.NonceSize);

	/// <summary>
	/// The password-based format, whose header holds the cost of the key derivation.
	/// </summary>
	private static readonly BlobFormat _passwordFormat = new(
		Version: 0x01,
		Argon2Settings.HeaderSize,
		SaltSize,
		_algorithm.NonceSize);

	/// <summary>
	/// The session-based format: the derivation from a random secret has no cost to record.
	/// </summary>
	private static readonly BlobFormat _sessionFormat = new(
		Version: 0x03,
		HeaderSize: 0,
		SaltSize,
		_algorithm.NonceSize);

	/// <summary>
	/// Domain separation label for the session key derivation.
	/// </summary>
	private static readonly byte[] _sessionKeyInfo = "DataOrganizer.SessionDek.v1"u8.ToArray();
	#endregion

	#region Methods
	/// <inheritdoc />
	public byte[] CreateRandomDek() => RandomNumberGenerator.GetBytes(_algorithm.KeySize);

	/// <inheritdoc />
	public byte[] Decrypt(
		byte[] input,
		PinnedBuffer password,
		ContentIdentity identity)
	{
		ArgumentNullException.ThrowIfNull(password);

		return DecryptCore(
			input,
			password.AsReadOnlySpan(),
			identity.ToAssociatedData(),
			_passwordFormat,
			DeriveKey);
	}

	/// <inheritdoc />
	public IEnumerable<ContentsIsValidPair> DecryptContents(ContentsIsValidPair[] contents, byte[] dek)
	{
		foreach (ContentsIsValidPair item in contents)
		{
			yield return ConvertContents(item, dek, encrypt: false);
		}
	}

	/// <inheritdoc />
	public byte[] DecryptWithDek(
		byte[] input,
		byte[] dek,
		ContentIdentity identity)
	{
		return DecryptCore(
			input,
			dek,
			identity.ToAssociatedData(),
			_dekFormat,
			ImportDekAsKey);
	}

	/// <inheritdoc />
	public byte[] DecryptWithSessionId(
		byte[] input,
		byte[] sessionId,
		ContentIdentity identity)
	{
		return DecryptCore(
			input,
			sessionId,
			identity.ToAssociatedData(),
			_sessionFormat,
			DeriveSessionKey);
	}

	/// <inheritdoc />
	public byte[] Encrypt(
		byte[] input,
		PinnedBuffer password,
		ContentIdentity identity)
	{
		ArgumentNullException.ThrowIfNull(password);

		Span<byte> header = stackalloc byte[Argon2Settings.HeaderSize];

		// The cost travels with the blob, so raising it later leaves earlier blobs readable.
		Argon2Settings
			.Current
			.Write(header);

		return EncryptCore(
			input,
			password.AsReadOnlySpan(),
			identity.ToAssociatedData(),
			_passwordFormat,
			header,
			DeriveKey);
	}

	/// <inheritdoc />
	public IEnumerable<ContentsIsValidPair> EncryptContents(ContentsIsValidPair[] contents, byte[] dek)
	{
		foreach (ContentsIsValidPair item in contents)
		{
			yield return ConvertContents(item, dek, encrypt: true);
		}
	}

	/// <inheritdoc />
	public byte[] EncryptWithDek(
		byte[] input,
		byte[] dek,
		ContentIdentity identity)
	{
		return EncryptCore(
			input,
			dek,
			identity.ToAssociatedData(),
			_dekFormat,
			header: default,
			ImportDekAsKey);
	}

	/// <inheritdoc />
	public byte[] EncryptWithSessionId(
		byte[] input,
		byte[] sessionId,
		ContentIdentity identity)
	{
		return EncryptCore(
			input,
			sessionId,
			identity.ToAssociatedData(),
			_sessionFormat,
			header: default,
			DeriveSessionKey);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds the AEAD key of a format from the secret and the salt.
	/// </summary>
	/// <exception cref="CryptographicException">The secret cannot produce a key of this format.</exception>
	private static Key CreateKey(
		KeyFactory keyFactory,
		ReadOnlySpan<byte> secret,
		ReadOnlySpan<byte> header,
		ReadOnlySpan<byte> salt,
		byte version)
	{
		try
		{
			return keyFactory(secret, header, salt);
		}
		catch (Exception ex) when (ex is not CryptographicException)
		{
			throw new CryptographicException(
				$"The key material of the format {version:X2} cannot be used.", ex);
		}
	}

	/// <summary>
	/// Decrypts a blob written in <paramref name="format" />.
	/// </summary>
	private static byte[] DecryptCore(
		byte[] input,
		ReadOnlySpan<byte> secret,
		byte[] associatedData,
		BlobFormat format,
		KeyFactory keyFactory)
	{
		ArgumentNullException.ThrowIfNull(input);

		if (input.Length < format.PrefixSize + _algorithm.TagSize)
		{
			throw new CryptographicException(
				$"Encrypted data of {input.Length} bytes is too short for the format {format.Version:X2}.");
		}

		if (input[0] != format.Version)
		{
			throw new CryptographicException(
				$"Encrypted data marked {input[0]:X2} cannot be read as the format {format.Version:X2}.");
		}

		using Key key = CreateKey(
			keyFactory,
			secret,
			input.AsSpan(BlobFormat.HeaderOffset, format.HeaderSize),
			input.AsSpan(format.SaltOffset, format.SaltSize),
			format.Version);

		byte[]? plaintext;

		try
		{
			ReadOnlySpan<byte> nonce = input.AsSpan(format.NonceOffset, _algorithm.NonceSize);

			ReadOnlySpan<byte> ciphertext = input.AsSpan(format.PrefixSize);

			plaintext = OpenAead(
				key,
				nonce,
				ciphertext,
				associatedData);
		}
		catch (Exception ex) when (ex is not CryptographicException)
		{
			throw new CryptographicException($"Decryption of the format {format.Version:X2} failed.", ex);
		}

		if (plaintext is not null)
		{
			return plaintext;
		}

		// The password format is the only one where a failed tag points at the secret rather than at the data.
		throw format.Version == _passwordFormat.Version
			? new InvalidCredentialException("The password does not fit the encrypted data.")
			: new AuthenticationTagMismatchException();
	}

	/// <summary>
	/// Derives a key with the cost the blob records.
	/// </summary>
	/// <exception cref="CryptographicException">The recorded cost is not supported.</exception>
	private static Key DeriveKey(
		ReadOnlySpan<byte> password,
		ReadOnlySpan<byte> header,
		ReadOnlySpan<byte> salt)
	{
		Argon2Settings settings = Argon2Settings.Read(header);

		Argon2id kdf = PasswordBasedKeyDerivationAlgorithm.Argon2id(new()
		{
			MemorySize = settings.MemorySize,
			NumberOfPasses = settings.NumberOfPasses,
			DegreeOfParallelism = settings.DegreeOfParallelism
		});

		byte[] blob = kdf.DeriveBytes(
			password: password,
			salt: salt,
			count: _algorithm.KeySize);

		try
		{
			return ImportKey(blob);
		}
		finally
		{
			blob.ZeroMemory();
		}
	}

	/// <summary>
	/// Derives a key from a session identifier. HKDF is enough here: unlike a password,
	/// the session identifier is high-entropy random material, so a memory-hard KDF buys nothing.
	/// </summary>
	private static Key DeriveSessionKey(
		ReadOnlySpan<byte> sessionId,
		ReadOnlySpan<byte> header,
		ReadOnlySpan<byte> salt)
	{
		byte[] blob = new byte[_algorithm.KeySize];

		try
		{
			HKDF.DeriveKey(
				hashAlgorithmName: HashAlgorithmName.SHA256,
				ikm: sessionId,
				output: blob,
				salt: salt,
				info: _sessionKeyInfo);

			return ImportKey(blob);
		}
		finally
		{
			blob.ZeroMemory();
		}
	}

	/// <summary>
	/// Encrypts into a blob written in <paramref name="format" />, prefixed with <paramref name="header" />.
	/// </summary>
	private static byte[] EncryptCore(
		byte[] input,
		ReadOnlySpan<byte> secret,
		byte[] associatedData,
		BlobFormat format,
		ReadOnlySpan<byte> header,
		KeyFactory keyFactory)
	{
		ArgumentNullException.ThrowIfNull(input);

		byte[] result = new byte[format.PrefixSize + input.Length + _algorithm.TagSize];

		result[0] = format.Version;

		header.CopyTo(result.AsSpan(BlobFormat.HeaderOffset, format.HeaderSize));

		Span<byte> saltSpan = result.AsSpan(format.SaltOffset, format.SaltSize);

		RandomNumberGenerator.Fill(saltSpan);

		using Key key = CreateKey(
			keyFactory,
			secret,
			header,
			saltSpan,
			format.Version);

		Span<byte> nonceSpan = result.AsSpan(format.NonceOffset, _algorithm.NonceSize);

		RandomNumberGenerator.Fill(nonceSpan);

		try
		{
			_algorithm.Encrypt(
				key: key,
				nonce: nonceSpan,
				associatedData: associatedData,
				plaintext: input,
				ciphertext: result.AsSpan(format.PrefixSize));
		}
		catch (Exception ex) when (ex is not CryptographicException)
		{
			throw new CryptographicException($"Encryption of the format {format.Version:X2} failed.", ex);
		}

		return result;
	}

	/// <summary>
	/// Adapts <see cref="ImportKey" /> to <see cref="KeyFactory" />; the DEK format carries neither a header nor a salt.
	/// </summary>
	private static Key ImportDekAsKey(
		ReadOnlySpan<byte> dek,
		ReadOnlySpan<byte> header,
		ReadOnlySpan<byte> salt) => ImportKey(dek);

	/// <summary>
	/// Imports raw key bytes as a key for the configured AEAD algorithm.
	/// </summary>
	private static Key ImportKey(ReadOnlySpan<byte> blob)
	{
		return Key.Import(
			algorithm: _algorithm,
			blob: blob,
			format: KeyBlobFormat.RawSymmetricKey);
	}

	/// <summary>
	/// Runs AEAD authenticated decryption. Returns plaintext on success, <c>null</c> on auth failure.
	/// Defensive: returns <c>null</c> if ciphertext is shorter than the tag.
	/// </summary>
	private static byte[]? OpenAead(
		Key key,
		ReadOnlySpan<byte> nonce,
		ReadOnlySpan<byte> ciphertext,
		ReadOnlySpan<byte> associatedData)
	{
		if (ciphertext.Length < _algorithm.TagSize)
		{
			return null;
		}

		byte[] plaintext = new byte[ciphertext.Length - _algorithm.TagSize];

		return _algorithm.Decrypt(
			key: key,
			nonce: nonce,
			associatedData: associatedData,
			ciphertext: ciphertext,
			plaintext: plaintext) ? plaintext : null;
	}

	/// <summary>
	/// Converts one content with the DEK. Empty content travels as it is, and a failure marks the pair
	/// invalid instead of breaking the whole sequence.
	/// </summary>
	private ContentsIsValidPair ConvertContents(
		ContentsIsValidPair item,
		byte[] dek,
		bool encrypt)
	{
		// Empty content is stored without encryption, so there is nothing to convert.
		if (item.Contents.IsEmpty())
		{
			return item;
		}

		ContentIdentity identity = ContentIdentity.ForContents(item.Id);

		try
		{
			return new()
			{
				Contents = encrypt
					? EncryptWithDek(item.Contents, dek, identity)
					: DecryptWithDek(item.Contents, dek, identity),
				Id = item.Id,
				IsValid = true
			};
		}
		catch (Exception ex) when (EncryptionFailures.IsCryptographic(ex))
		{
			return new()
			{
				Contents = item.Contents,
				Id = item.Id,
				IsValid = false
			};
		}
	}
	#endregion
}
