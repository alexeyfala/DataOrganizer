namespace DataOrganizer.DTO.Settings;

public readonly struct EntityCreationViewSettings
{
	#region Properties
	/// <summary>
	/// Returns <c>True</c> if "Dataset" is selected.
	/// </summary>
	public required bool IsDatasetSelected { get; init; }

	/// <summary>
	/// Returns <c>True</c> if "File" is selected.
	/// </summary>
	public required bool IsFileSelected { get; init; }

	/// <summary>
	/// Returns <c>True</c> if "Folder" is selected.
	/// </summary>
	public required bool IsFolderSelected { get; init; }

	/// <summary>
	/// Name.
	/// </summary>
	public required string Name { get; init; }
	#endregion
}
