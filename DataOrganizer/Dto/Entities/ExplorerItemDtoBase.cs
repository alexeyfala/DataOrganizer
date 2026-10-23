using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces.Hierarchy;
using Entities.Enums;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
	/// <inheritdoc cref="ExplorerItemBase.CreatedAt" />
	public required DateTime CreatedAt { get; init; }

	/// <inheritdoc cref="Enums.Encryption.EncryptionStatus" />
	[ObservableProperty]
	public partial EncryptionStatus EncryptionStatus { get; set; }

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
	/// Searches parent object by a condition.
	/// </summary>
	public FolderDto? FindParent(Predicate<FolderDto> condition)
	{
		return GetAllParents().FirstOrDefault(x => condition(x));
	}

	/// <summary>
	/// Searches the password keeper the object belongs to; <c>null</c> when there is none.
	/// </summary>
	public virtual FolderDto? FindPasswordKeeper() => FindParent(x => x.IsPasswordKeeper());

	/// <summary>
	/// Searches the password keeper whose session key opens the object now; <c>null</c> when the object is not decrypted
	/// or has no keeper.
	/// </summary>
	public FolderDto? FindUnlockedKeeper()
	{
		return EncryptionStatus == EncryptionStatus.Decrypted
			? FindPasswordKeeper()
			: null;
	}

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
