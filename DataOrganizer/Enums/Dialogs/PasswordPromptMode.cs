namespace DataOrganizer.Enums;

/// <summary>
/// The purpose a password box is opened for.
/// </summary>
public enum PasswordPromptMode
{
	/// <summary>
	/// An existing password is entered and checked against the data.
	/// </summary>
	Verify,

	/// <summary>
	/// A new password is set, so it is confirmed and has to satisfy the policy.
	/// </summary>
	Create
}
