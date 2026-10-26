using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using Hierarchy.Dto;
using Serilog;
using Shared.Extensions;
using System;

namespace DataOrganizer.Services.Encryption;

/// <inheritdoc cref="IContentCipher" />
public sealed class ContentCipher : IContentCipher
{
	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ISessionKeyStore" />
	private readonly ISessionKeyStore _sessionKeyStore;
	#endregion

	#region Constructors
	public ContentCipher(ILogger logger, ISessionKeyStore sessionKeyStore)
	{
		_logger = logger;

		_sessionKeyStore = sessionKeyStore;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public byte[] Decrypt(FileDto file, byte[] input)
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
			ContentIdentity.Contents,
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
			_logger.LogException(ex, breakInDebugger: false);

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

	#endregion
}
