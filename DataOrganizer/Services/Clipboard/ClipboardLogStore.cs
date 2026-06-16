using DataOrganizer.DTO.Clipboard;
using DataOrganizer.DTO.Clipboard.Persistence;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Encryption;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

public sealed class ClipboardLogStore : IClipboardLogStore
{
	#region Properties
	/// <inheritdoc />
	public bool IsUnlocked
	{
		get
		{
			lock (_keyLock)
			{
				return _dek is not null;
			}
		}
	}

	/// <inheritdoc />
	public bool KeyFileExists => _fileSystem.IsFileExists(_keyFilePath);
	#endregion

	#region Data
	/// <summary>
	/// File name of the encrypted journal.
	/// </summary>
	private const string HistoryFileName = "History.bin";

	/// <summary>
	/// File name of the password-wrapped data encryption key.
	/// </summary>
	private const string KeyFileName = "History.key";

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

	/// <summary>
	/// Guards access to <see cref="_dek" />.
	/// </summary>
	private readonly Lock _keyLock = new();

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <summary>
	/// The unwrapped data encryption key for this session; <c>null</c> until unlocked.
	/// </summary>
	private byte[]? _dek;
	#endregion

	#region Constructors
	public ClipboardLogStore(
		IAppEnvironment appEnvironment,
		IEncryptionService encryption,
		IFileSystem fileSystem,
		ILogger logger)
	{
		_encryption = encryption;

		_fileSystem = fileSystem;

		_logger = logger;

		_historyFilePath = appEnvironment.GetClipboardHistoryFilePath(HistoryFileName);

		_keyFilePath = appEnvironment.GetClipboardHistoryFilePath(KeyFileName);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		lock (_keyLock)
		{
			_dek?.ZeroMemory();

			_dek = null;
		}
	}

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
		byte[]? dek = GetKey();

		if (dek is null)
		{
			return;
		}

		byte[] plaintext = JsonSerializer.SerializeToUtf8Bytes(ClipboardLogMapper.ToPersisted(entries));

		try
		{
			if (_encryption.EncryptWithDek(plaintext, dek) is not { } ciphertext)
			{
				_logger.LogWarning("Failed to encrypt clipboard history; skipping save.");

				return;
			}

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
	public async Task<ClipboardLogUnlockResult> TryUnlockAsync(byte[] password, CancellationToken token = default)
	{
		try
		{
			return _fileSystem.IsFileExists(_keyFilePath)
				? await UnlockExistingAsync(password, token).ConfigureAwait(false)
				: await CreateNewKeyAsync(password, token).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);

			return new(ClipboardHistoryLogStatus.Failed, []);
		}
	}

	/// <summary>
	/// Decrypts and maps the journal with the current key; empty on missing / corrupt / unknown-version data.
	/// </summary>
	internal async Task<IReadOnlyList<ClipboardLogEntryBase>> LoadEntriesAsync(byte[] dek, CancellationToken token)
	{
		if (!_fileSystem.IsFileExists(_historyFilePath))
		{
			return [];
		}

		byte[] ciphertext = await _fileSystem
			.ReadAllBytesAsync(_historyFilePath, token)
			.ConfigureAwait(false);

		if (_encryption.DecryptWithDek(ciphertext, dek) is not { } plaintext)
		{
			_logger.LogWarning("Clipboard history journal could not be decrypted; treating as empty.");

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
	private async Task<ClipboardLogUnlockResult> CreateNewKeyAsync(byte[] password, CancellationToken token)
	{
		byte[] dek = _encryption.CreateRandomDek();

		if (_encryption.Encrypt(dek, password) is not { } wrapped)
		{
			dek.ZeroMemory();

			_logger.LogWarning("Failed to wrap a new clipboard history key.");

			return new(ClipboardHistoryLogStatus.Failed, []);
		}

		EnsureDirectory();

		await _fileSystem
			.WriteAllBytesAsync(_keyFilePath, wrapped, token)
			.ConfigureAwait(false);

		SetKey(dek);

		return new(ClipboardHistoryLogStatus.Unlocked, []);
	}

	/// <summary>
	/// Ensures the clipboard history directory exists.
	/// </summary>
	private void EnsureDirectory()
	{
		if (Path.GetDirectoryName(_historyFilePath) is { Length: > 0 } directory)
		{
			_fileSystem.CreateDirectory(directory);
		}
	}

	/// <summary>
	/// Returns the current session key (a snapshot), or <c>null</c> when locked.
	/// </summary>
	private byte[]? GetKey()
	{
		lock (_keyLock)
		{
			return _dek;
		}
	}

	/// <summary>
	/// Stores the unwrapped session key, wiping any previous one.
	/// </summary>
	private void SetKey(byte[] dek)
	{
		lock (_keyLock)
		{
			_dek?.ZeroMemory();

			_dek = dek;
		}
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
	private async Task<ClipboardLogUnlockResult> UnlockExistingAsync(byte[] password, CancellationToken token)
	{
		byte[] wrapped = await _fileSystem
			.ReadAllBytesAsync(_keyFilePath, token)
			.ConfigureAwait(false);

		if (_encryption.Decrypt(wrapped, password) is not { } dek)
		{
			return new(ClipboardHistoryLogStatus.WrongPassword, []);
		}

		SetKey(dek);

		IReadOnlyList<ClipboardLogEntryBase> entries = await LoadEntriesAsync(dek, token).ConfigureAwait(false);

		return new(ClipboardHistoryLogStatus.Unlocked, entries);
	}
	#endregion
}
