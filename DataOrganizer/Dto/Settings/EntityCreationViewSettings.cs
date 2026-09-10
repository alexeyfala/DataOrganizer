namespace DataOrganizer.Dto.Settings;

/// <summary>
/// The state of the object creation dialog, kept between openings.
/// </summary>
public readonly struct EntityCreationViewSettings
{
	#region Properties
	/// <summary>
	/// <c>True</c> when "Dataset" is selected.
	/// </summary>
	public required bool IsDatasetSelected { get; init; }

	/// <summary>
	/// <c>True</c> when "File" is selected.
	/// </summary>
	public required bool IsFileSelected { get; init; }

	/// <summary>
	/// <c>True</c> when "Folder" is selected.
	/// </summary>
	public required bool IsFolderSelected { get; init; }

	/// <summary>
	/// The name typed for the new object.
	/// </summary>
	public required string Name { get; init; }
	#endregion
}
