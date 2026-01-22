using Avalonia;
using Avalonia.Controls;
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
	public static IEnumerable<Visual> FindAllParents(this Visual element)
	{
		Visual? parent = element.GetVisualParent();

		while (parent is not null)
		{
			yield return parent;

			parent = parent.GetVisualParent();
		}
	}

	/// <summary>
	/// Finds a parent object by condition.
	/// </summary>
	public static T? FindParent<T>(this StyledElement? element, Predicate<T> condition) where T : class
	{
		StyledElement? current = element;

		while (current is not null)
		{
			if (current is T control && condition(control))
			{
				return control;
			}

			current = current.Parent;
		}

		return null;
	}

	/// <summary>
	/// Finds the first visual child of a specific type within the visual tree.
	/// </summary>
	/// <typeparam name="T">The type of the visual child to find.</typeparam>
	/// <param name="parent">The parent visual to start the search from.</param>
	/// <returns>The first visual child of the specified type, or null if not found.</returns>
	public static T? FindVisualChild<T>(this Visual parent) where T : class
	{
		foreach (Visual child in parent.GetVisualChildren())
		{
			if (child is T found)
			{
				return found;
			}

			if (FindVisualChild<T>(child) is { } subChild)
			{
				return subChild;
			}
		}

		return null;
	}

	/// <summary>
	/// Finds all visual children of a specific type within the visual tree.
	/// </summary>
	/// <typeparam name="T">The type of the visual children to find.</typeparam>
	/// <param name="parent">The parent visual to start the search from.</param>
	/// <returns>An enumerable collection of all visual children of the specified type.</returns>
	public static IEnumerable<T> FindVisualChildren<T>(this Visual parent) where T : class
	{
		foreach (Visual child in parent.GetVisualChildren())
		{
			if (child is T found)
			{
				yield return found;
			}

			foreach (T subChild in FindVisualChildren<T>(child))
			{
				yield return subChild;
			}
		}
	}

	/// <summary>
	/// Finds the first visual parent of a specific type recursively in the visual tree.
	/// </summary>
	public static T? FindVisualParent<T>(this Visual element) where T : class
	{
		Visual? parent = element.GetVisualParent();

		while (parent is not null)
		{
			if (parent is T found)
			{
				return found;
			}

			parent = parent.GetVisualParent();
		}

		return null;
	}

	/// <summary>
	/// <inheritdoc cref="FindVisualParent{T}(Visual)" /> With condition.
	/// </summary>
	public static T? FindVisualParent<T>(
		this Visual element,
		Predicate<T> condition) where T : class
	{
		Visual? parent = element.GetVisualParent();

		while (parent is not null)
		{
			if (parent is T found && condition(found))
			{
				return found;
			}

			parent = parent.GetVisualParent();
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
		if (element.FindVisualChild<VirtualizingStackPanel>() is not { } panel)
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
