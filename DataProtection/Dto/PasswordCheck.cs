using DataProtection.Enums;

namespace DataProtection.Dto;

/// <summary>
/// Outcome of checking a password and its confirmation against the policy.
/// </summary>
public sealed record PasswordCheck
{
	#region Properties
	/// <summary>
	/// <c>True</c> while the confirmation holds something other than the password.
	/// </summary>
	public required bool IsConfirmationMismatched { get; init; }

	/// <summary>
	/// <c>True</c> when the password alone satisfies the policy, the confirmation aside.
	/// </summary>
	public required bool IsPasswordAccepted { get; init; }

	/// <summary>
	/// <c>True</c> while the password holds fewer characters than the minimum length.
	/// </summary>
	public required bool IsPasswordTooShort { get; init; }

	/// <summary>
	/// <c>True</c> when the password and its confirmation both pass.
	/// </summary>
	public required bool IsValid { get; init; }

	/// <summary>
	/// Rating of a new password; <see cref="PasswordStrength.None" /> while an existing password is entered.
	/// </summary>
	public required PasswordStrength Strength { get; init; }
	#endregion
}
