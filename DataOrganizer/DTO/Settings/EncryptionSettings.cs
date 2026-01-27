namespace DataOrganizer.DTO.Settings;

/// <summary>
/// Encryption settings.
/// </summary>
internal readonly struct EncryptionSettings
{
	#region Properties
	/// <summary>
	/// Path to the file with the master password.
	/// </summary>
	public string? MasterPasswordFilePath { get; init; }
	#endregion
}
