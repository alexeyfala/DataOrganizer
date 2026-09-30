using Entities.Models;
using System;

namespace DataOrganizer.Dto.Hierarchy;

/// <summary>
/// Objects of a run of samples, with the folders to put under a password once they are saved.
/// </summary>
/// <param name="Items">Objects of the run, each after its folder.</param>
/// <param name="KeeperIds">Identifiers of the folders to encrypt.</param>
internal sealed record SampleObjects(
	ExplorerItemBase[] Items,
	Guid[] KeeperIds);
