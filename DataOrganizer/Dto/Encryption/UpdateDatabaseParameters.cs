using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using Repository.Dto;

namespace DataOrganizer.Dto.Encryption;

public sealed record UpdateDatabaseParameters
{
	#region Properties
	/// <summary>
	/// Database backup file path.
	/// </summary>
	public required string BackupFilePath { get; init; }

	/// <summary>
	/// The sequence of contents.
	/// </summary>
	public required ValidatedContents[] Contents { get; init; }

	/// <inheritdoc cref="FolderDto.EncryptedDek" />
	public required byte[]? EncryptedDek { get; init; }

	/// <summary>
	/// A sequence to <see cref="FileDto" /> objects.
	/// </summary>
	public required FileDto[] Files { get; init; }

	/// <inheritdoc cref="FolderDto" />
	public required FolderDto Folder { get; init; }

	/// <summary>
	/// The new encryption status.
	/// </summary>
	public required EncryptionStatus NewStatus { get; init; }

	/// <summary>
	/// The processed notes of the folder and of its objects.
	/// </summary>
	public required NoteUpdate[] Notes { get; init; }
	#endregion
}
