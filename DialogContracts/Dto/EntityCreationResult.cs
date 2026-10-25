using Entities.Enums;

namespace DialogContracts.Dto;

public sealed record EntityCreationResult(string Name, EntityKind Type);
