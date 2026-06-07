using Entities.Models;
using System;

namespace DataOrganizer.DTO.Entities.Models;

/// <inheritdoc cref="EntityModelBase" />
public abstract class EntityModelBaseDto
{
	#region Properties
	/// <inheritdoc />
	public required Guid Id { get; init; }

	/// <inheritdoc cref="EntityModelBase.Index" />
	public required int Index { get; set; }
	#endregion
}
