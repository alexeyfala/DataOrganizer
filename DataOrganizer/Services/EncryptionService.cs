using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using NSec.Cryptography;
using Repository.DTO;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using BC = BCrypt.Net.BCrypt;

namespace DataOrganizer.Services;

/// <summary>
/// <inheritdoc cref="IEncryptionService" />
/// </summary>
/// <remarks>
/// <para>
/// <b>Ciphertext format versioning.</b> Every ciphertext produced by this service starts with a
/// single version byte (see <see cref="FormatVersionPasswordV1" /> and <see cref="FormatVersionDekV1" />).
/// The version byte tells the decryptor how to interpret the remaining bytes — which algorithm,
/// which parameters, which layout. Without it, a future change in format (different AEAD, different
/// salt size, different KDF parameters, etc.) would silently break decryption of existing data,
/// because raw bytes are indistinguishable between formats.
/// </para>
/// <para>
/// <b>Current format layout:</b>
/// <list type="bullet">
///   <item><c>0x01</c> — password path: <c>[0x01][salt 16][nonce 24][ciphertext+tag]</c></item>
///   <item><c>0x02</c> — DEK path:      <c>[0x02][nonce 24][ciphertext+tag]</c></item>
/// </list>
/// Different version codes between paths also prevent accidental cross-use: a password-path blob
/// fed to <see cref="DecryptWithDek" /> returns <c>null</c> instead of attempting decryption.
/// </para>
/// <para>
/// <b>Rules when changing the format:</b>
/// <list type="number">
///   <item>Allocate a new version constant (e.g. <c>0x03</c>) — do not reuse existing ones.</item>
///   <item>Add the new layout / algorithm under the new code; keep the old branch readable so existing
///         data can still be decrypted.</item>
///   <item>Bump the version constant only in the <i>encryption</i> path; decryption should accept
///         all known versions and dispatch by the first byte.</item>
///   <item>Unknown version → return <c>null</c> (already enforced by the guards). Never fall back
///         to "try a different format" — that's a downgrade vector.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class EncryptionService : IEncryptionService
{
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
	/// Salt size.
	/// </summary>
	private const int SaltSize = 16;

	/// <summary>
	/// The encryption algorithm used.
	/// </summary>
	private static readonly AeadAlgorithm _algorithm = AeadAlgorithm.XChaCha20Poly1305;

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
		ArgumentNullException.ThrowIfNull(input);

		// Guard: enough bytes for [version][salt][nonce][tag] and the version byte must match.
		if (input.Length < 1 + SaltSize + _algorithm.NonceSize + _algorithm.TagSize
			|| input[0] != FormatVersionPasswordV1)
		{
			return null;
		}

		try
		{
			byte[] salt = new byte[SaltSize];

			Buffer.BlockCopy(input, 1, salt, 0, salt.Length);

			(byte[] nonce, byte[] ciphertext) = ExtractNonceAndCiphertext(input, offset: 1 + SaltSize);

			using Key key = DeriveKey(password, salt);

			return OpenAead(key, nonce, ciphertext);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return null;
		}
	}

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
		ArgumentNullException.ThrowIfNull(input);

		// Guard: enough bytes for [version][nonce][tag] and the version byte must match.
		if (input.Length < 1 + _algorithm.NonceSize + _algorithm.TagSize
			|| input[0] != FormatVersionDekV1)
		{
			return null;
		}

		try
		{
			(byte[] nonce, byte[] ciphertext) = ExtractNonceAndCiphertext(input, offset: 1);

			using Key key = ImportKey(dek);

			return OpenAead(key, nonce, ciphertext);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return null;
		}
	}

	/// <inheritdoc />
	public byte[]? Encrypt(byte[] input, byte[] password)
	{
		try
		{
			byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

			using Key key = DeriveKey(password, salt);

			(byte[] nonce, byte[] ciphertext) = SealAead(key, input);

			// Format: [version 1][salt 16][nonce 24][ciphertext+tag].
			byte[] result = new byte[1 + salt.Length + nonce.Length + ciphertext.Length];

			result[0] = FormatVersionPasswordV1;

			Buffer.BlockCopy(salt, 0, result, 1, salt.Length);

			Buffer.BlockCopy(nonce, 0, result, 1 + salt.Length, nonce.Length);

			Buffer.BlockCopy(ciphertext, 0, result, 1 + salt.Length + nonce.Length, ciphertext.Length);

			return result;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return null;
		}
	}

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
		try
		{
			using Key key = ImportKey(dek);

			(byte[] nonce, byte[] ciphertext) = SealAead(key, input);

			// Format: [version 2][nonce 24][ciphertext+tag].
			byte[] result = new byte[1 + nonce.Length + ciphertext.Length];

			result[0] = FormatVersionDekV1;

			Buffer.BlockCopy(nonce, 0, result, 1, nonce.Length);

			Buffer.BlockCopy(ciphertext, 0, result, 1 + nonce.Length, ciphertext.Length);

			return result;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return null;
		}
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

	#region Service
	/// <summary>
	/// Derives a key.
	/// </summary>
	private static Key DeriveKey(byte[] password, byte[] salt)
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
	/// Extracts the nonce and ciphertext slices from <paramref name="input" /> starting at <paramref name="offset" />.
	/// Caller is responsible for ensuring <paramref name="input" /> has enough bytes.
	/// </summary>
	private static (byte[] Nonce, byte[] Ciphertext) ExtractNonceAndCiphertext(byte[] input, int offset)
	{
		byte[] nonce = new byte[_algorithm.NonceSize];

		byte[] ciphertext = new byte[input.Length - offset - nonce.Length];

		Buffer.BlockCopy(input, offset, nonce, 0, nonce.Length);

		Buffer.BlockCopy(input, offset + nonce.Length, ciphertext, 0, ciphertext.Length);

		return (nonce, ciphertext);
	}

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
		byte[] nonce,
		byte[] ciphertext)
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
	/// Runs AEAD authenticated encryption with a fresh random nonce.
	/// Returns the nonce and the ciphertext (with appended tag).
	/// </summary>
	private static (byte[] Nonce, byte[] Ciphertext) SealAead(Key key, byte[] input)
	{
		byte[] nonce = RandomNumberGenerator.GetBytes(_algorithm.NonceSize);

		byte[] ciphertext = _algorithm.Encrypt(
			key: key,
			nonce: nonce,
			associatedData: [],
			plaintext: input);

		return (nonce, ciphertext);
	}
	#endregion
}
