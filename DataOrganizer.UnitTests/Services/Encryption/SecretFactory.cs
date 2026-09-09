using DataOrganizer.Helpers.Security;
using Shared.Common;
using System;
using TestSupport;

namespace DataOrganizer.UnitTests.Services.Encryption;

/// <summary>
/// Builds pinned secrets for the tests.
/// </summary>
internal static class SecretFactory
{
	#region Methods
	/// <summary>
	/// A pinned buffer holding random key material of the given size.
	/// </summary>
	public static PinnedBuffer CreateRandomKey(int size = 32) => new(TestData.CreateRandomBytes(size));

	/// <summary>
	/// A secret holding a random string of the given length.
	/// </summary>
	public static PinnedSecret CreateRandomSecret(int length = 10) => CreateSecret(RandomString.Create(length));

	/// <summary>
	/// A secret holding the given characters.
	/// </summary>
	public static PinnedSecret CreateSecret(string value)
	{
		PinnedSecret secret = new(value.Length);

		value
			.AsSpan()
			.CopyTo(secret.AsSpan());

		return secret;
	}
	#endregion
}
