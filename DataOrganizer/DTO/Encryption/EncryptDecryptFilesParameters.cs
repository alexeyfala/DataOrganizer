namespace DataOrganizer.DTO.Encryption;

public sealed class EncryptDecryptFilesParameters : HandlePasswordInputParameters
{
	#region Properties
	/// <summary>
	/// Password.
	/// </summary>
	public required string Password { get; init; }
	#endregion
}
