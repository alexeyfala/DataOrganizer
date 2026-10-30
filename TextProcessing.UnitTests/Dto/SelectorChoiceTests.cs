using AwesomeAssertions;
using TextProcessing.Dto;

namespace TextProcessing.UnitTests.Dto;

[TestFixture(Description = $@"Tests of ""{nameof(SelectorChoice)}"" type")]
internal class SelectorChoiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="SelectorChoice.IsAvailable" />: an item can be chosen unless it says otherwise.
	/// </summary>
	[Test]
	public void IsAvailable_Is_True_By_Default()
	{
		// Arrange
		SelectorChoice sut = new()
		{
			Id = "a",
			Name = "A",
			SearchTerms = []
		};

		// Act
		bool isAvailable = sut.IsAvailable;

		// Assert
		isAvailable
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SelectorChoice.IsDefault" />: the item with a default mark is the one taken when none is chosen.
	/// </summary>
	[TestCase("default", true)]
	[TestCase(null, false)]
	public void IsDefault_Follows_The_Default_Mark(string? defaultMark, bool expected)
	{
		// Arrange
		SelectorChoice sut = new()
		{
			DefaultMark = defaultMark,
			Id = "a",
			Name = "A",
			SearchTerms = []
		};

		// Act
		bool isDefault = sut.IsDefault;

		// Assert
		isDefault
			.Should()
			.Be(expected);
	}
	#endregion
}
