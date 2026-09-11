using Shared.Common;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestSupport.Common;

/// <summary>
/// Factory methods that build random primitive values.
/// </summary>
public static class RandomValues
{
	#region Methods
	/// <summary>
	/// Generates a random sequence of bytes.
	/// </summary>
	public static byte[] CreateBytes(int length)
	{
		byte[] buffer = new byte[length];

		Random
			.Shared
			.NextBytes(buffer);

		return buffer;
	}

	/// <summary>
	/// Generates a random directory name.
	/// </summary>
	public static string CreateDirectoryName()
	{
		return $"{RandomString.Create(6)}_directory";
	}

	/// <summary>
	/// Generates a random <see cref="double" /> number within a given range.
	/// </summary>
	/// <remarks>
	/// <see href="https://code-maze.com/csharp-random-double-range" />
	/// </remarks>
	public static double CreateDouble(double minValue, double maxValue)
	{
		double value = Random
			.Shared
			.NextDouble();

		return minValue + (value * (maxValue - minValue));
	}

	/// <summary>
	/// Generates a random <see cref="Enum" /> value.
	/// </summary>
	public static T CreateEnumValue<T>() where T : struct, Enum
	{
		T[] values = Enum.GetValues<T>();

		int randomIndex = Random
			.Shared
			.Next(values.Length);

		return (T)values.GetValue(randomIndex)!;
	}

	/// <summary>
	/// Returns a random enum value other than <paramref name="excluded" />.
	/// </summary>
	public static T CreateEnumValueExcept<T>(T excluded) where T : Enum
	{
		T[] filtered = [.. Enum
			.GetValues(typeof(T))
			.Cast<T>()
			.Where(value => !EqualityComparer<T>.Default.Equals(value, excluded))];

		if (filtered.IsEmpty())
		{
			throw new InvalidOperationException("No enum values available to select after exclusion.");
		}

		int index = Random
			.Shared
			.Next(0, filtered.Length);

		return filtered[index];
	}

	/// <summary>
	/// Generates a random file name with the given extension.
	/// </summary>
	public static string CreateFileName(int length, string extension)
	{
		return $"{RandomString.Create(length)}_file{extension}";
	}

	/// <summary>
	/// Generates a random file name with a random extension.
	/// </summary>
	public static string CreateFileName(int length)
	{
		return $"{RandomString.Create(length)}_file.{RandomString.Create(3).ToLower()}";
	}

	/// <summary>
	/// Creates the required number of random <see cref="Guid" /> objects.
	/// </summary>
	public static IEnumerable<Guid> CreateGuids(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return Guid.NewGuid();
		}
	}

	/// <summary>
	/// Generates a random <see cref="int" /> number within a given range.
	/// </summary>
	public static int CreateInt(int minValue, int maxValue)
	{
		return Random
			.Shared
			.Next(minValue, maxValue);
	}

	/// <summary>
	/// Generates a random number between 10 and 100.
	/// </summary>
	public static int CreateIntFrom10To100() => CreateInt(10, 101);
	#endregion
}
