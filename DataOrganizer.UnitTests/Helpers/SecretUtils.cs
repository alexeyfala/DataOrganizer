using DataOrganizer.Helpers.Security;
using Shared.Common;
using System;

namespace DataOrganizer.UnitTests.Helpers;

/// <summary>
/// Builds pinned secrets for the tests.
/// </summary>
internal static class SecretUtils
{
	#region Methods
	/// <summary>
	/// A secret holding a random string of the given length.
	/// </summary>
	public static PinnedSecret CreateRandomSecret(int length = 10) => CreateSecret(AppUtils.CreateRandomString(length));

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
