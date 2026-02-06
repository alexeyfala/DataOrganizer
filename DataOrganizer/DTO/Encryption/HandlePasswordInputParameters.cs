using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Views;

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

	/// <summary>
	/// A reference to <see cref="PasswordBox" />.
	/// </summary>
	public required PasswordBox View { get; init; }
	#endregion
}
