using AwesomeAssertions;
using Shared.Extensions;
using System.Collections.Generic;

namespace Shared.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(Shared.Extensions.CollectionExtensions)}"" type")]
internal class CollectionExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="Shared.Extensions.CollectionExtensions.ClearAddRange{T}" />: the previous items are gone,
	/// not kept in front of the new ones.
	/// </summary>
	[Test]
	public void ClearAddRange_Replaces_The_Previous_Items()
	{
		// Arrange
		List<int> collection = [1, 2, 3];

		// Act
		collection.ClearAddRange([4, 5]);

		// Assert
		collection
			.Should()
			.Equal(4, 5);
	}

	/// <summary>
	/// <see cref="Shared.Extensions.CollectionExtensions.SortBy{TSource, TKey}" />: the collection itself is sorted by the
	/// key, ascending.
	/// </summary>
	[Test]
	public void SortBy_Sorts_The_Collection_In_Place()
	{
		// Arrange
		List<string> collection = ["ccc", "a", "bb"];

		// Act
		collection.SortBy(static x => x.Length);

		// Assert
		collection
			.Should()
			.Equal("a", "bb", "ccc");
	}
	#endregion
}
