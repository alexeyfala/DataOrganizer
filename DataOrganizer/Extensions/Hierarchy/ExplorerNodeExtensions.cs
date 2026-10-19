using DataOrganizer.Dto.Entities;
using DataOrganizer.ViewModels.Hierarchy;
using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace DataOrganizer.Extensions.Hierarchy;

internal static class ExplorerNodeExtensions
{
	#region Methods
	/// <summary>
	/// Returns the row of an object; <c>null</c> when the rows show no such object.
	/// </summary>
	public static ExplorerNodeViewModelBase? FindNode(
		this IEnumerable<ExplorerNodeViewModelBase> nodes,
		ExplorerItemDtoBase model)
	{
		return nodes
			.FindPath(model)
			.LastOrDefault();
	}

	/// <summary>
	/// Returns the rows from the root down to the row of an object; empty when the rows show no such object.
	/// </summary>
	public static ExplorerNodeViewModelBase[] FindPath(
		this IEnumerable<ExplorerNodeViewModelBase> nodes,
		ExplorerItemDtoBase model)
	{
		List<ExplorerNodeViewModelBase> path = [];

		IEnumerable<ExplorerNodeViewModelBase> level = nodes;

		foreach (ExplorerItemDtoBase step in (ExplorerItemDtoBase[])[.. model.GetAllParents().Reverse(), model])
		{
			if (level.FirstOrDefault(x => x.Model == step) is not { } node)
			{
				return [];
			}

			path.Add(node);

			level = node.Children;
		}

		return [.. path];
	}

	/// <summary>
	/// Merges what <paramref name="selector" /> gives for each row of the tree at any depth, for as long as the row
	/// stays in the tree.
	/// </summary>
	public static IObservable<T> MergeManyInTree<T>(
		this ReadOnlyObservableCollection<ExplorerNodeViewModelBase> nodes,
		Func<ExplorerNodeViewModelBase, IObservable<T>> selector)
	{
		return nodes
			.ToObservableChangeSet()
			.MergeMany(x => x is FolderNodeViewModel folder
				? selector(x).Merge(folder.Children.MergeManyInTree(selector))
				: selector(x));
	}
	#endregion
}
