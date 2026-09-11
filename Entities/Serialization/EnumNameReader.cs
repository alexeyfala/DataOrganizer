using System;

namespace Entities.Serialization;

/// <summary>
/// Reads an enum value that is stored as a name.
/// </summary>
public static class EnumNameReader
{
	#region Methods
	/// <summary>
	/// Reads a stored name, accepting only the exact text that a written name has,
	/// and returns <paramref name="fallback"/> for anything else.
	/// </summary>
	public static T Read<T>(string? name, T fallback) where T : struct, Enum
	{
		return name is { Length: > 0 }
			&& !char.IsAsciiDigit(name[0])
			&& name[0] is not ('-' or '+')
			&& Enum.TryParse(name, out T result)
			&& string.Equals(result.ToString(), name, StringComparison.Ordinal)
				? result
				: fallback;
	}
	#endregion
}
