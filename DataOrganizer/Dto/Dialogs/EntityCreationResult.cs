using Entities.Enums;

namespace DataOrganizer.Dto.Dialogs;

public sealed record EntityCreationResult(string Name, EntityKind Type);
