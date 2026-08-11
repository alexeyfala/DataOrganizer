using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;

namespace DataOrganizer.Services.Encryption;

public sealed class SessionKeyStore : ISessionKeyStore, IDisposable
{
	#region Data
	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

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
	public SessionKeyStore(IEncryptionService encryption, ILogger logger)
	{
		_encryption = encryption;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public byte[]? Decrypt(Guid keeperId, ContentIdentity identity, byte[] encryptedContents)
	{
		lock (_mutex)
		{
			if (Unwrap(keeperId) is not { } dek)
			{
				return null;
			}

			try
			{
				if (_encryption.DecryptWithDek(
					encryptedContents,
					dek,
					identity.ToAssociatedData()) is { } decrypted)
				{
					return decrypted;
				}

				// The key is there, so the contents either do not belong to this place or have been altered.
				_logger.LogWarning(
					$@"Contents of the unlocked keeper ""{keeperId}"" failed authentication: {identity.Purpose} of ""{identity.Id}""");

				return null;
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
	public byte[]? Encrypt(Guid keeperId, ContentIdentity identity, byte[] contents)
	{
		lock (_mutex)
		{
			if (Unwrap(keeperId) is not { } dek)
			{
				return null;
			}

			try
			{
				return _encryption.EncryptWithDek(contents, dek, identity.ToAssociatedData());
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
			byte[] sessionId = GetSessionId();

			try
			{
				// The stored key is bound to its keeper, so a wrapped key cannot be moved between keepers.
				if (_encryption.EncryptWithSessionId(
					dek,
					sessionId,
					ContentIdentity.ForDek(keeperId).ToAssociatedData()) is not { } wrappedDek)
				{
					// Nothing has been stored, so a session secret created just now is not needed.
					if (_wrappedDeks.Count == 0)
					{
						DropSessionId();
					}

					return false;
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
			finally
			{
				sessionId.ZeroMemory();
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
	/// Returns a copy of the session secret, creating the secret on first use.
	/// The copy is short lived and the caller wipes it.
	/// </summary>
	private byte[] GetSessionId()
	{
		if (_sessionId is null)
		{
			int length = RandomNumberGenerator.GetInt32(32, 65);

			_sessionId = new(length);

			RandomNumberGenerator.Fill(_sessionId.AsSpan());
		}

		return _sessionId
			.AsReadOnlySpan()
			.ToArray();
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
	/// Unwraps the stored key of a keeper. Returns <c>null</c> when the keeper is locked
	/// or the stored key no longer matches the session secret.
	/// </summary>
	private byte[]? Unwrap(Guid keeperId)
	{
		if (_sessionId is null || !_wrappedDeks.TryGetValue(keeperId, out PinnedBuffer? wrappedDek))
		{
			return null;
		}

		byte[] sessionId = GetSessionId();

		byte[] input = wrappedDek
			.AsReadOnlySpan()
			.ToArray();

		try
		{
			return _encryption.DecryptWithSessionId(
				input,
				sessionId,
				ContentIdentity.ForDek(keeperId).ToAssociatedData());
		}
		finally
		{
			input.ZeroMemory();

			sessionId.ZeroMemory();
		}
	}
	#endregion
}
