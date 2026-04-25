using Entities.Enums;

namespace DataOrganizer.DTO;

public sealed class EntityCreationResult
{
	#region Properties
	/// <summary>
	/// Name.
	/// </summary>
	public required string Name { get; init; }

	/// <inheritdoc cref="EntityType" />
	public required EntityType Type { get; init; }
	#endregion
}
