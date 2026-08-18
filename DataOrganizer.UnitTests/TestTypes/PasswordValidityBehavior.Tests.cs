using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Behaviors;
using DataOrganizer.Enums;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(PasswordValidityBehavior)}"" type")]
internal class PasswordValidityBehaviorTests
{
	#region Data
	/// <summary>
	/// Minimum length the tested behavior is set up with.
	/// </summary>
	private const int MinimumLength = 8;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="PasswordValidityBehavior.IsPasswordAccepted" />: an empty confirmation is not yet
	/// a mismatch, and the password alone already counts as accepted.
	/// </summary>
	[AvaloniaTest]
	public void Create_Accepts_The_Password_While_The_Confirmation_Is_Empty()
	{
		// Arrange
		(PasswordValidityBehavior sut, TextBox password, _) = CreateSetup(isConfirmationRequired: true);

		// Act
		password.Text = new('a', MinimumLength);

		// Assert
		sut.IsPasswordAccepted
			.Should()
			.BeTrue();

		sut.IsConfirmationMismatched
			.Should()
			.BeFalse();

		sut.IsValid
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="PasswordValidityBehavior.Strength" />: the password being set is rated as it is typed.
	/// </summary>
	[AvaloniaTest]
	[TestCase("password", PasswordStrength.Weak)]
	[TestCase("aB3#kL9", PasswordStrength.Fair)]
	[TestCase("aB3#kL9%mZ", PasswordStrength.Strong)]
	[TestCase("aB3#kL9%mZ2^qT", PasswordStrength.VeryStrong)]
	public void Create_Rates_The_Password(string typed, PasswordStrength expected)
	{
		// Arrange
		(PasswordValidityBehavior sut, TextBox password, _) = CreateSetup(isConfirmationRequired: true);

		// Act
		password.Text = typed;

		// Assert
		sut.Strength
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="PasswordValidityBehavior.IsConfirmationMismatched" />: a confirmation that differs
	/// from the password is reported and blocks the result.
	/// </summary>
	[AvaloniaTest]
	public void Create_Refuses_A_Differing_Confirmation()
	{
		// Arrange
		(PasswordValidityBehavior sut, TextBox password, TextBox confirmation) = CreateSetup(isConfirmationRequired: true);

		// Act
		password.Text = new('a', MinimumLength);

		confirmation.Text = new('b', MinimumLength);

		// Assert
		sut.IsConfirmationMismatched
			.Should()
			.BeTrue();

		sut.IsValid
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="PasswordValidityBehavior.IsValid" />: a password shorter than the minimum
	/// is refused while a new one is being set.
	/// </summary>
	[AvaloniaTest]
	public void Create_Refuses_A_Password_Below_The_Minimum_Length()
	{
		// Arrange
		(PasswordValidityBehavior sut, TextBox password, TextBox confirmation) = CreateSetup(isConfirmationRequired: true);

		// Act
		password.Text = new('a', MinimumLength - 1);

		confirmation.Text = password.Text;

		// Assert
		sut.IsPasswordTooShort
			.Should()
			.BeTrue();

		sut.IsPasswordAccepted
			.Should()
			.BeFalse();

		sut.IsValid
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="PasswordValidityBehavior.IsValid" />: a password of exactly the minimum length
	/// repeated in the confirmation passes.
	/// </summary>
	[AvaloniaTest]
	public void Create_Takes_A_Password_Of_The_Minimum_Length()
	{
		// Arrange
		(PasswordValidityBehavior sut, TextBox password, TextBox confirmation) = CreateSetup(isConfirmationRequired: true);

		// Act
		password.Text = new('a', MinimumLength);

		confirmation.Text = password.Text;

		// Assert
		sut.IsPasswordTooShort
			.Should()
			.BeFalse();

		sut.IsConfirmationMismatched
			.Should()
			.BeFalse();

		sut.IsValid
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="PasswordValidityBehavior.IsValid" />: a password framed by spaces is refused.
	/// </summary>
	[AvaloniaTest]
	[TestCase(" password")]
	[TestCase("password ")]
	public void Edge_Spaces_Are_Refused(string typed)
	{
		// Arrange
		(PasswordValidityBehavior sut, TextBox password, _) = CreateSetup(isConfirmationRequired: false);

		// Act
		password.Text = typed;

		// Assert
		sut.IsValid
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="PasswordValidityBehavior.Strength" />: an existing password is not rated, as its
	/// rating changes nothing.
	/// </summary>
	[AvaloniaTest]
	public void Verify_Does_Not_Rate_The_Password()
	{
		// Arrange
		(PasswordValidityBehavior sut, TextBox password, _) = CreateSetup(isConfirmationRequired: false);

		// Act
		password.Text = "aB3#kL9%mZ2^qT";

		// Assert
		sut.Strength
			.Should()
			.Be(PasswordStrength.None);
	}

	/// <summary>
	/// <see cref="PasswordValidityBehavior.IsValid" />: the minimum length is not applied while an
	/// existing password is entered, so short ones still open their data.
	/// </summary>
	[AvaloniaTest]
	public void Verify_Ignores_The_Minimum_Length()
	{
		// Arrange
		(PasswordValidityBehavior sut, TextBox password, _) = CreateSetup(isConfirmationRequired: false);

		// Act
		password.Text = "a";

		// Assert
		sut.IsPasswordTooShort
			.Should()
			.BeFalse();

		sut.IsValid
			.Should()
			.BeTrue();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds the behavior attached to a password input and bound to a confirmation input.
	/// </summary>
	private static (PasswordValidityBehavior Sut, TextBox Password, TextBox Confirmation) CreateSetup(bool isConfirmationRequired)
	{
		TextBox password = new();

		TextBox confirmation = new();

		PasswordValidityBehavior sut = new()
		{
			ConfirmationInput = confirmation,
			IsConfirmationRequired = isConfirmationRequired,
			MinimumLength = MinimumLength
		};

		sut.Attach(password);

		return (sut, password, confirmation);
	}
	#endregion
}
