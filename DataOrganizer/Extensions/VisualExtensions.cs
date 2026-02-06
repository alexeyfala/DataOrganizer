using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Extensions;

internal static class VisualExtensions
{
	#region Methods
	/// <summary>
	/// Finds all parents in the visual tree.
	/// </summary>
	public static IEnumerable<Visual> FindAllVisualParents(this Visual element)
	{
		Visual? item = element.GetVisualParent();

		while (item is not null)
		{
			yield return item;

			item = item.GetVisualParent();
		}
	}

	/// <summary>
	/// Finds the first logical child of a specific type within the logical tree.
	/// </summary>
	public static T? FindLogicalChild<T>(this ILogical parent) where T : class
	{
		Stack<ILogical> stack = new(parent.GetLogicalChildren());

		while (stack.Count > 0)
		{
			ILogical item = stack.Pop();

			if (item is T found)
			{
				return found;
			}

			foreach (ILogical child in item.GetLogicalChildren())
			{
				stack.Push(child);
			}
		}

		return null;
	}

	/// <summary>
	/// Finds a parent object by condition.
	/// </summary>
	public static T? FindLogicalParent<T>(this StyledElement? element, Predicate<T> condition) where T : class
	{
		StyledElement? item = element?.Parent;

		while (item is not null)
		{
			if (item is T parent && condition(parent))
			{
				return parent;
			}

			item = item.Parent;
		}

		return null;
	}

	/// <summary>
	/// Finds a parent object by type.
	/// </summary>
	public static T? FindLogicalParent<T>(this StyledElement? element) where T : class
	{
		StyledElement? item = element?.Parent;

		while (item is not null)
		{
			if (item is T found)
			{
				return found;
			}

			item = item.Parent;
		}

		return null;
	}

	/// <summary>
	/// Finds the first visual child of a specific type within the visual tree.
	/// </summary>
	public static T? FindVisualChild<T>(this Visual parent) where T : class
	{
		Stack<Visual> stack = new(parent.GetVisualChildren());

		while (stack.Count > 0)
		{
			Visual item = stack.Pop();

			if (item is T found)
			{
				return found;
			}

			foreach (Visual child in item.GetVisualChildren())
			{
				stack.Push(child);
			}
		}

		return null;
	}

	/// <summary>
	/// Finds all visual children of a specific type within the visual tree.
	/// </summary>
	public static IEnumerable<T> FindVisualChildren<T>(this Visual parent) where T : class
	{
		Stack<Visual> stack = new(parent.GetVisualChildren());

		while (stack.Count > 0)
		{
			Visual item = stack.Pop();

			if (item is T found)
			{
				yield return found;
			}

			foreach (Visual child in item.GetVisualChildren())
			{
				stack.Push(child);
			}
		}
	}

	/// <summary>
	/// Finds the first visual parent of a specific type in the visual tree.
	/// </summary>
	public static T? FindVisualParent<T>(this Visual element) where T : class
	{
		Visual? item = element.GetVisualParent();

		while (item is not null)
		{
			if (item is T found)
			{
				return found;
			}

			item = item.GetVisualParent();
		}

		return null;
	}

	/// <summary>
	/// Finds the <see cref="" /> within the visual tree and waits until it is loaded.
	/// </summary>
	public static async Task<bool> WaitVirtualizingStackPanelIsLoadedAsync(
		this Visual element,
		CancellationToken token = default)
	{
		if (FindVisualChild<VirtualizingStackPanel>(element) is not { } panel)
		{
			return false;
		}

		Func<bool> condition = () => panel.IsLoaded;

		await condition
			.WaitAsync(300, 10, token)
			.ConfigureAwait(true);

		return panel.IsLoaded;
	}
	#endregion
}
