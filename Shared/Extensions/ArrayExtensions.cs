using System;

namespace Shared.Extensions;

public static class ArrayExtensions
{
	#region Methods
	/// <summary>
	/// <c>True</c> when <see cref="Array.Length" /> == 0.
	/// </summary>
	public static bool IsEmpty<T>(this T[] target) => target.Length == 0;

	/// <summary>
	/// <c>True</c> when <see cref="Array.Length" /> > 0.
	/// </summary>
	public static bool IsNotEmpty<T>(this T[] target) => target.Length > 0;
	#endregion
}
