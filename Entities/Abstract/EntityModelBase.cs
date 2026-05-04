using System;

namespace Entities.Abstract;

/// <summary>
/// The base model for all entities.
/// </summary>
public abstract class EntityModelBase
{
	#region Properties
	/// <inheritdoc />
	public Guid Id { get; set; }

	/// <summary>
	/// The index of the object in the parent collection (necessary for correct positioning in the collection).
	/// </summary>
	public int Index { get; set; }
	#endregion
}
