namespace DataOrganizer.DTO.Encryption;

public sealed class FolderEncryptionParameters : HandlePasswordParameters
{
	#region Properties
	/// <summary>
	/// Password.
	/// </summary>
	public required string Password { get; init; }
	#endregion	
}
