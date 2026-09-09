using System;

namespace Entities.Converters;

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
	public static T Read<T>(string? value, T fallback) where T : struct, Enum
	{
		return value is { Length: > 0 }
			&& !char.IsAsciiDigit(value[0])
			&& value[0] is not ('-' or '+')
			&& Enum.TryParse(value, out T result)
			&& string.Equals(result.ToString(), value, StringComparison.Ordinal)
				? result
				: fallback;
	}
	#endregion
}
