namespace DataOrganizer.DTO.Encryption;

public sealed class EncryptDecryptFolderParameters : HandlePasswordInputParameters
{
	#region Properties
	/// <summary>
	/// Password.
	/// </summary>
	public required string Password { get; init; }
	#endregion	
}
