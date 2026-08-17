using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using Shared.Extensions;
using System;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Encryption;

public sealed class ContentCipher : IContentCipher
{
	#region Data
	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IEncryptionFailureReporter" />
	private readonly IEncryptionFailureReporter _failureReporter;

	/// <inheritdoc cref="IKeeperUnlocker" />
	private readonly IKeeperUnlocker _keeperUnlocker;

	/// <inheritdoc cref="ISessionKeyStore" />
	private readonly ISessionKeyStore _sessionKeyStore;
	#endregion

	#region Constructors
	public ContentCipher(
		IEncryptionService encryption,
		IEncryptionFailureReporter failureReporter,
		IKeeperUnlocker keeperUnlocker,
		ISessionKeyStore sessionKeyStore)
	{
		_encryption = encryption;

		_failureReporter = failureReporter;

		_keeperUnlocker = keeperUnlocker;

		_sessionKeyStore = sessionKeyStore;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public byte[] Decrypt(FileModelDto file, byte[] input)
	{
		// Empty content is written without encryption, so there is nothing to open here.
		if (input.IsEmpty())
		{
			return input;
		}

		if (file.FindPasswordKeeper() is not { } root)
		{
			throw new InvalidOperationException(
				$@"The file ""{file.Id}"" is marked as decrypted but belongs to no password keeper.");
		}

		return _sessionKeyStore.Decrypt(
			root.Id,
			ContentIdentity.ForContents(file.Id),
			input);
	}

	/// <inheritdoc />
	public async Task<byte[]?> TryToDecryptContentsAsync(
		FileModelDto file,
		byte[] contents,
		string header,
		CancellationToken token = default)
	{
		// Empty content is written without encryption, so neither a password nor a key is needed.
		if (contents.IsEmpty())
		{
			return contents;
		}

		if (file.EncryptionStatus == EncryptionStatus.Encrypted)
		{
			if (file.FindPasswordKeeper() is not { } root || root.EncryptedDek is null)
			{
				return null;
			}

			if (await _keeperUnlocker.RequestDekAsync(
				keeperId: root.Id,
				encryptedDek: root.EncryptedDek,
				header: header,
				token: token).ConfigureAwait(false) is not { } decryptedDek)
			{
				return null;
			}

			try
			{
				return _encryption.DecryptWithDek(
					contents,
					decryptedDek,
					ContentIdentity.ForContents(file.Id));
			}
			catch (Exception ex) when (ex is InvalidCredentialException or CryptographicException)
			{
				_failureReporter.Report(ex);

				return null;
			}
			finally
			{
				decryptedDek.ZeroMemory();
			}
		}
		else if (file.EncryptionStatus == EncryptionStatus.Decrypted)
		{
			try
			{
				return Decrypt(file, contents);
			}
			catch (Exception ex) when (ex is CryptographicException or InvalidOperationException)
			{
				_failureReporter.Report(ex);

				return null;
			}
		}

		return contents;
	}
	#endregion
}
