using AwesomeAssertions;
using Shared.Extensions;
using System.Collections.Generic;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ListExtensions)}"" type")]
internal class ListExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="ListExtensions.MoveToTop{T}" />: keeps the list unchanged when the element is already first.
	/// </summary>
	[Test]
	public void MoveToTop_Keeps_List_Unchanged_When_Index_Is_Zero()
	{
		// Arrange
		List<int> list = [1, 2, 3];

		// Act
		list.MoveToTop(0);

		// Assert
		list
			.Should()
			.Equal(1, 2, 3);
	}

	/// <summary>
	/// <see cref="ListExtensions.MoveToTop{T}" />: moves the element at the given index to the beginning, shifting the rest right.
	/// </summary>
	[Test]
	public void MoveToTop_Moves_Element_At_Index_To_The_Beginning()
	{
		// Arrange
		List<int> list = [10, 20, 30, 40];

		// Act
		list.MoveToTop(2);

		// Assert
		list
			.Should()
			.Equal(30, 10, 20, 40);
	}

	/// <summary>
	/// <see cref="ListExtensions.MoveToTop{T}" />: moves the last element to the beginning, shifting the rest right.
	/// </summary>
	[Test]
	public void MoveToTop_Moves_Last_Element_To_The_Beginning()
	{
		// Arrange
		List<int> list = [1, 2, 3];

		// Act
		list.MoveToTop(2);

		// Assert
		list
			.Should()
			.Equal(3, 1, 2);
	}
	#endregion
}
