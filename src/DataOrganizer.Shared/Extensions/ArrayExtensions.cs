using System;

namespace DataOrganizer.Shared.Extensions;

/// <summary>
/// Tells whether an array holds any elements.
/// </summary>
public static class ArrayExtensions
{
	#region Methods
	/// <summary>
	/// <c>True</c> when <see cref="Array.Length" /> == 0.
	/// </summary>
	public static bool IsEmpty<T>(this T[] array) => array.Length == 0;

	/// <summary>
	/// <c>True</c> when <see cref="Array.Length" /> > 0.
	/// </summary>
	public static bool IsNotEmpty<T>(this T[] array) => array.Length > 0;
	#endregion
}
