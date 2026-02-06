using DataOrganizer.DTO.Entities.Models;

namespace DataOrganizer.DTO.Encryption;

/// <summary>
/// The parameters for password input for encryption/decryption files in folder.
/// </summary>
public class HandlePasswordInputParameters : TakeCryptPasswordParameters
{
	#region Properties
	/// <summary>
	/// A sequence to <see cref="FileModelDto" /> objects.
	/// </summary>
	public required FileModelDto[] Files { get; init; }
	#endregion
}
