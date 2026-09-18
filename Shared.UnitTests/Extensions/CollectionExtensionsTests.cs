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
	#endregion
}
