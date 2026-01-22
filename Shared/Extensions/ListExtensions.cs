using System.Collections.Generic;

namespace Shared.Extensions;

public static class ListExtensions
{
	#region Methods
	/// <summary>
	/// Moves an element of a sequence to the beginning of a list.
	/// </summary>
	public static void MoveToTop<T>(this IList<T> sequence, int index)
	{
		T item = sequence[index];

		for (int i = index; i > 0; i--)
		{
			sequence[i] = sequence[i - 1];
		}

		sequence[0] = item;
	}
	#endregion
}
