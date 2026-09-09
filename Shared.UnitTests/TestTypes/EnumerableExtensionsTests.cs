using AwesomeAssertions;
using Shared.Extensions;
using System.Collections.Generic;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EnumerableExtensions)}"" type")]
internal class EnumerableExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="EnumerableExtensions.ForEachFor{T}" />: invokes the action with each element and its index.
	/// </summary>
	[Test]
	public void ForEachFor_Invokes_Action_With_Element_And_Index()
	{
		// Arrange
		string[] source = ["a", "b", "c"];

		List<(string Item, int Index)> visited = [];

		// Act
		source.ForEachFor((item, index) => visited.Add((item, index)));

		// Assert
		visited
			.Should()
			.Equal(("a", 0), ("b", 1), ("c", 2));
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.OfSpecificType{TSource, TResult}" />: returns only exact-type instances, excluding subclasses.
	/// </summary>
	[Test]
	public void OfSpecificType_Returns_Only_Exact_Type_Excluding_Subclasses()
	{
		// Arrange
		Base exact = new();

		Derived derived = new();

		Base[] source = [exact, derived];

		// Act
		IEnumerable<Base> result = source.OfSpecificType<Base, Base>();

		// Assert
		result
			.Should()
			.Equal(exact);
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.OrderBySequenceKeepSource{T, TOrdered}" />: appends source items missing from the ordering sequence at the end.
	/// </summary>
	[Test]
	public void OrderBySequenceKeepSource_Appends_Unmatched_Source_Items_At_End()
	{
		// Arrange
		Item a = new(1, "a");

		Item b = new(2, "b");

		Item c = new(3, "c");

		Item[] source = [a, b, c];

		int[] ordered = [3, 1];

		// Act
		IEnumerable<Item> result = source.OrderBySequenceKeepSource(ordered, x => x.Id);

		// Assert
		result
			.Should()
			.Equal(c, a, b);
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.OrderBySequenceKeepSource{T, TOrdered}" />: ignores ordering keys that are absent from the source.
	/// </summary>
	[Test]
	public void OrderBySequenceKeepSource_Ignores_Ordered_Keys_Missing_From_Source()
	{
		// Arrange
		Item a = new(1, "a");

		Item b = new(2, "b");

		Item[] source = [a, b];

		int[] ordered = [5, 2, 1];

		// Act
		IEnumerable<Item> result = source.OrderBySequenceKeepSource(ordered, x => x.Id);

		// Assert
		result
			.Should()
			.Equal(b, a);
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.OrderBySequenceKeepSource{T, TOrdered}" />: orders the source by the supplied sequence of keys.
	/// </summary>
	[Test]
	public void OrderBySequenceKeepSource_Orders_Source_By_Given_Sequence()
	{
		// Arrange
		Item a = new(1, "a");

		Item b = new(2, "b");

		Item c = new(3, "c");

		Item[] source = [a, b, c];

		int[] ordered = [3, 1, 2];

		// Act
		IEnumerable<Item> result = source.OrderBySequenceKeepSource(ordered, x => x.Id);

		// Assert
		result
			.Should()
			.Equal(c, a, b);
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.SplitAsString{T}" />: appends a trailing separator when requested.
	/// </summary>
	[Test]
	public void SplitAsString_Appends_Trailing_Separator_When_Requested()
	{
		// Arrange
		int[] source = [1, 2];

		// Act
		string result = source.SplitAsString("-", addSeparatorToEnd: true);

		// Assert
		result
			.Should()
			.Be("1-2-");
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.SplitAsString{T}" />: joins the items with the separator between them.
	/// </summary>
	[Test]
	public void SplitAsString_Joins_Items_With_Separator()
	{
		// Arrange
		int[] source = [1, 2, 3];

		// Act
		string result = source.SplitAsString(", ");

		// Assert
		result
			.Should()
			.Be("1, 2, 3");
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.SplitAsString{T}" />: returns an empty string for a null sequence.
	/// </summary>
	[Test]
	public void SplitAsString_Returns_Empty_For_Null_Sequence()
	{
		// Arrange
		int[]? source = null;

		// Act
		string result = source!.SplitAsString(", ");

		// Assert
		result
			.Should()
			.BeEmpty();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Base type for the exact-type filtering test.
	/// </summary>
	private class Base;

	/// <summary>
	/// Subclass that must be excluded by an exact-type filter on <see cref="Base" />.
	/// </summary>
	private sealed class Derived : Base;

	/// <summary>
	/// Item with an ordering key and a label for sequence-ordering tests.
	/// </summary>
	private sealed record Item(int Id, string Name);
	#endregion
}
