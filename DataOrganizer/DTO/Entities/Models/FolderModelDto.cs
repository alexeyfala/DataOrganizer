using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.DTO.Entities.Abstract;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataOrganizer.DTO.Entities.Models;

/// <inheritdoc cref="FolderModel" />
public sealed partial class FolderModelDto : ExplorerModelBaseDto
{
	#region Properties
	/// <inheritdoc cref="FolderModel.Children" />
	public ObservableCollection<ExplorerModelBaseDto> Children { get; } = [];

	/// <inheritdoc cref="FolderModel.PasswordHash" />
	public string? PasswordHash { get; set; }
	#endregion

	#region Auto-Generated Properties
	/// <inheritdoc cref="FolderModel.IsExpanded" />
	[ObservableProperty]
	private bool _isExpanded;
	#endregion

	#region Events
	/// <summary>
	/// Occurs when <see cref="IsExpanded" /> changes.
	/// </summary>
	public static event EventHandler<FolderModelDto>? IsExpandedChanged;
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="IsExpanded" /> changes.
	/// </summary>
	partial void OnIsExpandedChanged(bool value)
	{
		if (this.Id == default)
		{
			return;
		}

		IsExpandedChanged?.Invoke(this, this);
	}
	#endregion

	#region Methods
	/// <summary>
	/// Returns <c>True</c> if any child satisfies the condition.
	/// </summary>
	public bool AnyChild(Func<ExplorerModelBaseDto, bool> condition)
	{
		ExplorerModelBaseDto[] children = [.. Children];

		while (children.Length > 0)
		{
			if (children.Any(condition))
			{
				return true;
			}

			children = [.. children
				.OfType<FolderModelDto>()
				.SelectMany(x => x.Children)];
		}

		return false;
	}

	/// <summary>
	/// Searches a parent by condition.
	/// </summary>
	public FolderModelDto? FindParent(Func<FolderModelDto, bool> condition)
	{
		FolderModelDto? item = Parent;

		while (item is not null)
		{
			if (condition(item))
			{
				return item;
			}

			item = item.Parent;
		}

		return null;
	}

	/// <summary>
	/// Returns a flat sequence of all child objects.
	/// </summary>
	public IEnumerable<ExplorerModelBaseDto> GetAllChildren()
	{
		ExplorerModelBaseDto[] children = [.. Children];

		while (children.Length > 0)
		{
			foreach (ExplorerModelBaseDto child in children)
			{
				yield return child;
			}

			children = [.. children
				.OfType<FolderModelDto>()
				.SelectMany(x => x.Children)];
		}
	}
	#endregion
}
