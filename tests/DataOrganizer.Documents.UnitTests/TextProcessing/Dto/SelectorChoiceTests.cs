using AwesomeAssertions;
using DataOrganizer.Documents.TextProcessing.Dto;

namespace DataOrganizer.Documents.UnitTests.TextProcessing.Dto;

[TestFixture(Description = $@"Tests of ""{nameof(SelectorChoice)}"" type")]
internal class SelectorChoiceTests
{
	#region Methods
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
