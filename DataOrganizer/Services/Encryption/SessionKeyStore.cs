using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;

namespace DataOrganizer.Services.Encryption;

public sealed class SessionKeyStore : ISessionKeyStore, IDisposable
{
	#region Data
	/// <summary>
	/// Size of the session secret: 256 bits of entropy for the key derivation.
	/// </summary>
	private const int SessionIdSize = 32;

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="System.Threading.Lock" />
	private readonly Lock _mutex = new();

	/// <summary>
	/// Session wrapped data encryption key per password keeper. The value is ciphertext: it opens
	/// only with the session secret, so it needs neither pinning nor wiping.
	/// </summary>
	private readonly Dictionary<Guid, byte[]> _wrappedDeks = [];

	/// <summary>
	/// Session secret the stored keys are wrapped with; exists while at least one keeper is unlocked.
	/// </summary>
	private PinnedBuffer? _sessionId;
	#endregion

	#region Constructors
	public SessionKeyStore(IEncryptionService encryption) => _encryption = encryption;
	#endregion

	#region Methods
	/// <inheritdoc />
	public byte[] Decrypt(
		Guid keeperId,
		ContentIdentity identity,
		byte[] encryptedContents)
	{
		lock (_mutex)
		{
			using PinnedBuffer dek = Unwrap(keeperId);

			return _encryption.DecryptWithDek(
				encryptedContents,
				dek,
				identity);
		}
	}

	/// <inheritdoc />
	public void Dispose() => LockAll();

	/// <inheritdoc />
	public byte[] Encrypt(Guid keeperId, ContentIdentity identity, byte[] contents)
	{
		lock (_mutex)
		{
			using PinnedBuffer dek = Unwrap(keeperId);

			return _encryption.EncryptWithDek(
				contents,
				dek,
				identity);
		}
	}

	/// <inheritdoc />
	public bool IsUnlocked(Guid keeperId)
	{
		lock (_mutex)
		{
			return _wrappedDeks.ContainsKey(keeperId);
		}
	}

	/// <inheritdoc />
	public void Lock(Guid keeperId)
	{
		lock (_mutex)
		{
			Remove(keeperId);

			if (_wrappedDeks.Count == 0)
			{
				DropSessionId();
			}
		}
	}

	/// <inheritdoc />
	public void LockAll()
	{
		lock (_mutex)
		{
			// The wrappers are left to the collector: dropping the session secret below already
			// makes every one of them unopenable.
			_wrappedDeks.Clear();

			DropSessionId();
		}
	}

	/// <inheritdoc />
	public bool Unlock(Guid keeperId, PinnedBuffer dek)
	{
		if (dek is null or { Length: 0 })
		{
			return false;
		}

		lock (_mutex)
		{
			PinnedBuffer sessionId = EnsureSessionId();

			byte[] wrappedDek;

			try
			{
				wrappedDek = _encryption.EncryptWithSessionId(
					dek,
					sessionId,
					ContentIdentity.ForDek(keeperId));
			}
			catch
			{
				// Nothing has been stored, so a session secret created just now is not needed.
				if (_wrappedDeks.Count == 0)
				{
					DropSessionId();
				}

				throw;
			}

			// Replaces, never drops the session secret: the new key is already wrapped with it.
			Remove(keeperId);

			_wrappedDeks[keeperId] = wrappedDek;

			return true;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Wipes the session secret, invalidating every key wrapped with it.
	/// </summary>
	private void DropSessionId()
	{
		_sessionId?.Dispose();

		_sessionId = null;
	}

	/// <summary>
	/// Returns the session secret, creating it on first use. The store owns the buffer,
	/// so no caller wipes it.
	/// </summary>
	private PinnedBuffer EnsureSessionId()
	{
		if (_sessionId is null)
		{
			_sessionId = new(SessionIdSize);

			RandomNumberGenerator.Fill(_sessionId.AsSpan());
		}

		return _sessionId;
	}

	/// <summary>
	/// Discards the stored key of a keeper, leaving the session secret untouched.
	/// </summary>
	private void Remove(Guid keeperId) => _wrappedDeks.Remove(keeperId);

	/// <summary>
	/// Unwraps the stored key of a keeper.
	/// </summary>
	/// <exception cref="InvalidOperationException">The keeper is locked.</exception>
	private PinnedBuffer Unwrap(Guid keeperId)
	{
		if (_sessionId is not { } sessionId || !_wrappedDeks.TryGetValue(keeperId, out byte[]? wrappedDek))
		{
			throw new InvalidOperationException($@"The keeper ""{keeperId}"" is locked.");
		}

		return _encryption.DecryptWithSessionId(
			wrappedDek,
			sessionId,
			ContentIdentity.ForDek(keeperId));
	}
	#endregion
}
