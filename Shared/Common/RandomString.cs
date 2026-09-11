using System;
using System.Linq;

namespace Shared.Common;

/// <summary>
/// Source of random strings.
/// </summary>
public static class RandomString
{
	#region Methods
	/// <summary>
	/// Generates a random string in upper case of the required length.
	/// </summary>
	public static string Create(int length)
	{
		const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

		char[] characters = [.. Enumerable
			.Repeat(alphabet, length)
			.Select(x => x[Random.Shared.Next(x.Length)])];

		return new string(characters);
	}
	#endregion
}
