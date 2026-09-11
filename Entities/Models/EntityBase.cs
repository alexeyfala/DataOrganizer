using System;

namespace Entities.Models;

/// <summary>
/// The base of every entity stored in the database.
/// </summary>
public abstract class EntityBase
{
	#region Properties
	/// <summary>
	/// Identifier.
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	/// Position of the object among its siblings.
	/// </summary>
	public int Index { get; set; }
	#endregion
}
