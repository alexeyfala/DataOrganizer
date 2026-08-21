using DataOrganizer.Helpers.Security;
using System;
using System.Security.Cryptography;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Holds data encryption keys of unlocked password keepers for the lifetime of a session.
/// A held key stays wrapped with a session secret and is unwrapped for the length of a single call.
/// </summary>
public interface ISessionKeyStore
{
	#region Methods
	/// <summary>
	/// Decrypts contents with the key of an unlocked keeper.
	/// </summary>
	/// <exception cref="InvalidOperationException">The keeper is locked.</exception>
	/// <exception cref="AuthenticationTagMismatchException">The contents do not fit the purpose of <paramref name="identity" /> or have been altered.</exception>
	byte[] Decrypt(
		Guid keeperId,
		ContentIdentity identity,
		byte[] encryptedContents);

	/// <summary>
	/// Encrypts contents with the key of an unlocked keeper, binding them to the purpose of <paramref name="identity" />.
	/// </summary>
	/// <exception cref="InvalidOperationException">The keeper is locked.</exception>
	byte[] Encrypt(
		Guid keeperId,
		ContentIdentity identity,
		byte[] contents);

	/// <summary>
	/// Indicates whether the key of a keeper is currently held.
	/// </summary>
	bool IsUnlocked(Guid keeperId);

	/// <summary>
	/// Drops the key of a keeper.
	/// </summary>
	void Lock(Guid keeperId);

	/// <summary>
	/// Drops the keys of every keeper together with the session secret.
	/// </summary>
	void LockAll();

	/// <summary>
	/// Takes the key of a keeper into the store, wrapped with the session secret.
	/// Returns <c>false</c> when the key cannot be stored; the passed key remains with the caller.
	/// </summary>
	bool Unlock(Guid keeperId, PinnedBuffer dek);
	#endregion
}
