using DataProtection.Helpers;
using DataProtection.Interfaces;
using FileContents.Dto;
using FileContents.Enums;
using FileContents.Interfaces;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Repository.Dto;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileContents.Services;

/// <inheritdoc cref="IFileContentsReader" />
public sealed class FileContentsReader : IFileContentsReader
{
	#region Data
	/// <inheritdoc cref="IContentCipher" />
	private readonly IContentCipher _contentCipher;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDekCipher" />
	private readonly IDekCipher _encryption;

	/// <inheritdoc cref="IEncryptionFailureReporter" />
	private readonly IEncryptionFailureReporter _failureReporter;

	/// <inheritdoc cref="IKeeperUnlocker" />
	private readonly IKeeperUnlocker _keeperUnlocker;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public FileContentsReader(
		IContentCipher contentCipher,
		IDbAccess dbAccess,
		IDekCipher encryption,
		IEncryptionFailureReporter failureReporter,
		IKeeperUnlocker keeperUnlocker,
		ILogger logger)
	{
		_contentCipher = contentCipher;

		_dbAccess = dbAccess;

		_encryption = encryption;

		_failureReporter = failureReporter;

		_keeperUnlocker = keeperUnlocker;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task<FileContentsReading> ReadAsync(
		FileDto file,
		string header,
		CancellationToken token = default)
	{
		return ReadAndOpenAsync(
			file.Id,
			contents => OpenAsync(file, contents, header, token),
			token);
	}

	/// <inheritdoc />
	public Task<FileContentsReading> ReadUnlockedAsync(FileDto file, CancellationToken token = default)
	{
		return ReadAndOpenAsync(
			file.Id,
			contents => Task.FromResult(OpenUnlocked(file, contents)),
			token);
	}

	/// <inheritdoc />
	public Task<FileContentsReading> ReadUnlockedAsync(
		Guid fileId,
		Guid? keeperId,
		CancellationToken token = default)
	{
		return ReadAndOpenAsync(
			fileId,
			contents => Task.FromResult(Open(keeperId, contents)),
			token);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Opens contents held under the session key of a keeper, if one is given; <c>null</c> reports a refusal.
	/// </summary>
	private byte[]? Open(Guid? keeperId, byte[] contents)
	{
		// Empty content is written without encryption, so there is nothing to open.
		if (keeperId is not { } id || contents.IsEmpty())
		{
			return contents;
		}

		return _contentCipher.TryDecrypt(
			id,
			ContentIdentity.Contents,
			contents);
	}

	/// <summary>
	/// Opens the contents of a file, asking for the password of a locked keeper; <c>null</c> when they stay closed.
	/// </summary>
	private async Task<byte[]?> OpenAsync(
		FileDto file,
		byte[] contents,
		string header,
		CancellationToken token)
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
					ContentIdentity.Contents);
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
				return _contentCipher.Decrypt(file, contents);
			}
			catch (Exception ex) when (EncryptionFailures.IsSessionCipher(ex))
			{
				_failureReporter.Report(ex);

				return null;
			}
		}

		return contents;
	}

	/// <summary>
	/// Opens the contents of a file without asking for a password; <c>null</c> when they stay closed.
	/// </summary>
	private byte[]? OpenUnlocked(FileDto file, byte[] contents)
	{
		if (file.EncryptionStatus == EncryptionStatus.Encrypted)
		{
			return null;
		}

		if (file.EncryptionStatus != EncryptionStatus.Decrypted)
		{
			return contents;
		}

		try
		{
			return _contentCipher.Decrypt(file, contents);
		}
		// Stryker disable once Block : without the block the method returns null as well, and only the log goes
		catch (Exception ex) when (EncryptionFailures.IsSessionCipher(ex))
		{
			// The contents are read without a prompt, so the failure only reaches the log.
			_logger.LogException(ex);

			return null;
		}
	}

	/// <summary>
	/// Reads the stored contents of a file, checks them and opens them by <paramref name="open" />, which hands back
	/// <c>null</c> for contents that stay closed.
	/// </summary>
	private async Task<FileContentsReading> ReadAndOpenAsync(
		Guid fileId,
		Func<byte[], Task<byte[]?>> open,
		CancellationToken token)
	{
		ValidatedContents result = await _dbAccess
			.GetFileContentsAsync(fileId, token)
			.ConfigureAwait(false);

		if (!result.IsValid)
		{
			return new()
			{
				Status = FileContentsStatus.Damaged
			};
		}

		if (await open(result.Contents).ConfigureAwait(false) is not { } contents)
		{
			return new()
			{
				Status = FileContentsStatus.Closed
			};
		}

		return new()
		{
			Contents = contents,
			Status = FileContentsStatus.Opened
		};
	}
	#endregion
}
