using Entities.Enums;

namespace DataOrganizer.DTO;

public sealed record EntityCreationResult(string Name, EntityType Type);
