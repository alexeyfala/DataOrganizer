using AwesomeAssertions;
using DataProtection.Dto;
using DataProtection.Enums;
using DataProtection.Helpers;

namespace DataProtection.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(PasswordPolicy)}"" type")]
internal class PasswordPolicyTests
{
	#region Data
	/// <summary>
	/// Least length of a new password in these tests.
	/// </summary>
	private const int MinimumLength = 8;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="PasswordPolicy.Check" />: a new password is accepted before it is confirmed, but is not valid yet.
	/// </summary>
	[Test]
	public void Check_Accepts_A_New_Password_While_The_Confirmation_Is_Empty()
	{
		// Act
		PasswordCheck result = PasswordPolicy.Check(
			"password",
			string.Empty,
			isNewPassword: true,
			MinimumLength);

		// Assert
		result
			.Should()
			.Be(new PasswordCheck
			{
				IsConfirmationMismatched = false,
				IsPasswordAccepted = true,
				IsPasswordTooShort = false,
				IsValid = false,
				Strength = PasswordStrengthEstimator.Estimate("password")
			});
	}

	/// <summary>
	/// <see cref="PasswordPolicy.Check" />: a new password not typed yet is not too short.
	/// </summary>
	[Test]
	public void Check_Does_Not_Find_An_Empty_New_Password_Too_Short()
	{
		// Act
		PasswordCheck result = PasswordPolicy.Check(
			string.Empty,
			string.Empty,
			isNewPassword: true,
			MinimumLength);

		// Assert
		result.IsPasswordTooShort
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="PasswordPolicy.Check" />: an existing password is neither measured against the minimum length nor
	/// rated.
	/// </summary>
	[Test]
	public void Check_Ignores_The_Length_And_The_Rating_Of_An_Existing_Password()
	{
		// Act
		PasswordCheck result = PasswordPolicy.Check(
			"pass",
			null,
			isNewPassword: false,
			MinimumLength);

		// Assert
		result
			.Should()
			.Be(new PasswordCheck
			{
				IsConfirmationMismatched = false,
				IsPasswordAccepted = true,
				IsPasswordTooShort = false,
				IsValid = true,
				Strength = PasswordStrength.None
			});
	}

	/// <summary>
	/// <see cref="PasswordPolicy.Check" />: a blank password is refused.
	/// </summary>
	[TestCase(null)]
	[TestCase("")]
	public void Check_Refuses_A_Blank_Password(string? password)
	{
		// Act
		PasswordCheck result = PasswordPolicy.Check(
			password,
			password,
			isNewPassword: false,
			MinimumLength);

		// Assert
		result.IsValid
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="PasswordPolicy.Check" />: a confirmation that differs from a new password is reported and keeps it
	/// from being valid.
	/// </summary>
	[Test]
	public void Check_Refuses_A_Differing_Confirmation()
	{
		// Act
		PasswordCheck result = PasswordPolicy.Check(
			"password",
			"passwork",
			isNewPassword: true,
			MinimumLength);

		// Assert
		result.IsConfirmationMismatched
			.Should()
			.BeTrue();

		result.IsValid
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="PasswordPolicy.Check" />: a new password shorter than the minimum length is reported and refused.
	/// </summary>
	[Test]
	public void Check_Refuses_A_New_Password_Below_The_Minimum_Length()
	{
		// Act
		PasswordCheck result = PasswordPolicy.Check(
			"passwor",
			"passwor",
			isNewPassword: true,
			MinimumLength);

		// Assert
		result.IsPasswordTooShort
			.Should()
			.BeTrue();

		result.IsPasswordAccepted
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="PasswordPolicy.Check" />: a password that starts or ends with a space is refused.
	/// </summary>
	[TestCase(" password")]
	[TestCase("password ")]
	public void Check_Refuses_Edge_Spaces(string password)
	{
		// Act
		PasswordCheck result = PasswordPolicy.Check(
			password,
			password,
			isNewPassword: false,
			MinimumLength);

		// Assert
		result.IsPasswordAccepted
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="PasswordPolicy.Check" />: a confirmed new password of the minimum length is valid.
	/// </summary>
	[Test]
	public void Check_Takes_A_Confirmed_Password_Of_The_Minimum_Length()
	{
		// Act
		PasswordCheck result = PasswordPolicy.Check(
			"password",
			"password",
			isNewPassword: true,
			MinimumLength);

		// Assert
		result.IsValid
			.Should()
			.BeTrue();
	}
	#endregion
}
