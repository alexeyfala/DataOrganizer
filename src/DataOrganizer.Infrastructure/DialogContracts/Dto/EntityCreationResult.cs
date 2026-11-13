using DataOrganizer.Database.Entities.Enums;

namespace DataOrganizer.Infrastructure.DialogContracts.Dto;

/// <summary>
/// The name and the kind of a new object entered by the user.
/// </summary>
public sealed record EntityCreationResult(string Name, EntityKind Type);
