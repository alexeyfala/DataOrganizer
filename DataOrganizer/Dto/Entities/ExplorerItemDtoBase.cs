using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces;
using Entities.Enums;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace DataOrganizer.Dto.Entities;

/// <inheritdoc cref="ExplorerItemBase" />
[ObservableObject]
[DebuggerDisplay(
	$"{nameof(Id)} = {{{nameof(Id)}}}, " +
	$"{nameof(Kind)} = {{{nameof(Kind)}}}, " +
	$"{nameof(Name)} = {{{nameof(Name)}}}")]
public abstract partial class ExplorerItemDtoBase : EntityDtoBase, INamed
{
	#region Properties
	/// <summary>
	/// Default empty for non-folder items; <see cref="FolderDto" /> overrides it.
	/// Lives on the base because the TreeDataTemplate ItemsSource binding can re-evaluate
	/// against a <see cref="FileDto" /> when a <c>TreeViewItem</c> is recycled.
	/// </summary>
	public virtual ObservableCollection<ExplorerItemDtoBase> Children { get; } = [];

	/// <inheritdoc cref="ExplorerItemBase.CreatedAt" />
	public required DateTime CreatedAt { get; init; }

	/// <inheritdoc cref="Enums.Encryption.EncryptionStatus" />
	[ObservableProperty]
	public partial EncryptionStatus EncryptionStatus { get; set; }

	/// <inheritdoc cref="FolderEntity.IsExpanded" />
	/// <remarks>
	/// Stays on the base as a virtual auto-property: the TreeView's TreeViewItem style binds
	/// IsExpanded for every container, so the property must resolve against <see cref="ExplorerItemDtoBase" />.
	/// <see cref="FolderDto" /> overrides it with the real observable implementation.
	/// </remarks>
	public virtual bool IsExpanded { get; set; }

	/// <inheritdoc cref="ExplorerItemBase.IsSelected" />
	public bool IsSelected { get; set; }

	/// <inheritdoc cref="ExplorerItemBase.Kind" />
	public required EntityKind Kind { get; init; }

	/// <inheritdoc cref="ExplorerItemBase.Name" />
	[ObservableProperty]
	public partial string Name { get; set; } = string.Empty;

	/// <inheritdoc cref="ExplorerItemBase.Note" />
	[ObservableProperty]
	public partial byte[]? Note { get; set; }

	/// <inheritdoc cref="ExplorerItemBase.Parent" />
	public FolderDto? Parent { get; set; }

	/// <inheritdoc cref="ExplorerItemBase.ParentId" />
	public Guid? ParentId { get; set; }

	/// <inheritdoc cref="ExplorerItemBase.UpdatedAt" />
	public required DateTime UpdatedAt { get; set; }
	#endregion

	#region Methods
	/// <summary>
	/// <c>True</c> when any parent satisfies the condition.
	/// </summary>
	public bool AnyParent(Predicate<FolderDto> condition)
	{
		FolderDto? item = Parent;

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
	public FolderDto? FindParent(Predicate<FolderDto> condition)
	{
		FolderDto? item = Parent;

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
	/// Searches the password keeper the object belongs to; <c>null</c> when there is none.
	/// </summary>
	public virtual FolderDto? FindPasswordKeeper() => FindParent(x => x.IsPasswordKeeper());

	/// <summary>
	/// Returns a sequence of <see cref="FolderDto" /> parent objects.
	/// </summary>
	public IEnumerable<FolderDto> GetAllParents()
	{
		FolderDto? item = Parent;

		while (item is not null)
		{
			yield return item;

			item = item.Parent;
		}
	}
	#endregion
}
