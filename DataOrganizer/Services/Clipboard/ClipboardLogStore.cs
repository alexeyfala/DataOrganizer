using DataOrganizer.DTO.Clipboard;
using DataOrganizer.DTO.Clipboard.Persistence;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Encryption;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

public sealed class ClipboardLogStore : IClipboardLogStore
{
	#region Properties
	/// <inheritdoc />
	public bool IsUnlocked => _sessionKeyStore.IsUnlocked(_historyKeyId);

	/// <inheritdoc />
	public bool KeyFileExists => _fileSystem.IsFileExists(_keyFilePath);
	#endregion

	#region Data
	/// <summary>
	/// Service key of the session key store dedicated to the clipboard history.
	/// </summary>
	public const string SessionKeyStoreKey = "ClipboardHistory";

	/// <summary>
	/// File name of the encrypted journal.
	/// </summary>
	private const string HistoryFileName = "History.bin";

	/// <summary>
	/// File name of the password-wrapped data encryption key.
	/// </summary>
	private const string KeyFileName = "History.key";

	/// <summary>
	/// Identifier the data encryption key is held under in the session key store.
	/// </summary>
	private static readonly Guid _historyKeyId = new("6f0a1c74-6c8e-4f2b-9a3d-7e5b1c0d8a42");

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <summary>
	/// Absolute path to the encrypted journal file.
	/// </summary>
	private readonly string _historyFilePath;

	/// <summary>
	/// Absolute path to the wrapped-key file.
	/// </summary>
	private readonly string _keyFilePath;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ISessionKeyStore" />
	private readonly ISessionKeyStore _sessionKeyStore;
	#endregion

	#region Constructors
	public ClipboardLogStore(
		IAppEnvironment appEnvironment,
		IEncryptionService encryption,
		IFileSystem fileSystem,
		ILogger logger,
		[FromKeyedServices(SessionKeyStoreKey)] ISessionKeyStore sessionKeyStore)
	{
		_encryption = encryption;

		_fileSystem = fileSystem;

		_logger = logger;

		_sessionKeyStore = sessionKeyStore;

		_historyFilePath = appEnvironment.GetClipboardHistoryFilePath(HistoryFileName);

		_keyFilePath = appEnvironment.GetClipboardHistoryFilePath(KeyFileName);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose() => _sessionKeyStore.Lock(_historyKeyId);

	/// <inheritdoc />
	public void EraseAll()
	{
		TryEraseFile(_historyFilePath);

		TryEraseFile(_keyFilePath);

		TryEraseDirectory();

		Dispose();
	}

	/// <inheritdoc />
	public void EraseHistory() => TryEraseFile(_historyFilePath);

	/// <inheritdoc />
	public async Task SaveAsync(IReadOnlyList<ClipboardLogEntryBase> entries, CancellationToken token = default)
	{
		if (!IsUnlocked)
		{
			return;
		}

		byte[] plaintext = JsonSerializer.SerializeToUtf8Bytes(ClipboardLogMapper.ToPersisted(entries));

		try
		{
			byte[] ciphertext = _sessionKeyStore.Encrypt(
				_historyKeyId,
				ContentIdentity.ForClipboardJournal(_historyKeyId),
				plaintext);

			EnsureDirectory();

			await _fileSystem
				.WriteAllBytesAsync(_historyFilePath, ciphertext, token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);
		}
		finally
		{
			// Plaintext journal may contain secrets — wipe it once encrypted / on failure.
			plaintext.ZeroMemory();
		}
	}

	/// <inheritdoc />
	public async Task<ClipboardLogUnlockResult> TryUnlockAsync(PinnedBuffer password, CancellationToken token = default)
	{
		try
		{
			return _fileSystem.IsFileExists(_keyFilePath)
				? await UnlockExistingAsync(password, token).ConfigureAwait(false)
				: await CreateNewKeyAsync(password, token).ConfigureAwait(false);
		}
		catch (InvalidCredentialException)
		{
			_logger.LogWarning("The password of the clipboard history has been rejected.");

			return new(ClipboardLogStatus.WrongPassword, []);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);

			return new(ClipboardLogStatus.Failed, []);
		}
	}

	/// <summary>
	/// Decrypts and maps the journal with the current key; empty on missing / corrupt / unknown-version data.
	/// </summary>
	internal async Task<IReadOnlyList<ClipboardLogEntryBase>> LoadEntriesAsync(CancellationToken token)
	{
		if (!_fileSystem.IsFileExists(_historyFilePath))
		{
			return [];
		}

		byte[] ciphertext = await _fileSystem
			.ReadAllBytesAsync(_historyFilePath, token)
			.ConfigureAwait(false);

		byte[] plaintext;

		try
		{
			plaintext = _sessionKeyStore.Decrypt(
				_historyKeyId,
				ContentIdentity.ForClipboardJournal(_historyKeyId),
				ciphertext);
		}
		catch (CryptographicException ex)
		{
			// The key is right, so the journal itself is damaged; the unlock still stands.
			_logger.LogException(ex);

			return [];
		}

		try
		{
			if (JsonSerializer.Deserialize<PersistedClipboardLog>(plaintext) is not { } history)
			{
				return [];
			}

			if (history.Version != PersistedClipboardLog.CurrentVersion)
			{
				_logger.LogWarning(
					$"Clipboard history version {history.Version} is not supported (expected {PersistedClipboardLog.CurrentVersion}); treating as empty.");

				return [];
			}

			return ClipboardLogMapper.ToDomain(history);
		}
		catch (JsonException ex)
		{
			_logger.LogWarning($"Clipboard history journal is malformed; treating as empty: {ex.Message}");

			return [];
		}
		finally
		{
			plaintext.ZeroMemory();
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a fresh DEK, wraps it with <paramref name="password" />, and stores the wrapped key.
	/// </summary>
	private async Task<ClipboardLogUnlockResult> CreateNewKeyAsync(PinnedBuffer password, CancellationToken token)
	{
		byte[] dek = _encryption.CreateRandomDek();

		try
		{
			byte[] wrapped = _encryption.Encrypt(
				dek,
				password,
				ContentIdentity.ForClipboardDek(_historyKeyId).ToAssociatedData());

			EnsureDirectory();

			await _fileSystem
				.WriteAllBytesAsync(_keyFilePath, wrapped, token)
				.ConfigureAwait(false);

			return _sessionKeyStore.Unlock(_historyKeyId, dek)
				? new(ClipboardLogStatus.Unlocked, [])
				: new(ClipboardLogStatus.Failed, []);
		}
		finally
		{
			dek.ZeroMemory();
		}
	}

	/// <summary>
	/// Ensures the clipboard history directory exists.
	/// </summary>
	private void EnsureDirectory()
	{
		if (Path.GetDirectoryName(_historyFilePath) is not { Length: > 0 } directory)
		{
			return;
		}

		_fileSystem.CreateDirectory(directory);
	}

	/// <summary>
	/// Removes the clipboard history directory if present.
	/// </summary>
	private void TryEraseDirectory()
	{
		try
		{
			if (Path.GetDirectoryName(_historyFilePath) is { Length: > 0 } directory && _fileSystem.IsDirectoryExists(directory))
			{
				_fileSystem.DeleteDirectory(directory);
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);
		}
	}

	/// <summary>
	/// Erases a file if present.
	/// </summary>
	private void TryEraseFile(string filePath)
	{
		try
		{
			if (_fileSystem.IsFileExists(filePath))
			{
				_fileSystem.EraseAndDeleteFile(filePath);
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);
		}
	}

	/// <summary>
	/// Unwraps an existing key with <paramref name="password" /> and loads the previous journal.
	/// </summary>
	private async Task<ClipboardLogUnlockResult> UnlockExistingAsync(PinnedBuffer password, CancellationToken token)
	{
		byte[] wrapped = await _fileSystem
			.ReadAllBytesAsync(_keyFilePath, token)
			.ConfigureAwait(false);

		byte[] dek = _encryption.Decrypt(
			wrapped,
			password,
			ContentIdentity.ForClipboardDek(_historyKeyId).ToAssociatedData());

		try
		{
			if (!_sessionKeyStore.Unlock(_historyKeyId, dek))
			{
				return new(ClipboardLogStatus.Failed, []);
			}
		}
		finally
		{
			dek.ZeroMemory();
		}

		IReadOnlyList<ClipboardLogEntryBase> entries = await LoadEntriesAsync(token).ConfigureAwait(false);

		return new(ClipboardLogStatus.Unlocked, entries);
	}
	#endregion
}
