using Entities.Models;

namespace Repository.Dto;

/// <summary>
/// The entities read out of a database file.
/// </summary>
public sealed class LoadedEntities
{
	#region Properties
	/// <summary>
	/// A flat sequence of <see cref="FileModel" />.
	/// </summary>
	public required FileModel[] Files { get; init; }

	/// <summary>
	/// A flat sequence of <see cref="FolderModel" />.
	/// </summary>
	public required FolderModel[] Folders { get; init; }
	#endregion
}
