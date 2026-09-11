using System.Collections.Generic;

namespace Shared.Extensions;

public static class ListExtensions
{
	#region Methods
	/// <summary>
	/// Moves an element to the beginning of a list.
	/// </summary>
	public static void MoveToTop<T>(this IList<T> list, int index)
	{
		T item = list[index];

		for (int i = index; i > 0; i--)
		{
			list[i] = list[i - 1];
		}

		list[0] = item;
	}
	#endregion
}
