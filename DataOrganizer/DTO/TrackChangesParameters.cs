using DataOrganizer.Abstract;

namespace DataOrganizer.DTO;

public sealed class TrackChangesParameters : ExecuteFileParametersBase
{
	#region Properties
	/// <summary>
	/// File name.
	/// </summary>
	public required string FileName { get; init; }

	/// <summary>
	/// Path to the file.
	/// </summary>
	public required string FilePath { get; init; }
	#endregion
}
