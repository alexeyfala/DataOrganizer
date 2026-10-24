using Entities.Models;
using Hierarchy.Extensions;
using Hierarchy.Interfaces;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hierarchy.Dto;

/// <inheritdoc cref="FolderEntity" />
public sealed class FolderDto : ExplorerItemDtoBase, IPasswordKeeper
{
	#region Properties
	/// <inheritdoc cref="FolderEntity.Children" />
	public ObservableCollection<ExplorerItemDtoBase> Children { get; } = [];

	/// <inheritdoc />
	public byte[]? EncryptedDek { get; set; }
	#endregion

	#region Methods
	/// <summary>
	/// <c>True</c> when any child satisfies the condition.
	/// </summary>
	public bool AnyChild(Predicate<ExplorerItemDtoBase> condition)
	{
		return Children
			.Flatten()
			.Any(x => condition(x));
	}

	/// <inheritdoc />
	/// <remarks>
	/// A folder protects its own contents as well, hence the check of the folder itself.
	/// </remarks>
	public override FolderDto? FindPasswordKeeper() => IsPasswordKeeper() ? this : base.FindPasswordKeeper();

	/// <summary>
	/// Returns a flat sequence of all child objects.
	/// </summary>
	public IEnumerable<ExplorerItemDtoBase> GetAllChildren() => Children.Flatten();

	/// <summary>
	/// <c>True</c> when <see cref="EncryptedDek" /> has a value.
	/// </summary>
	public bool IsPasswordKeeper() => EncryptedDek?.IsNotEmpty() ?? false;

	/// <summary>
	/// Returns the folder itself and its immediate subfolders as one sequence.
	/// </summary>
	public IEnumerable<ExplorerItemDtoBase> WithSubfolders()
	{
		return this
			.ToEnumerable()
			.Concat(Children.GetFolders());
	}
	#endregion
}
