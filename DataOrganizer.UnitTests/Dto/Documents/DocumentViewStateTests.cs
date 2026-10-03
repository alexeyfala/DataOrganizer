using AwesomeAssertions;
using DataOrganizer.Dto.Documents;

namespace DataOrganizer.UnitTests.Dto.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentViewState)}"" type")]
internal class DocumentViewStateTests
{
	#region Methods
	/// <summary>
	/// <see cref="DocumentViewState.Bookmarks" />: an empty set of lines is kept as <c>null</c>, the one form of none.
	/// </summary>
	[Test]
	public void Bookmarks_Stores_An_Empty_Set_As_Null()
	{
		// Act
		DocumentViewState sut = Create(bookmarks: []);

		// Assert
		sut.Bookmarks
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentViewState.Equals(DocumentViewState)" />: the bookmarks compare by their lines,
	/// and no bookmarks equal an empty set.
	/// </summary>
	[TestCase(new[] { 2, 5 }, new[] { 2, 5 }, true)]
	[TestCase(null, new int[0], true)]
	[TestCase(new[] { 2 }, new[] { 5 }, false)]
	public void Equals_Compares_The_Bookmarks_By_Their_Lines(int[]? bookmarks, int[]? otherBookmarks, bool expected)
	{
		// Arrange
		DocumentViewState sut = Create(bookmarks);

		// Act
		bool isEqual = sut.Equals(Create(otherBookmarks));

		// Assert
		isEqual
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocumentViewState.Equals(DocumentViewState)" />: the folded blocks compare by their offsets,
	/// and no folded blocks equal an empty set.
	/// </summary>
	[TestCase(new[] { 10, 40 }, new[] { 10, 40 }, true)]
	[TestCase(null, new int[0], true)]
	[TestCase(new[] { 10 }, new[] { 40 }, false)]
	public void Equals_Compares_The_Folded_Blocks_By_Their_Offsets(
		int[]? foldedBlocks,
		int[]? otherFoldedBlocks,
		bool expected)
	{
		// Arrange
		DocumentViewState sut = Create(foldedBlocks: foldedBlocks);

		// Act
		bool isEqual = sut.Equals(Create(foldedBlocks: otherFoldedBlocks));

		// Assert
		isEqual
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocumentViewState.Equals(DocumentViewState)" />: the unfolded blocks compare by their offsets,
	/// and an empty set, which folds every block, differs from none.
	/// </summary>
	[TestCase(new[] { 10, 40 }, new[] { 10, 40 }, true)]
	[TestCase(null, new int[0], false)]
	[TestCase(new[] { 10 }, new[] { 40 }, false)]
	public void Equals_Compares_The_Unfolded_Blocks_By_Their_Offsets(
		int[]? unfoldedBlocks,
		int[]? otherUnfoldedBlocks,
		bool expected)
	{
		// Arrange
		DocumentViewState sut = Create(unfoldedBlocks: unfoldedBlocks);

		// Act
		bool isEqual = sut.Equals(Create(unfoldedBlocks: otherUnfoldedBlocks));

		// Assert
		isEqual
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocumentViewState.FoldedBlocks" />: an empty set of offsets is kept as <c>null</c>, the one form of none.
	/// </summary>
	[Test]
	public void FoldedBlocks_Stores_An_Empty_Set_As_Null()
	{
		// Act
		DocumentViewState sut = Create(foldedBlocks: []);

		// Assert
		sut.FoldedBlocks
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentViewState.GetHashCode" />: states with the same lines have the same hash.
	/// </summary>
	[TestCase(new[] { 2, 5 }, new[] { 2, 5 })]
	[TestCase(null, new int[0])]
	public void GetHashCode_Is_The_Same_For_The_Same_Lines(int[]? bookmarks, int[]? otherBookmarks)
	{
		// Arrange
		DocumentViewState sut = Create(bookmarks);

		// Act
		int hash = sut.GetHashCode();

		// Assert
		hash
			.Should()
			.Be(Create(otherBookmarks).GetHashCode());
	}

	/// <summary>
	/// <see cref="DocumentViewState.UnfoldedBlocks" />: an empty set of offsets is kept, as it folds every block.
	/// </summary>
	[Test]
	public void UnfoldedBlocks_Keeps_An_Empty_Set()
	{
		// Act
		DocumentViewState sut = Create(unfoldedBlocks: []);

		// Assert
		sut.UnfoldedBlocks
			.Should()
			.BeEmpty();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a view state with the bookmarks and the blocks, the rest of it the same every time.
	/// </summary>
	private static DocumentViewState Create(
		int[]? bookmarks = null,
		int[]? foldedBlocks = null,
		int[]? unfoldedBlocks = null)
	{
		return new()
		{
			Bookmarks = bookmarks,
			CaretPosition = new(line: 3, column: 1),
			FoldedBlocks = foldedBlocks,
			ScrollOffset = new(0.0, 100.0),
			SelectionLength = 4,
			SelectionStart = 20,
			UnfoldedBlocks = unfoldedBlocks
		};
	}
	#endregion
}
