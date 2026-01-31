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
	/// Return a sequence of <see cref="FolderModelDto" /> parent objects.
	/// </summary>
	public IEnumerable<FolderModelDto> GetParents()
	{
		if (Parent is null)
		{
			yield break;
		}

		yield return Parent;

		foreach (FolderModelDto parent in Parent.GetParents())
		{
			yield return parent;
		}
	}
	#endregion
}
