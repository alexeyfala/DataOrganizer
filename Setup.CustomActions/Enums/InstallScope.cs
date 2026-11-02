namespace Setup.CustomActions.Enums;

/// <summary>
/// Installation context of a product: for all users of the machine or for one user.
/// </summary>
public enum InstallScope
{
	/// <summary>
	/// Installed for all users of the machine.
	/// </summary>
	PerMachine,

	/// <summary>
	/// Installed for one user.
	/// </summary>
	PerUser
}
