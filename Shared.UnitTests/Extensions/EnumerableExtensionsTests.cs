using AwesomeAssertions;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(EnumerableExtensions)}"" type")]
internal class EnumerableExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="EnumerableExtensions.AsNotNull{T}(T[])" />: a null array is handed on as an empty one.
	/// </summary>
	[Test]
	public void AsNotNull_Turns_A_Null_Array_Into_An_Empty_One()
	{
		// Arrange
		int[]? source = null;

		// Act
		int[] result = source.AsNotNull();

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.Flatten{T}" />: the children of an item are asked for only when the walk goes on past
	/// it, so a consumer that stops early asks for none.
	/// </summary>
	[Test]
	public void Flatten_Asks_For_The_Children_Of_An_Item_Only_When_The_Walk_Goes_On()
	{
		// Arrange
		List<int> asked = [];

		int[] roots = [1, 2];

		// Act
		int[] result = [.. roots
			.Flatten(x =>
			{
				asked.Add(x);

				return [x * 10];
			})
			.Take(1)];

		// Assert
		result
			.Should()
			.Equal(2);

		asked
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.Flatten{T}" />: lists the items in the order of the walk, the last root first, every
	/// item before its children and the last child first.
	/// </summary>
	[Test]
	public void Flatten_Lists_The_Items_In_The_Order_Of_The_Walk()
	{
		// Arrange
		Dictionary<int, int[]> children = new()
		{
			[1] = [3, 4],
			[4] = [5]
		};

		int[] roots = [1, 2];

		// Act
		int[] result = [.. roots.Flatten(x => children.TryGetValue(x, out int[]? found) ? found : [])];

		// Assert
		result
			.Should()
			.Equal(2, 1, 4, 5, 3);
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.ForEachAsync{T}(IEnumerable{T}, Action{T}, int, CancellationToken)" />:
	/// a degree of parallelism that is not positive is refused instead of reaching the loop.
	/// </summary>
	[Test]
	public async Task ForEachAsync_Refuses_A_Degree_Of_Parallelism_That_Is_Not_Positive()
	{
		// Arrange
		int[] source = [1, 2, 3];

		// Act
		Func<Task> act = () => source.ForEachAsync(_ => { }, maxDegreeOfParallelism: -1);

		// Assert
		await act
			.Should()
			.ThrowAsync<ArgumentOutOfRangeException>();
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.ForEachWithIndex{T}" />: invokes the action with each element and its index.
	/// </summary>
	[Test]
	public void ForEachWithIndex_Invokes_Action_With_Element_And_Index()
	{
		// Arrange
		string[] source = ["a", "b", "c"];

		List<(string Item, int Index)> visited = [];

		// Act
		source.ForEachWithIndex((item, index) => visited.Add((item, index)));

		// Assert
		visited
			.Should()
			.Equal(("a", 0), ("b", 1), ("c", 2));
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.JoinAsString{T}" />: an empty sequence gets no trailing separator.
	/// </summary>
	[Test]
	public void JoinAsString_Adds_No_Trailing_Separator_To_An_Empty_Sequence()
	{
		// Arrange
		int[] source = [];

		// Act
		string result = source.JoinAsString("-", addSeparatorToEnd: true);

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.JoinAsString{T}" />: appends a trailing separator when requested.
	/// </summary>
	[Test]
	public void JoinAsString_Appends_Trailing_Separator_When_Requested()
	{
		// Arrange
		int[] source = [1, 2];

		// Act
		string result = source.JoinAsString("-", addSeparatorToEnd: true);

		// Assert
		result
			.Should()
			.Be("1-2-");
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.JoinAsString{T}" />: joins the items with the separator between them.
	/// </summary>
	[Test]
	public void JoinAsString_Joins_Items_With_Separator()
	{
		// Arrange
		int[] source = [1, 2, 3];

		// Act
		string result = source.JoinAsString(", ");

		// Assert
		result
			.Should()
			.Be("1, 2, 3");
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.JoinAsString{T}" />: returns an empty string for a null sequence.
	/// </summary>
	[Test]
	public void JoinAsString_Returns_Empty_For_Null_Sequence()
	{
		// Arrange
		int[]? source = null;

		// Act
		string result = source!.JoinAsString(", ");

		// Assert
		result
			.Should()
			.BeEmpty();
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
	#endregion

	#region Nested Types
	/// <summary>
	/// Item with an ordering key and a label for sequence-ordering tests.
	/// </summary>
	private sealed record Item(int Id, string Name);
	#endregion
}
