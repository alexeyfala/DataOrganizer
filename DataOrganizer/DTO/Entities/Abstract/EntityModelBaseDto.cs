using Entities.Abstract;
using Entities.Interfaces;
using System;

namespace DataOrganizer.DTO.Entities.Abstract;

/// <inheritdoc cref="EntityModelBase" />
public abstract class EntityModelBaseDto : IIdentity
{
	#region Properties
	/// <inheritdoc />
	public required Guid Id { get; init; }
	#endregion
}
