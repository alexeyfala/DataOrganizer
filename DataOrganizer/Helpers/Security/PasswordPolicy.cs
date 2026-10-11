using DataOrganizer.Dto.Encryption;
using DataOrganizer.Enums.Encryption;
using System;

namespace DataOrganizer.Helpers.Security;

/// <summary>
/// Rules a typed password and its confirmation have to follow.
/// </summary>
internal static class PasswordPolicy
{
	#region Methods
	/// <summary>
	/// Checks a password and its confirmation; the confirmation and the minimum length only weigh in while a new
	/// password is set.
	/// </summary>
	public static PasswordCheck Check(
		string? password,
		string? confirmation,
		bool isNewPassword,
		int minimumLength)
	{
		const char space = ' ';

		bool isAccepted = !string.IsNullOrWhiteSpace(password)
			&& !password.StartsWith(space)
			&& !password.EndsWith(space);

		bool isConfirmed = string.Equals(
			password,
			confirmation,
			StringComparison.Ordinal);

		bool isPasswordAccepted = isAccepted && (!isNewPassword || password!.Length >= minimumLength);

		return new()
		{
			IsConfirmationMismatched = isNewPassword
				&& confirmation is { Length: > 0 }
				&& !isConfirmed,
			IsPasswordAccepted = isPasswordAccepted,
			IsPasswordTooShort = isNewPassword
				&& password is { Length: > 0 }
				&& password.Length < minimumLength,
			IsValid = isPasswordAccepted && (!isNewPassword || isConfirmed),
			Strength = isNewPassword
				? PasswordStrengthEstimator.Estimate(password)
				: PasswordStrength.None
		};
	}
	#endregion
}
