using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;

namespace DataOrganizer.DTO.Encryption;

/// <summary>
/// The parameters for password input for encryption/decryption files in folder.
/// </summary>
public class HandlePasswordInputParameters
{
	#region Properties
	/// <inheritdoc cref="CryptoAction" />
	public required CryptoAction Action { get; init; }

	/// <summary>
	/// A sequence to <see cref="FileModelDto" /> objects.
	/// </summary>
	public required FileModelDto[] Files { get; init; }

	/// <inheritdoc cref="FolderModelDto" />
	public required FolderModelDto Folder { get; init; }
	#endregion
}
