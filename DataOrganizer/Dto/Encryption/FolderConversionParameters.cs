using DataOrganizer.Dto.Entities;
using DataOrganizer.Helpers.Security;

namespace DataOrganizer.Dto.Encryption;

/// <summary>
/// The values a conversion of the contents and the notes of a folder runs with.
/// </summary>
public sealed record FolderConversionParameters
{
	#region Properties
	/// <summary>
	/// The data encryption key of the folder.
	/// </summary>
	public required PinnedBuffer Dek { get; init; }

	/// <summary>
	/// <c>True</c> to encrypt, <c>False</c> to decrypt.
	/// </summary>
	public required bool Encrypt { get; init; }

	/// <summary>
	/// The files whose contents and notes are converted.
	/// </summary>
	public required FileDto[] Files { get; init; }

	/// <summary>
	/// The folder whose note is converted, along with the notes of its subfolders.
	/// </summary>
	public required FolderDto Folder { get; init; }
	#endregion
}
