using System;
using System.Security.Cryptography;

namespace DataOrganizer.TestSupport.Common;

/// <summary>
/// Failures of a cipher that a test feeds to the code under test.
/// </summary>
public static class CipherFailures
{
	#region Methods
	/// <summary>
	/// Failures an operation on the key of a session can end with.
	/// </summary>
	public static Exception[] Session() =>
	[
		new AuthenticationTagMismatchException(),
		new InvalidOperationException()
	];
	#endregion
}
