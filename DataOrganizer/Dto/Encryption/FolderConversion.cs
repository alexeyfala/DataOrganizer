using Repository.Dto;

namespace DataOrganizer.Dto.Encryption;

/// <summary>
/// The contents and the notes of a folder in their converted form, next to the contents they came from.
/// </summary>
public sealed record FolderConversion
{
	#region Properties
	/// <summary>
	/// The contents in their converted form.
	/// </summary>
	public required ValidatedContents[] Converted { get; init; }

	/// <summary>
	/// The contents as they were read from the database.
	/// </summary>
	public required ValidatedContents[] Loaded { get; init; }

	/// <summary>
	/// The converted notes of the folder, of its subfolders and of its files.
	/// </summary>
	public required NoteUpdate[] Notes { get; init; }
	#endregion
}
