using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using Serilog;
using Shared.Extensions;
using System;
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

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ISessionKeyStore" />
	private readonly ISessionKeyStore _sessionKeyStore;
	#endregion

	#region Constructors
	public ContentCipher(
		IEncryptionService encryption,
		IEncryptionFailureReporter failureReporter,
		IKeeperUnlocker keeperUnlocker,
		ILogger logger,
		ISessionKeyStore sessionKeyStore)
	{
		_encryption = encryption;

		_failureReporter = failureReporter;

		_keeperUnlocker = keeperUnlocker;

		_logger = logger;

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
	public byte[]? TryDecrypt(Guid keeperId, ContentIdentity identity, byte[] input)
	{
		try
		{
			return _sessionKeyStore.Decrypt(keeperId, identity, input);
		}
		catch (Exception ex) when (EncryptionFailures.IsSessionCipher(ex))
		{
			// The caller renders or saves content, so the failure only reaches the log.
			_logger.LogException(ex);

			return null;
		}
	}

	/// <inheritdoc />
	public byte[]? TryEncrypt(Guid keeperId, ContentIdentity identity, byte[] input)
	{
		try
		{
			return _sessionKeyStore.Encrypt(keeperId, identity, input);
		}
		catch (Exception ex) when (EncryptionFailures.IsSessionCipher(ex))
		{
			// The caller renders or saves content, so the failure only reaches the log.
			_logger.LogException(ex);

			return null;
		}
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

			using PinnedBuffer? decryptedDek = await _keeperUnlocker.RequestDekAsync(
				keeper: root,
				header: header,
				token: token).ConfigureAwait(false);

			if (decryptedDek is null)
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
			catch (Exception ex) when (EncryptionFailures.IsCryptographic(ex))
			{
				_failureReporter.Report(ex);

				return null;
			}
		}
		else if (file.EncryptionStatus == EncryptionStatus.Decrypted)
		{
			try
			{
				return Decrypt(file, contents);
			}
			catch (Exception ex) when (EncryptionFailures.IsSessionCipher(ex))
			{
				_failureReporter.Report(ex);

				return null;
			}
		}

		return contents;
	}
	#endregion
}
