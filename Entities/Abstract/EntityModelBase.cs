using Entities.Interfaces;
using System;

namespace Entities.Abstract;

/// <summary>
/// The base model for all entities.
/// </summary>
public abstract class EntityModelBase : IIdentity
{
	#region Properties
	/// <inheritdoc />
	public Guid Id { get; init; }
	#endregion
}
