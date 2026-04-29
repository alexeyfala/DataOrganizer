using DataOrganizer.Abstract;

namespace DataOrganizer.DTO;

public sealed class TrackChangesParameters : ExecuteFileParametersBase
{
	#region Properties
	/// <summary>
	/// A path to the file.
	/// </summary>
	public required string FilePath { get; init; }
	#endregion
}
