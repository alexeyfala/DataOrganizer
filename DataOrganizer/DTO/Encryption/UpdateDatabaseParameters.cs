using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using Repository.DTO;

namespace DataOrganizer.DTO.Encryption;

public sealed class UpdateDatabaseParameters
{
	#region Properties
	/// <summary>
	/// Database backup file path.
	/// </summary>
	public required string BackupFilePath { get; init; }

	/// <summary>
	/// The sequence of contents.
	/// </summary>
	public required ContentsIsValidPair[] Contents { get; init; }

	/// <inheritdoc cref="FolderModelDto.EncryptedDek" />
	public required byte[]? EncryptedDek { get; init; }

	/// <summary>
	/// A sequence to <see cref="FileModelDto" /> objects.
	/// </summary>
	public required FileModelDto[] Files { get; init; }

	/// <inheritdoc cref="FolderModelDto" />
	public required FolderModelDto Folder { get; init; }

	/// <summary>
	/// The new encryption status.
	/// </summary>
	public required EncryptionStatus NewStatus { get; init; }

	/// <inheritdoc cref="FolderModelDto.PasswordHash" />
	public required string? PasswordHash { get; init; }
	#endregion
}
