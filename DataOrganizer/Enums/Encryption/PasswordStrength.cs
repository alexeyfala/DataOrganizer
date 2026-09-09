namespace DataOrganizer.Enums;

/// <summary>
/// How much guessing a password is estimated to withstand.
/// </summary>
public enum PasswordStrength
{
	/// <summary>
	/// Nothing has been typed yet, so there is nothing to rate.
	/// </summary>
	None,

	/// <summary>
	/// Falls to an offline guessing run within reach of a single machine.
	/// </summary>
	Weak,

	/// <summary>
	/// Holds against casual guessing, not against a determined offline run.
	/// </summary>
	Fair,

	/// <summary>
	/// Costs more than an offline run is usually worth.
	/// </summary>
	Strong,

	/// <summary>
	/// Out of reach of an offline run.
	/// </summary>
	VeryStrong
}
