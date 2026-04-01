using System;

namespace Shared.Extensions;

public static class ArrayExtensions
{
	#region Methods
	/// <summary>
	/// Returns <c>True</c> if the array is empty, that is, <see cref="Array.Length" /> == 0.
	/// </summary>
	public static bool IsEmpty<T>(this T[] target) => target.Length == 0;
	#endregion
}
