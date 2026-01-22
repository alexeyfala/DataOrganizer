using System;

namespace Entities.Interfaces;

/// <summary>
/// Defines an entity with the <see cref="Id" /> property.
/// </summary>
public interface IIdentity
{
	#region Properties
	/// <summary>
	/// Identifier.
	/// </summary>
	Guid Id { get; }
	#endregion Properties
}
