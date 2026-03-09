using Entities.Models;

namespace Repository.DTO;

public sealed class LoadFromDbResult
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
