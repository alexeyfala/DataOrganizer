using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using Shared.Extensions;
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
	/// Session wrapped data encryption key per password keeper.
	/// </summary>
	private readonly Dictionary<Guid, PinnedBuffer> _wrappedDeks = [];

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
			byte[] dek = Unwrap(keeperId);

			try
			{
				return _encryption.DecryptWithDek(
					encryptedContents,
					dek,
					identity);
			}
			finally
			{
				dek.ZeroMemory();
			}
		}
	}

	/// <inheritdoc />
	public void Dispose() => LockAll();

	/// <inheritdoc />
	public byte[] Encrypt(Guid keeperId, ContentIdentity identity, byte[] contents)
	{
		lock (_mutex)
		{
			byte[] dek = Unwrap(keeperId);

			try
			{
				return _encryption.EncryptWithDek(
					contents,
					dek,
					identity);
			}
			finally
			{
				dek.ZeroMemory();
			}
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
			foreach (PinnedBuffer wrappedDek in _wrappedDeks.Values)
			{
				wrappedDek.Dispose();
			}

			_wrappedDeks.Clear();

			DropSessionId();
		}
	}

	/// <inheritdoc />
	public bool Unlock(Guid keeperId, byte[] dek)
	{
		if (dek.IsEmpty())
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

			try
			{
				// Replaces, never drops the session secret: the new key is already wrapped with it.
				Remove(keeperId);

				_wrappedDeks[keeperId] = new(wrappedDek);

				return true;
			}
			finally
			{
				wrappedDek.ZeroMemory();
			}
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
	private void Remove(Guid keeperId)
	{
		if (!_wrappedDeks.Remove(keeperId, out PinnedBuffer? wrappedDek))
		{
			return;
		}

		wrappedDek.Dispose();
	}

	/// <summary>
	/// Unwraps the stored key of a keeper.
	/// </summary>
	/// <exception cref="InvalidOperationException">The keeper is locked.</exception>
	private byte[] Unwrap(Guid keeperId)
	{
		if (_sessionId is null || !_wrappedDeks.TryGetValue(keeperId, out PinnedBuffer? wrappedDek))
		{
			throw new InvalidOperationException($@"The keeper ""{keeperId}"" is locked.");
		}

		PinnedBuffer sessionId = EnsureSessionId();

		byte[] input = wrappedDek
			.AsReadOnlySpan()
			.ToArray();

		try
		{
			return _encryption.DecryptWithSessionId(
				input,
				sessionId,
				ContentIdentity.ForDek(keeperId));
		}
		finally
		{
			input.ZeroMemory();
		}
	}
	#endregion
}
