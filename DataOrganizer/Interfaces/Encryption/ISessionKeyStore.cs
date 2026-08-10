using System;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Holds data encryption keys of unlocked password keepers for the lifetime of a session.
/// The keys never leave the store.
/// </summary>
public interface ISessionKeyStore
{
	#region Methods
	/// <summary>
	/// Decrypts contents with the key of an unlocked keeper. Returns <c>null</c> when the keeper is locked.
	/// </summary>
	byte[]? Decrypt(Guid keeperId, byte[] encryptedContents);

	/// <summary>
	/// Encrypts contents with the key of an unlocked keeper. Returns <c>null</c> when the keeper is locked.
	/// </summary>
	byte[]? Encrypt(Guid keeperId, byte[] contents);

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
	/// Takes the key of a keeper into the store. Returns <c>false</c> when the key cannot be stored.
	/// </summary>
	bool Unlock(Guid keeperId, byte[] dek);
	#endregion
}
