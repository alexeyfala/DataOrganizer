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

public sealed class EncryptionService : IEncryptionService
{
	#region Data
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
		// Guard against malformed input: otherwise ciphertext.Length would be negative
		// and Buffer.BlockCopy would throw ArgumentException inside the try/catch.
		if (input.Length < SaltSize + _algorithm.NonceSize + _algorithm.TagSize)
		{
			return null;
		}

		try
		{
			byte[] salt = new byte[SaltSize];

			Buffer.BlockCopy(input, 0, salt, 0, salt.Length);

			(byte[] nonce, byte[] ciphertext) = ExtractNonceAndCiphertext(input, offset: SaltSize);

			using Key key = DeriveKey(password, salt);

			return OpenAead(key, nonce, ciphertext);
		}
		// Expected: crypto-level failure.
		catch (CryptographicException ex)
		{
			_logger.LogException(ex);
		}
		// Unexpected: anything else. Caught to keep the UI alive, logged for diagnostics.
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}

		return null;
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
		if (input.Length < _algorithm.NonceSize + _algorithm.TagSize)
		{
			return null;
		}

		try
		{
			(byte[] nonce, byte[] ciphertext) = ExtractNonceAndCiphertext(input, offset: 0);

			using Key key = ImportKey(dek);

			return OpenAead(key, nonce, ciphertext);
		}
		// Expected: crypto-level failure.
		catch (CryptographicException ex)
		{
			_logger.LogException(ex);
		}
		// Unexpected: anything else. Caught to keep the UI alive, logged for diagnostics.
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}

		return null;
	}

	/// <inheritdoc />
	public byte[]? Encrypt(byte[] input, byte[] password)
	{
		try
		{
			byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

			using Key key = DeriveKey(password, salt);

			(byte[] nonce, byte[] ciphertext) = SealAead(key, input);

			byte[] result = new byte[salt.Length + nonce.Length + ciphertext.Length];

			Buffer.BlockCopy(salt, 0, result, 0, salt.Length);

			Buffer.BlockCopy(nonce, 0, result, salt.Length, nonce.Length);

			Buffer.BlockCopy(ciphertext, 0, result, salt.Length + nonce.Length, ciphertext.Length);

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

			byte[] result = new byte[nonce.Length + ciphertext.Length];

			Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);

			Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);

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
