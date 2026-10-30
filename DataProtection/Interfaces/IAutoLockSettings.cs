namespace DataProtection.Interfaces;

/// <summary>
/// Delay of the auto-lock chosen in the settings.
/// </summary>
public interface IAutoLockSettings
{
	#region Properties
	/// <summary>
	/// Minutes of inactivity after which the decrypted contents are hidden; <c>0</c> disables the auto-lock.
	/// </summary>
	int AutoLockMinutes { get; }
	#endregion
}
