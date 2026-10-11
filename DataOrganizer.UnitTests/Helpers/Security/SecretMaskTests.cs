using AwesomeAssertions;
using DataOrganizer.Helpers.Security;
using Shared.Common;

namespace DataOrganizer.UnitTests.Helpers.Security;

[TestFixture(Description = $@"Tests of ""{nameof(SecretMask)}"" type")]
internal class SecretMaskTests
{
	#region Methods
	/// <summary>
	/// <see cref="SecretMask.Apply" />: a hidden value shows the same mask whatever its length.
	/// </summary>
	[Test]
	public void Apply_Hides_The_Length_Of_A_Hidden_Value()
	{
		// Act
		string? result = SecretMask.Apply(RandomString.Create(40), isHidden: true);

		// Assert
		result
			.Should()
			.Be(SecretMask.Apply("a", isHidden: true))
			.And
			.NotBeNullOrEmpty();
	}

	/// <summary>
	/// <see cref="SecretMask.Apply" />: an empty value shows no mask.
	/// </summary>
	[TestCase(null)]
	[TestCase("")]
	public void Apply_Shows_No_Mask_For_An_Empty_Value(string? value)
	{
		// Act
		string? result = SecretMask.Apply(value, isHidden: true);

		// Assert
		result
			.Should()
			.Be(value);
	}

	/// <summary>
	/// <see cref="SecretMask.Apply" />: a value that is not hidden is shown as it is.
	/// </summary>
	[Test]
	public void Apply_Shows_The_Value_That_Is_Not_Hidden()
	{
		// Arrange
		string value = RandomString.Create(16);

		// Act
		string? result = SecretMask.Apply(value, isHidden: false);

		// Assert
		result
			.Should()
			.Be(value);
	}
	#endregion
}
