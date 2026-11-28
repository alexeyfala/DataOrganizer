using AwesomeAssertions;
using DataOrganizer.Hierarchy.FilteredLists.Helpers;
using DataOrganizer.Hierarchy.Interfaces;
using DataOrganizer.Hierarchy.UnitTests.TestHelpers.Factories;
using System;

namespace DataOrganizer.Hierarchy.UnitTests.FilteredLists.Helpers;

/// <summary>
/// Tests of <see cref="NameFilter" /> type.
/// </summary>
[TestFixture]
internal sealed class NameFilterTests
{
	#region Methods
	/// <summary>
	/// <see cref="NameFilter.Create" />: a blank search matches any name.
	/// </summary>
	[TestCase(null)]
	[TestCase("")]
	[TestCase("  ")]
	public void Create_Blank_Query_Matches_Every_Name(string? query)
	{
		// Act
		Func<INamed, bool> predicate = NameFilter.Create(query);

		// Assert
		predicate(ItemDtoFactory.CreateNamedFileDto("Other"))
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NameFilter.Create" />: a search matches the names that hold it, case aside, and no other.
	/// </summary>
	[Test]
	public void Create_Matches_The_Names_That_Hold_The_Query()
	{
		// Act
		Func<INamed, bool> predicate = NameFilter.Create("hello");

		// Assert
		predicate(ItemDtoFactory.CreateNamedFileDto("Say Hello world"))
			.Should()
			.BeTrue();

		predicate(ItemDtoFactory.CreateNamedFileDto("Other"))
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="NameFilter.IsBlank" />: a search of white space alone holds nothing to look for.
	/// </summary>
	[TestCase(null, true)]
	[TestCase("", true)]
	[TestCase("  ", true)]
	[TestCase(" a ", false)]
	public void IsBlank_Tells_A_Query_Without_Anything_To_Look_For(string? query, bool expected)
	{
		// Act
		bool result = NameFilter.IsBlank(query);

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion
}
