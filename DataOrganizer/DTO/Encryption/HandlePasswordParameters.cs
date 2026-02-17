using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;

namespace DataOrganizer.DTO.Encryption;

/// <summary>
/// The parameters for password input for encryption/decryption files in folder.
/// </summary>
public class HandlePasswordParameters
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

	#region Methods
	/// <summary>
	/// Creates <see cref="FolderEncryptionParameters" /> from <see cref="HandlePasswordParameters" /> and <paramref name="password"/>.
	/// </summary>
	public FolderEncryptionParameters CreateFrom(string password) => new()
	{
		Action = Action,
		Files = Files,
		Folder = Folder,
		Password = password
	};
	#endregion
}
