using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using Entities.Abstract;
using Entities.Enums;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace DataOrganizer.DTO.Entities.Abstract;

/// <inheritdoc cref="ExplorerModelBase" />
[ObservableObject]
[DebuggerDisplay(
	$"{nameof(Id)} = {{{nameof(Id)}}}, " +
	$"{nameof(EntityType)} = {{{nameof(EntityType)}}}, " +
	$"{nameof(Name)} = {{{nameof(Name)}}}")]
public abstract partial class ExplorerModelBaseDto : EntityModelBaseDto, IName
{
	#region Properties
	/// <summary>
	/// Default empty for non-folder items; <see cref="FolderModelDto" /> overrides it.
	/// Lives on the base because the TreeDataTemplate ItemsSource binding can re-evaluate
	/// against a <see cref="FileModelDto" /> when a <c>TreeViewItem</c> is recycled.
	/// </summary>
	public virtual ObservableCollection<ExplorerModelBaseDto> Children { get; } = [];

	/// <inheritdoc cref="ExplorerModelBase.CreatedDate" />
	public required DateTime CreatedDate { get; init; }

	/// <inheritdoc cref="Enums.EncryptionStatus" />
	[ObservableProperty]
	public partial EncryptionStatus EncryptionStatus { get; set; }

	/// <inheritdoc cref="ExplorerModelBase.EntityType" />
	public required EntityType EntityType { get; init; }

	/// <inheritdoc cref="FolderModel.IsExpanded" />
	/// <remarks>
	/// Stays on the base as a virtual auto-property: the TreeView's TreeViewItem style binds
	/// IsExpanded for every container, so the property must resolve against <see cref="ExplorerModelBaseDto" />.
	/// <see cref="FolderModelDto" /> overrides it with the real observable implementation.
	/// </remarks>
	public virtual bool IsExpanded { get; set; }

	/// <inheritdoc cref="ExplorerModelBase.IsSelected" />
	public bool IsSelected { get; set; }

	/// <inheritdoc cref="ExplorerModelBase.Name" />
	[ObservableProperty]
	public partial string Name { get; set; } = string.Empty;

	/// <inheritdoc cref="ExplorerModelBase.Note" />
	[ObservableProperty]
	public partial string? Note { get; set; }

	/// <inheritdoc cref="ExplorerModelBase.Parent" />
	public FolderModelDto? Parent { get; set; }

	/// <inheritdoc cref="ExplorerModelBase.ParentId" />
	public Guid? ParentId { get; set; }

	/// <inheritdoc cref="ExplorerModelBase.UpdatedDate" />
	public required DateTime UpdatedDate { get; set; }
	#endregion

	#region Methods
	/// <summary>
	/// Returns <c>True</c> if any parent satisfies the condition.
	/// </summary>
	public bool AnyParent(Predicate<FolderModelDto> condition)
	{
		FolderModelDto? item = Parent;

		while (item is not null)
		{
			if (condition(item))
			{
				return true;
			}

			item = item.Parent;
		}

		return false;
	}

	/// <summary>
	/// Searches parent object by a condition.
	/// </summary>
	public FolderModelDto? FindParent(Predicate<FolderModelDto> condition)
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
	/// Return a sequence of <see cref="FolderModelDto" /> parent objects.
	/// </summary>
	public IEnumerable<FolderModelDto> GetAllParents()
	{
		FolderModelDto? item = Parent;

		while (item is not null)
		{
			yield return item;

			item = item.Parent;
		}
	}
	#endregion
}
