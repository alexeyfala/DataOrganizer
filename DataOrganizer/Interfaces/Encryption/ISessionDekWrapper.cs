using DataOrganizer.Helpers.Security;
using System;
using System.Security.Cryptography;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Wraps data encryption keys with the identifier of the session.
/// </summary>
public interface ISessionDekWrapper
{
	#region Methods
	/// <summary>
	/// Unwraps the session encrypted DEK using a session identifier (runs HKDF); the key is returned
	/// in pinned storage the caller owns.
	/// </summary>
	/// <exception cref="AuthenticationTagMismatchException">The key or the associated data does not fit the input, or the input has been altered.</exception>
	/// <exception cref="CryptographicException">The wrapper is of another format or size, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input or the session identifier is absent.</exception>
	PinnedBuffer DecryptWithSessionId(
		byte[] input,
		PinnedBuffer sessionId,
		ContentIdentity identity);

	/// <summary>
	/// Wraps a DEK with a session identifier (runs HKDF), for the length of a session.
	/// </summary>
	/// <exception cref="CryptographicException">The input is not the size of a key, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The key or the session identifier is absent.</exception>
	byte[] EncryptWithSessionId(
		PinnedBuffer dek,
		PinnedBuffer sessionId,
		ContentIdentity identity);
	#endregion
}
