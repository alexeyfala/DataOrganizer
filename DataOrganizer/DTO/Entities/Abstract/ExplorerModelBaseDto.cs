using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using Entities.Abstract;
using Entities.Enums;
using Entities.Models;
using System;
using System.Collections.Generic;
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
	/// <inheritdoc cref="ExplorerModelBase.CreatedDate" />
	public required DateTime CreatedDate { get; init; }

	/// <inheritdoc cref="Enums.EncryptionStatus" />
	[ObservableProperty]
	public partial EncryptionStatus EncryptionStatus { get; set; }

	/// <inheritdoc cref="ExplorerModelBase.EntityType" />
	public required EntityType EntityType { get; init; }

	/// <inheritdoc cref="FolderModel.IsExpanded" />
	/// <remarks>
	/// Due to an error when binding a property to a TreeViewItem in XAML,
	/// I have to place the property in <see cref="ExplorerModelBase" /> instead of <see cref="FolderModelDto" />.
	/// </remarks>
	[ObservableProperty]
	public partial bool IsExpanded { get; set; }

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

	#region Partial
	/// <summary>
	/// Called when <see cref="IsExpanded" /> changes.
	/// </summary>
	partial void OnIsExpandedChanged(bool value)
	{
		if (this.Id == default || this is not FolderModelDto folder)
		{
			return;
		}

		WeakReferenceMessenger
			.Default
			.Send(new FolderExpandedChangedMessage(folder.Id, folder.IsExpanded));
	}
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
