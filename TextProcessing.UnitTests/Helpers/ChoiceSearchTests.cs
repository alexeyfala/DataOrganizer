using AwesomeAssertions;
using TextProcessing.Dto;
using TextProcessing.Helpers;

namespace TextProcessing.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(ChoiceSearch)}"" type")]
internal class ChoiceSearchTests
{
	#region Methods
	/// <summary>
	/// <see cref="ChoiceSearch.Find" />: an item that matches in another letter case is found.
	/// </summary>
	[Test]
	public void Find_Ignores_The_Letter_Case()
	{
		// Arrange
		SelectorChoice python = CreateChoice("Python");

		// Act
		SelectorChoice[] result = ChoiceSearch.Find([python], "PY");

		// Assert
		result
			.Should()
			.Equal(python);
	}

	/// <summary>
	/// <see cref="ChoiceSearch.Find" />: an exact search term goes first, then the start of a name, the start of a term
	/// or of a description, a part of a name and a part of a description; an item without a match is left out.
	/// </summary>
	[Test]
	public void Find_Puts_The_Best_Matches_First()
	{
		// Arrange
		SelectorChoice exactTerm = CreateChoice("Zeta", searchTerms: ["py"]);

		SelectorChoice namePrefix = CreateChoice("Python");

		SelectorChoice termPrefix = CreateChoice("Quill", searchTerms: ["pyw"]);

		SelectorChoice namePart = CreateChoice("Copy");

		SelectorChoice descriptionPart = CreateChoice("Wren", description: "happy");

		SelectorChoice other = CreateChoice("Other");

		// Act
		SelectorChoice[] result = ChoiceSearch.Find(
			[descriptionPart, namePart, other, termPrefix, namePrefix, exactTerm],
			"py");

		// Assert
		result
			.Should()
			.Equal(exactTerm, namePrefix, termPrefix, namePart, descriptionPart);
	}

	/// <summary>
	/// <see cref="ChoiceSearch.Find" />: an item that cannot be chosen follows every item that can, however well it
	/// matches.
	/// </summary>
	[Test]
	public void Find_Puts_The_Unavailable_Items_Last()
	{
		// Arrange
		SelectorChoice unavailable = CreateChoice("Python", isAvailable: false);

		SelectorChoice available = CreateChoice("Copy");

		// Act
		SelectorChoice[] result = ChoiceSearch.Find([unavailable, available], "py");

		// Assert
		result
			.Should()
			.Equal(available, unavailable);
	}

	/// <summary>
	/// <see cref="ChoiceSearch.Normalize" />: the spaces around a search and the dot of an extension are dropped.
	/// </summary>
	[TestCase("  .cs  ", "cs")]
	[TestCase("cs", "cs")]
	[TestCase("   ", "")]
	[TestCase(null, "")]
	public void Normalize_Drops_The_Spaces_And_The_Dot_Of_An_Extension(string? typed, string expected)
	{
		// Act
		string result = ChoiceSearch.Normalize(typed);

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates an item of a list with a search.
	/// </summary>
	private static SelectorChoice CreateChoice(
		string name,
		string[]? searchTerms = null,
		string? description = null,
		bool isAvailable = true) => new()
		{
			Description = description,
			Id = name,
			IsAvailable = isAvailable,
			Name = name,
			SearchTerms = searchTerms ?? []
		};
	#endregion
}
