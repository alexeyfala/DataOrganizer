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
			ReadOnlySpan<byte> salt = input.AsSpan(1, SaltSize);

			using Key key = DeriveKey(password, salt);

			ReadOnlySpan<byte> nonce = input.AsSpan(1 + SaltSize, _algorithm.NonceSize);

			ReadOnlySpan<byte> ciphertext = input.AsSpan(1 + SaltSize + _algorithm.NonceSize);

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
			using Key key = ImportKey(dek);

			ReadOnlySpan<byte> nonce = input.AsSpan(1, _algorithm.NonceSize);

			ReadOnlySpan<byte> ciphertext = input.AsSpan(1 + _algorithm.NonceSize);

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
			int nonceSize = _algorithm.NonceSize;

			byte[] result = new byte[1 + SaltSize + nonceSize + input.Length + _algorithm.TagSize];

			result[0] = FormatVersionPasswordV1;

			Span<byte> saltSpan = result.AsSpan(1, SaltSize);

			RandomNumberGenerator.Fill(saltSpan);

			using Key key = DeriveKey(password, saltSpan);

			Span<byte> nonceSpan = result.AsSpan(1 + SaltSize, nonceSize);

			RandomNumberGenerator.Fill(nonceSpan);

			_algorithm.Encrypt(key, nonceSpan, [], input, result.AsSpan(1 + SaltSize + nonceSize));

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

			int nonceSize = _algorithm.NonceSize;

			byte[] result = new byte[1 + nonceSize + input.Length + _algorithm.TagSize];

			result[0] = FormatVersionDekV1;

			Span<byte> nonceSpan = result.AsSpan(1, nonceSize);

			RandomNumberGenerator.Fill(nonceSpan);

			_algorithm.Encrypt(key, nonceSpan, [], input, result.AsSpan(1 + nonceSize));

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
	#endregion
}
