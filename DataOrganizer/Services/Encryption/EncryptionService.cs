using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using NSec.Cryptography;
using Repository.DTO;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography;

namespace DataOrganizer.Services.Encryption;

public sealed class EncryptionService : IEncryptionService
{
	#region Types
	/// <summary>
	/// Produces the AEAD key of a format from its secret and the per-message salt.
	/// </summary>
	private delegate Key KeyFactory(byte[] secret, ReadOnlySpan<byte> salt);
	#endregion

	#region Data
	/// <summary>
	/// Format version for the DEK-based path: <c>[0x02][nonce][ciphertext+tag]</c>.
	/// </summary>
	private const byte FormatVersionDekV1 = 0x02;

	/// <summary>
	/// Format version for the password-based path: <c>[0x01][salt][nonce][ciphertext+tag]</c>.
	/// </summary>
	private const byte FormatVersionPasswordV1 = 0x01;

	/// <summary>
	/// Format version for the session-based path: <c>[0x03][salt][nonce][ciphertext+tag]</c>.
	/// </summary>
	private const byte FormatVersionSessionV1 = 0x03;

	/// <summary>
	/// Salt size.
	/// </summary>
	private const int SaltSize = 16;

	/// <summary>
	/// The encryption algorithm used.
	/// </summary>
	private static readonly AeadAlgorithm _algorithm = AeadAlgorithm.XChaCha20Poly1305;

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
		byte[] password,
		byte[] associatedData)
	{
		return DecryptCore(
			input,
			password,
			associatedData,
			FormatVersionPasswordV1,
			SaltSize,
			DeriveKey);
	}

	/// <inheritdoc />
	public IEnumerable<ContentsIsValidPair> DecryptContents(ContentsIsValidPair[] contents, byte[] dek)
	{
		foreach (ContentsIsValidPair item in contents)
		{
			yield return new()
			{
				Contents = DecryptWithDek(
					item.Contents,
					dek,
					ContentIdentity.ForContents(item.Id).ToAssociatedData()),
				Id = item.Id,
				IsValid = true
			};
		}
	}

	/// <inheritdoc />
	public byte[] DecryptWithDek(
		byte[] input,
		byte[] dek,
		byte[] associatedData)
	{
		return DecryptCore(
			input,
			dek,
			associatedData,
			FormatVersionDekV1,
			saltSize: 0,
			ImportDekAsKey);
	}

	/// <inheritdoc />
	public byte[] DecryptWithSessionId(
		byte[] input,
		byte[] sessionId,
		byte[] associatedData)
	{
		return DecryptCore(
			input,
			sessionId,
			associatedData,
			FormatVersionSessionV1,
			SaltSize,
			DeriveSessionKey);
	}

	/// <inheritdoc />
	public byte[] Encrypt(
		byte[] input,
		byte[] password,
		byte[] associatedData)
	{
		return EncryptCore(
			input,
			password,
			associatedData,
			FormatVersionPasswordV1,
			SaltSize,
			DeriveKey);
	}

	/// <inheritdoc />
	public IEnumerable<ContentsIsValidPair> EncryptContents(ContentsIsValidPair[] contents, byte[] dek)
	{
		foreach (ContentsIsValidPair item in contents)
		{
			yield return new()
			{
				Contents = EncryptWithDek(
					item.Contents,
					dek,
					ContentIdentity.ForContents(item.Id).ToAssociatedData()),
				Id = item.Id,
				IsValid = true
			};
		}
	}

	/// <inheritdoc />
	public byte[] EncryptWithDek(
		byte[] input,
		byte[] dek,
		byte[] associatedData)
	{
		return EncryptCore(
			input,
			dek,
			associatedData,
			FormatVersionDekV1,
			saltSize: 0,
			ImportDekAsKey);
	}

	/// <inheritdoc />
	public byte[] EncryptWithSessionId(
		byte[] input,
		byte[] sessionId,
		byte[] associatedData)
	{
		return EncryptCore(
			input,
			sessionId,
			associatedData,
			FormatVersionSessionV1,
			SaltSize,
			DeriveSessionKey);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Decrypts a blob laid out as <c>[version][salt][nonce][ciphertext+tag]</c>.
	/// A zero <paramref name="saltSize" /> means the format carries no salt.
	/// </summary>
	private static byte[] DecryptCore(
		byte[] input,
		byte[] secret,
		byte[] associatedData,
		byte version,
		int saltSize,
		KeyFactory keyFactory)
	{
		ArgumentNullException.ThrowIfNull(input);

		// Guard: enough bytes for [version][salt][nonce][tag] and the version byte must match.
		if (input.Length < 1 + saltSize + _algorithm.NonceSize + _algorithm.TagSize || input[0] != version)
		{
			//return null;
			throw new CryptographicException(
				$"Encrypted data of {input.Length} bytes marked {input[0]:X2} cannot be read as the format {version:X2}.");
		}

		byte[]? plaintext;

		try
		{
			ReadOnlySpan<byte> salt = input.AsSpan(1, saltSize);

			using Key key = keyFactory(secret, salt);

			ReadOnlySpan<byte> nonce = input.AsSpan(1 + saltSize, _algorithm.NonceSize);

			ReadOnlySpan<byte> ciphertext = input.AsSpan(1 + saltSize + _algorithm.NonceSize);

			plaintext = OpenAead(key, nonce, ciphertext, associatedData);
		}
		catch (Exception ex)
		{
			throw new CryptographicException($"Decryption of the format {version:X2} failed.", ex);
		}

		if (plaintext is not null)
		{
			return plaintext;
		}

		// The password format is the only one where a failed tag points at the secret rather than at the data.
		throw version == FormatVersionPasswordV1
			? new InvalidCredentialException("The password does not fit the encrypted data.")
			: new AuthenticationTagMismatchException();
	}

	/// <summary>
	/// Derives a key.
	/// </summary>
	private static Key DeriveKey(byte[] password, ReadOnlySpan<byte> salt)
	{
		Argon2id kdf = PasswordBasedKeyDerivationAlgorithm.Argon2id(new()
		{
			MemorySize = 65536,
			NumberOfPasses = 3,
			DegreeOfParallelism = 1
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
	private static Key DeriveSessionKey(byte[] sessionId, ReadOnlySpan<byte> salt)
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
	/// Encrypts into a blob laid out as <c>[version][salt][nonce][ciphertext+tag]</c>.
	/// A zero <paramref name="saltSize" /> means the format carries no salt.
	/// </summary>
	private static byte[] EncryptCore(
		byte[] input,
		byte[] secret,
		byte[] associatedData,
		byte version,
		int saltSize,
		KeyFactory keyFactory)
	{
		try
		{
			int nonceSize = _algorithm.NonceSize;

			byte[] result = new byte[1 + saltSize + nonceSize + input.Length + _algorithm.TagSize];

			result[0] = version;

			Span<byte> saltSpan = result.AsSpan(1, saltSize);

			RandomNumberGenerator.Fill(saltSpan);

			using Key key = keyFactory(secret, saltSpan);

			Span<byte> nonceSpan = result.AsSpan(1 + saltSize, nonceSize);

			RandomNumberGenerator.Fill(nonceSpan);

			_algorithm.Encrypt(
				key: key,
				nonce: nonceSpan,
				associatedData: associatedData,
				plaintext: input,
				ciphertext: result.AsSpan(1 + saltSize + nonceSize));

			return result;
		}
		catch (Exception ex)
		{
			throw new CryptographicException($"Encryption of the format {version:X2} failed.", ex);
		}
	}

	/// <summary>
	/// Adapts <see cref="ImportKey" /> to <see cref="KeyFactory" />; the DEK format carries no salt.
	/// </summary>
	private static Key ImportDekAsKey(byte[] dek, ReadOnlySpan<byte> _) => ImportKey(dek);

	/// <summary>
	/// Imports raw key bytes as a key for the configured AEAD algorithm.
	/// </summary>
	private static Key ImportKey(byte[] blob)
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
	#endregion
}
