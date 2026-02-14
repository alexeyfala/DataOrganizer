using DataOrganizer.DTO.Entities.Models;

namespace DataOrganizer.Abstract;

public abstract class ExecuteFileParametersBase
{
	#region Properties
	/// <summary>
	/// A contents of the file.
	/// </summary>
	public required byte[] Contents { get; set; }

	/// <summary>
	/// Encrypted password.
	/// </summary>
	public required byte[]? EncryptedPassword { get; init; }

	/// <inheritdoc cref="FileModelDto" />
	public required FileModelDto File { get; init; }
	#endregion
}
