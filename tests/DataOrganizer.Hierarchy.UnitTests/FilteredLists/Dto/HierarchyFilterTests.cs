using AwesomeAssertions;
using DataOrganizer.Hierarchy.FilteredLists.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Hierarchy.UnitTests.FilteredLists.Dto;

[TestFixture(Description = $@"Tests of ""{nameof(HierarchyFilter)}"" type")]
internal class HierarchyFilterTests
{
	#region Methods
	/// <summary>
	/// <see cref="HierarchyFilter.IsNothingFound" />: only a search that shows no object has found nothing.
	/// </summary>
	[TestCase(null, 0, false)]
	[TestCase("a", 0, true)]
	[TestCase("a", 1, false)]
	public void IsNothingFound_Tells_A_Query_That_Shows_No_Object(string? query, int shownCount, bool expected)
	{
		// Arrange
		HierarchyFilter sut = new()
		{
			ExpandedIds = new HashSet<Guid>(),
			Query = query,
			ShownIds = Enumerable
				.Range(0, shownCount)
				.Select(static _ => Guid.NewGuid())
				.ToHashSet()
		};

		// Act
		bool result = sut.IsNothingFound;

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="HierarchyFilter.Shows" />: a filter without a search shows any object.
	/// </summary>
	[Test]
	public void Shows_Any_Object_Without_A_Query()
	{
		// Act
		bool result = HierarchyFilter
			.None
			.Shows(Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="HierarchyFilter.Shows" />: a filter with a search shows its shown objects and no other.
	/// </summary>
	[Test]
	public void Shows_Only_The_Shown_Objects_With_A_Query()
	{
		// Arrange
		Guid shownId = Guid.NewGuid();

		HierarchyFilter sut = new()
		{
			ExpandedIds = new HashSet<Guid>(),
			Query = "a",
			ShownIds = new HashSet<Guid> { shownId }
		};

		// Act, Assert
		sut.Shows(shownId)
			.Should()
			.BeTrue();

		sut.Shows(Guid.NewGuid())
			.Should()
			.BeFalse();
	}
	#endregion
}
