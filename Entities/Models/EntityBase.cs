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
	/// The index of the object in the parent collection (necessary for correct positioning in the collection).
	/// </summary>
	public int Index { get; set; }
	#endregion
}
