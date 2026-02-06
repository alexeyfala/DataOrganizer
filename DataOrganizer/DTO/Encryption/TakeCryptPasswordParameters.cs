using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;

namespace DataOrganizer.DTO.Encryption;

/// <summary>
/// The parameters for encryption/decryption files in folder.
/// </summary>
public class TakeCryptPasswordParameters
{
	#region Properties
	/// <inheritdoc cref="CryptoAction" />
	public required CryptoAction Action { get; init; }

	/// <inheritdoc cref="FolderModelDto" />
	public required FolderModelDto Folder {  get; init; }
	#endregion
}
