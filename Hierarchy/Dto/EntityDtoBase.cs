using Entities.Models;
using System;

namespace Hierarchy.Dto;

/// <inheritdoc cref="EntityBase" />
public abstract class EntityDtoBase
{
	#region Properties
	/// <inheritdoc cref="EntityBase.Id" />
	public required Guid Id { get; init; }

	/// <inheritdoc cref="EntityBase.Index" />
	public required int Index { get; set; }
	#endregion
}
