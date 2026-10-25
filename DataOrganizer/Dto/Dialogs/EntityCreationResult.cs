using Entities.Enums;

namespace DataOrganizer.Dto.Dialogs;

/// <summary>
/// The name and the kind of a new object entered by the user.
/// </summary>
public sealed record EntityCreationResult(string Name, EntityKind Type);
