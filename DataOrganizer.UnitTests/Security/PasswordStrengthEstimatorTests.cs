using AwesomeAssertions;
using DataOrganizer.Enums;
using DataOrganizer.Helpers.Security;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(PasswordStrengthEstimator)}"" type")]
internal class PasswordStrengthEstimatorTests
{
	#region Methods
	/// <summary>
	/// <see cref="PasswordStrengthEstimator.Estimate" />: repeats, runs and a common base word
	/// cost an attacker less than fresh characters, whatever the length says.
	/// </summary>
	[Test]
	public void Estimate_Discounts_What_Costs_An_Attacker_Less()
	{
		// Act
		PasswordStrength repeated = PasswordStrengthEstimator.Estimate("zzzzzzzzzzzzzz");

		PasswordStrength sequential = PasswordStrengthEstimator.Estimate("abcdefghijklmn");

		PasswordStrength common = PasswordStrengthEstimator.Estimate("passwordpasswo");

		PasswordStrength varied = PasswordStrengthEstimator.Estimate("kqzmvptrwbxjhn");

		// Assert
		repeated
			.Should()
			.Be(PasswordStrength.Weak);

		sequential
			.Should()
			.Be(PasswordStrength.Weak);

		common
			.Should()
			.Be(PasswordStrength.Weak);

		varied
			.Should()
			.Be(PasswordStrength.Strong);
	}

	/// <summary>
	/// <see cref="PasswordStrengthEstimator.Estimate" />: a leaked-list word, a keyboard run or a
	/// single repeated character is guessed offline whatever else is bolted onto it.
	/// </summary>
	[TestCase("password")]
	[TestCase("PASSWORD")]
	[TestCase("Password1!")]
	[TestCase("12345678")]
	[TestCase("qwerty123")]
	[TestCase("aaaaaaaaaaaa")]
	[TestCase("abcdefghij")]
	public void Estimate_Rates_A_Guessable_Password_As_Weak(string password)
	{
		// Act
		PasswordStrength result = PasswordStrengthEstimator.Estimate(password);

		// Assert
		result
			.Should()
			.Be(PasswordStrength.Weak);
	}

	/// <summary>
	/// <see cref="PasswordStrengthEstimator.Estimate" />: a long unstructured password is out of
	/// reach of an offline run.
	/// </summary>
	[TestCase("aB3#kL9%mZ2^qT")]
	[TestCase("kqzmvptrwbxjhnfdgu")]
	public void Estimate_Rates_A_Long_Varied_Password_As_Very_Strong(string password)
	{
		// Act
		PasswordStrength result = PasswordStrengthEstimator.Estimate(password);

		// Assert
		result
			.Should()
			.Be(PasswordStrength.VeryStrong);
	}

	/// <summary>
	/// <see cref="PasswordStrengthEstimator.Estimate" />: an unstructured password of the length
	/// the policy asks for holds against casual guessing only.
	/// </summary>
	[TestCase("aB3#kL9")]
	[TestCase("kqzmvptrw")]
	public void Estimate_Rates_A_Short_Varied_Password_As_Fair(string password)
	{
		// Act
		PasswordStrength result = PasswordStrengthEstimator.Estimate(password);

		// Assert
		result
			.Should()
			.Be(PasswordStrength.Fair);
	}

	/// <summary>
	/// <see cref="PasswordStrengthEstimator.Estimate" />: length and variety together carry a
	/// password past what an offline run is worth.
	/// </summary>
	[TestCase("aB3#kL9%mZ")]
	[TestCase("kqzmvptrwbxjhn")]
	[TestCase("Tr0ub4dor&3")]
	public void Estimate_Rates_A_Varied_Password_As_Strong(string password)
	{
		// Act
		PasswordStrength result = PasswordStrengthEstimator.Estimate(password);

		// Assert
		result
			.Should()
			.Be(PasswordStrength.Strong);
	}

	/// <summary>
	/// <see cref="PasswordStrengthEstimator.Estimate" />: nothing typed yet is not rated.
	/// </summary>
	[TestCase(null)]
	[TestCase("")]
	[TestCase("   ")]
	public void Estimate_Returns_None_For_Empty_Or_Whitespace(string? password)
	{
		// Act
		PasswordStrength result = PasswordStrengthEstimator.Estimate(password);

		// Assert
		result
			.Should()
			.Be(PasswordStrength.None);
	}
	#endregion
}
