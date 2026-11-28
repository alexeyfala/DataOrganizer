using DataOrganizer.Database.Entities.Enums;

namespace DataOrganizer.Infrastructure.DialogContracts.Dto;

/// <summary>
/// The name and the kind of a new object entered by the user.
/// </summary>
/// <param name="Name">Name of the new object.</param>
/// <param name="Type">Kind of the new object.</param>
public sealed record EntityCreationResult(string Name, EntityKind Type);
