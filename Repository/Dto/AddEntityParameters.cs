using Entities.Enums;
using Entities.Models;
using System;

namespace Repository.Dto;

public readonly struct AddEntityParameters
{
	#region Properties
	/// <inheritdoc cref="FileEntity.Contents" />
	public byte[]? FileContents { get; init; }

	/// <inheritdoc cref="ExplorerItemBase.Index" />
	public required int Index { get; init; }

	/// <inheritdoc cref="ExplorerItemBase.Kind" />
	public required EntityKind Kind { get; init; }

	/// <inheritdoc cref="ExplorerItemBase.Name" />
	public required string Name { get; init; }

	/// <inheritdoc cref="ExplorerItemBase.ParentId" />
	public required Guid? ParentId { get; init; }
	#endregion
}
