using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using NSec.Cryptography;
using Repository.DTO;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using BC = BCrypt.Net.BCrypt;

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

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public EncryptionService(ILogger logger) => _logger = logger;
	#endregion

	#region Methods
	/// <inheritdoc />
	public byte[] CreateRandomDek() => RandomNumberGenerator.GetBytes(_algorithm.KeySize);

	/// <inheritdoc />
	public byte[]? Decrypt(byte[] input, byte[] password)
	{
		return DecryptCore(
			input,
			password,
			FormatVersionPasswordV1,
			SaltSize,
			DeriveKey);
	}

	/// <inheritdoc />
	public IEnumerable<ContentsIsValidPair> DecryptContents(ContentsIsValidPair[] contents, byte[] dek)
	{
		foreach (ContentsIsValidPair item in contents)
		{
			if (DecryptWithDek(item.Contents, dek) is { } output)
			{
				yield return new()
				{
					Contents = output,
					Id = item.Id,
					IsValid = true
				};
			}
			else
			{
				yield break;
			}
		}
	}

	/// <inheritdoc />
	public byte[]? DecryptWithDek(byte[] input, byte[] dek)
	{
		return DecryptCore(
			input,
			dek,
			FormatVersionDekV1,
			saltSize: 0,
			ImportDekAsKey);
	}

	/// <inheritdoc />
	public byte[]? DecryptWithSessionId(byte[] input, byte[] sessionId)
	{
		return DecryptCore(
			input,
			sessionId,
			FormatVersionSessionV1,
			SaltSize,
			DeriveSessionKey);
	}

	/// <inheritdoc />
	public byte[]? Encrypt(byte[] input, byte[] password)
	{
		return EncryptCore(
			input,
			password,
			FormatVersionPasswordV1,
			SaltSize,
			DeriveKey);
	}

	/// <inheritdoc />
	public IEnumerable<ContentsIsValidPair> EncryptContents(ContentsIsValidPair[] contents, byte[] dek)
	{
		foreach (ContentsIsValidPair item in contents)
		{
			if (EncryptWithDek(item.Contents, dek) is { } output)
			{
				yield return new()
				{
					Contents = output,
					Id = item.Id,
					IsValid = true
				};
			}
			else
			{
				yield break;
			}
		}
	}

	/// <inheritdoc />
	public byte[]? EncryptWithDek(byte[] input, byte[] dek)
	{
		return EncryptCore(
			input,
			dek,
			FormatVersionDekV1,
			saltSize: 0,
			ImportDekAsKey);
	}

	/// <inheritdoc />
	public byte[]? EncryptWithSessionId(byte[] input, byte[] sessionId)
	{
		return EncryptCore(
			input,
			sessionId,
			FormatVersionSessionV1,
			SaltSize,
			DeriveSessionKey);
	}

	/// <inheritdoc />
	public string HashPassword(char[] password)
	{
		string temp = new(password);

		try
		{
			return BC.EnhancedHashPassword(temp);
		}
		finally
		{
			SecureStringHelper.WipeString(temp);
		}
	}

	/// <inheritdoc />
	public byte[]? RewrapDek(
		byte[] wrappedDek,
		byte[] oldPassword,
		byte[] newPassword)
	{
		if (Decrypt(wrappedDek, oldPassword) is not { } dek)
		{
			return null;
		}

		try
		{
			return Encrypt(dek, newPassword);
		}
		finally
		{
			dek.ZeroMemory();
		}
	}

	/// <inheritdoc />
	public bool VerifyPassword(char[] password, string hash)
	{
		string temp = new(password);

		try
		{
			return BC.EnhancedVerify(temp, hash);
		}
		finally
		{
			SecureStringHelper.WipeString(temp);
		}
	}
	#endregion

	#region Helpers
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
		ReadOnlySpan<byte> ciphertext)
	{
		if (ciphertext.Length < _algorithm.TagSize)
		{
			return null;
		}

		byte[] plaintext = new byte[ciphertext.Length - _algorithm.TagSize];

		return _algorithm.Decrypt(
			key: key,
			nonce: nonce,
			associatedData: [],
			ciphertext: ciphertext,
			plaintext: plaintext) ? plaintext : null;
	}

	/// <summary>
	/// Decrypts a blob laid out as <c>[version][salt][nonce][ciphertext+tag]</c>.
	/// A zero <paramref name="saltSize" /> means the format carries no salt.
	/// </summary>
	private byte[]? DecryptCore(
		byte[] input,
		byte[] secret,
		byte version,
		int saltSize,
		KeyFactory keyFactory)
	{
		ArgumentNullException.ThrowIfNull(input);

		// Guard: enough bytes for [version][salt][nonce][tag] and the version byte must match.
		if (input.Length < 1 + saltSize + _algorithm.NonceSize + _algorithm.TagSize
			|| input[0] != version)
		{
			return null;
		}

		try
		{
			ReadOnlySpan<byte> salt = input.AsSpan(1, saltSize);

			using Key key = keyFactory(secret, salt);

			ReadOnlySpan<byte> nonce = input.AsSpan(1 + saltSize, _algorithm.NonceSize);

			ReadOnlySpan<byte> ciphertext = input.AsSpan(1 + saltSize + _algorithm.NonceSize);

			return OpenAead(key, nonce, ciphertext);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return null;
		}
	}

	/// <summary>
	/// Encrypts into a blob laid out as <c>[version][salt][nonce][ciphertext+tag]</c>.
	/// A zero <paramref name="saltSize" /> means the format carries no salt.
	/// </summary>
	private byte[]? EncryptCore(
		byte[] input,
		byte[] secret,
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
				associatedData: [],
				plaintext: input,
				ciphertext: result.AsSpan(1 + saltSize + nonceSize));

			return result;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return null;
		}
	}
	#endregion
}
