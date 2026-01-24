using DataOrganizer.Interfaces;
using NSec.Cryptography;
using Serilog;
using Shared.Extensions;
using System;
using System.Security.Cryptography;

namespace DataOrganizer.Services;

public sealed class EncryptionService : IEncryptionService
{
	#region Data
	/// <summary>
	/// Salt size.
	/// </summary>
	private const int _saltSize = 16;

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
	public bool Decrypt(byte[] input, byte[] password, out byte[] output)
	{
		output = [];

		try
		{
			byte[] salt = new byte[_saltSize];

			byte[] nonce = new byte[_algorithm.NonceSize];

			byte[] cipherText = new byte[input.Length - salt.Length - nonce.Length];

			Buffer.BlockCopy(input, 0, salt, 0, salt.Length);

			Buffer.BlockCopy(input, salt.Length, nonce, 0, nonce.Length);

			Buffer.BlockCopy(input, salt.Length + nonce.Length, cipherText, 0, cipherText.Length);

			using Key key = DeriveKey(password, salt);

			if (_algorithm.Decrypt(
				key: key,
				nonce: nonce,
				associatedData: [],
				ciphertext: cipherText) is { } decrypted)
			{
				output = decrypted;

				return true;
			}

			return false;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
	}

	/// <inheritdoc />
	public bool Encrypt(byte[] input, byte[] password, out byte[] output)
	{
		output = [];

		try
		{
			byte[] salt = RandomNumberGenerator.GetBytes(_saltSize);

			byte[] nonce = RandomNumberGenerator.GetBytes(_algorithm.NonceSize);

			using Key key = DeriveKey(password, salt);

			byte[] encrypted = _algorithm.Encrypt(
				key,
				nonce,
				associatedData: [],
				input);

			byte[] result = new byte[salt.Length + nonce.Length + encrypted.Length];

			Buffer.BlockCopy(salt, 0, result, 0, salt.Length);

			Buffer.BlockCopy(nonce, 0, result, salt.Length, nonce.Length);

			Buffer.BlockCopy(encrypted, 0, result, salt.Length + nonce.Length, encrypted.Length);

			output = result;

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
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

		return Key.Import(
			algorithm: _algorithm,
			blob: blob,
			format: KeyBlobFormat.RawSymmetricKey);
	}
	#endregion
}
