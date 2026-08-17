using System;
using System.Security.Authentication;
using System.Security.Cryptography;

namespace DataOrganizer.Helpers.Security;

/// <summary>
/// Tells the expected failures of an encryption operation apart from the rest.
/// </summary>
internal static class EncryptionFailures
{
	#region Methods
	/// <summary>
	/// <c>True</c> for a failure of a cryptographic operation, a rejected password included.
	/// </summary>
	public static bool IsCryptographic(Exception exception)
	{
		return exception is InvalidCredentialException or CryptographicException;
	}

	/// <summary>
	/// <c>True</c> for a failure of an operation on the key of a session: damaged data or a locked keeper.
	/// </summary>
	public static bool IsSessionCipher(Exception exception)
	{
		return exception is CryptographicException or InvalidOperationException;
	}
	#endregion
}
