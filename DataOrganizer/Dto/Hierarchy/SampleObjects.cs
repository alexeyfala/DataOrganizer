using Entities.Models;
using System;

namespace DataOrganizer.Dto.Hierarchy;

/// <summary>
/// Objects of a run of samples, with the folders to put under a password once they are saved and the files to damage
/// after that.
/// </summary>
internal sealed record SampleObjects
{
	#region Properties
	/// <summary>
	/// Identifiers of the encrypted files whose contents are to be damaged.
	/// </summary>
	public required Guid[] DamagedFileIds { get; init; }

	/// <summary>
	/// Objects of the run, each after its folder.
	/// </summary>
	public required ExplorerItemBase[] Items { get; init; }

	/// <summary>
	/// Identifiers of the folders to encrypt.
	/// </summary>
	public required Guid[] KeeperIds { get; init; }
	#endregion
}
