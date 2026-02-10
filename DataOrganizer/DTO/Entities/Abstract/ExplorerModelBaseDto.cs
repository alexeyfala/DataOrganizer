using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using Entities.Abstract;
using Entities.Enums;
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

	/// <inheritdoc cref="ExplorerModelBase.EntityType" />
	public required EntityType EntityType { get; init; }

	/// <inheritdoc cref="ExplorerModelBase.Index" />
	public required int Index { get; set; }

	/// <inheritdoc cref="ExplorerModelBase.IsSelected" />
	public bool IsSelected { get; set; }

	/// <inheritdoc cref="ExplorerModelBase.Parent" />
	public FolderModelDto? Parent { get; set; }

	/// <inheritdoc cref="ExplorerModelBase.ParentId" />
	public Guid? ParentId { get; set; }

	/// <inheritdoc cref="ExplorerModelBase.UpdatedDate" />
	public required DateTime UpdatedDate { get; set; }
	#endregion

	#region Auto-Generated Properties
	/// <inheritdoc cref="Enums.EncryptionStatus" />
	[ObservableProperty]
	private EncryptionStatus _encryptionStatus;

	/// <inheritdoc cref="ExplorerModelBase.Name" />
	[ObservableProperty]
	private string _name = string.Empty;

	/// <inheritdoc cref="ExplorerModelBase.Note" />
	[ObservableProperty]
	private string? _note;
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
