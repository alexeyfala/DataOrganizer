using Entities.Enums;
using Entities.Models;
using System;

namespace Repository.Dto;

public readonly struct AddEntityParameters
{
	#region Properties
	/// <inheritdoc cref="ExplorerModelBase.EntityType" />
	public required EntityKind EntityType { get; init; }

	/// <inheritdoc cref="FileModel.Contents" />
	public byte[]? FileContents { get; init; }

	/// <inheritdoc cref="ExplorerModelBase.Index" />
	public required int Index { get; init; }

	/// <inheritdoc cref="ExplorerModelBase.Name" />
	public required string Name { get; init; }

	/// <inheritdoc cref="ExplorerModelBase.ParentId" />
	public required Guid? ParentId { get; init; }
	#endregion
}
