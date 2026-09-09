using Entities.Models;
using System;

namespace DataOrganizer.Dto.Entities;

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
