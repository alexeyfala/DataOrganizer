using Entities.Enums;

namespace DataOrganizer.Dto;

public sealed record EntityCreationResult(string Name, EntityType Type);
