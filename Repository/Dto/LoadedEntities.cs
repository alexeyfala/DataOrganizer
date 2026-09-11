using Entities.Models;

namespace Repository.Dto;

/// <summary>
/// The entities read out of a database file.
/// </summary>
public sealed class LoadedEntities
{
	#region Properties
	/// <summary>
	/// A flat sequence of <see cref="FileEntity" />.
	/// </summary>
	public required FileEntity[] Files { get; init; }

	/// <summary>
	/// A flat sequence of <see cref="FolderEntity" />.
	/// </summary>
	public required FolderEntity[] Folders { get; init; }
	#endregion
}
