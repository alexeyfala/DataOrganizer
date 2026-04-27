using DataOrganizer.DTO.Entities.Models;

namespace DataOrganizer.Abstract;

public abstract class ExecuteFileParametersBase
{
	#region Properties
	/// <summary>
	/// A contents of the file.
	/// </summary>
	public required byte[] Contents { get; set; }

	/// <inheritdoc cref="FileModelDto" />
	public required FileModelDto File { get; init; }

	/// <summary>
	/// Encrypted within the session DEK.
	/// </summary>
	public required byte[]? SessionEncryptedDek { get; set; }
	#endregion
}
