using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Helpers.Text;
using System.Linq;

namespace DataOrganizer.UnitTests.Helpers.Text;

[TestFixture(Description = $@"Tests of ""{nameof(LineBookmarks)}"" type")]
internal class LineBookmarksTests
{
	#region Methods
	/// <summary>
	/// <see cref="LineBookmarks.Clear" />: reports a change only when there were bookmarks to remove.
	/// </summary>
	[Test]
	public void Clear_Raises_Changed_Only_With_Bookmarks([Values] bool hasBookmarks)
	{
		// Arrange
		LineBookmarks sut = new()
		{
			Document = new("One\nTwo\nThree")
		};

		if (hasBookmarks)
		{
			sut.Toggle(2);
		}

		int changeCount = 0;

		sut.Changed += (_, _) => changeCount++;

		// Act
		sut.Clear();

		// Assert
		changeCount
			.Should()
			.Be(hasBookmarks ? 1 : 0);
	}

	/// <summary>
	/// <see cref="LineBookmarks.Clear" />: removes every bookmark.
	/// </summary>
	[Test]
	public void Clear_Removes_Every_Bookmark()
	{
		// Arrange
		LineBookmarks sut = new()
		{
			Document = new("One\nTwo\nThree")
		};

		sut.Toggle(1);

		sut.Toggle(3);

		// Act
		sut.Clear();

		// Assert
		sut.GetLines()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="LineBookmarks.Contains" />: tells a bookmarked line from a line without a bookmark.
	/// </summary>
	[TestCase(1, false)]
	[TestCase(2, true)]
	public void Contains_Tells_A_Bookmarked_Line(int line, bool expected)
	{
		// Arrange
		LineBookmarks sut = new()
		{
			Document = new("One\nTwo\nThree")
		};

		sut.Toggle(2);

		// Act
		bool isBookmarked = sut.Contains(line);

		// Assert
		isBookmarked
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="LineBookmarks.Document" />: another document starts without bookmarks.
	/// </summary>
	[Test]
	public void Document_Starts_Without_Bookmarks()
	{
		// Arrange
		LineBookmarks sut = new()
		{
			Document = new("One\nTwo\nThree")
		};

		sut.Toggle(2);

		// Act
		sut.Document = new("Four\nFive\nSix");

		// Assert
		sut.GetLines()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="LineBookmarks.FindNext" />: after the last bookmark the search goes round to the first one.
	/// </summary>
	[TestCase(5)]
	[TestCase(7)]
	public void FindNext_Goes_Round_To_The_First_Bookmark(int line)
	{
		// Arrange
		LineBookmarks sut = new()
		{
			Document = new(string.Join('\n', Enumerable.Repeat("Line", 7)))
		};

		sut.Toggle(2);

		sut.Toggle(5);

		// Act
		int? found = sut.FindNext(line);

		// Assert
		found
			.Should()
			.Be(2);
	}

	/// <summary>
	/// <see cref="LineBookmarks.FindNext" />: without bookmarks there is nothing to find.
	/// </summary>
	[Test]
	public void FindNext_Needs_A_Bookmark()
	{
		// Arrange
		LineBookmarks sut = new()
		{
			Document = new("One\nTwo\nThree")
		};

		// Act
		int? found = sut.FindNext(1);

		// Assert
		found
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="LineBookmarks.FindNext" />: returns the first bookmarked line after the line.
	/// </summary>
	[TestCase(1, 2)]
	[TestCase(2, 5)]
	public void FindNext_Returns_The_Next_Bookmarked_Line(int line, int expected)
	{
		// Arrange
		LineBookmarks sut = new()
		{
			Document = new(string.Join('\n', Enumerable.Repeat("Line", 7)))
		};

		sut.Toggle(2);

		sut.Toggle(5);

		// Act
		int? found = sut.FindNext(line);

		// Assert
		found
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="LineBookmarks.FindPrevious" />: before the first bookmark the search goes round to the last one.
	/// </summary>
	[TestCase(1)]
	[TestCase(2)]
	public void FindPrevious_Goes_Round_To_The_Last_Bookmark(int line)
	{
		// Arrange
		LineBookmarks sut = new()
		{
			Document = new(string.Join('\n', Enumerable.Repeat("Line", 7)))
		};

		sut.Toggle(2);

		sut.Toggle(5);

		// Act
		int? found = sut.FindPrevious(line);

		// Assert
		found
			.Should()
			.Be(5);
	}

	/// <summary>
	/// <see cref="LineBookmarks.FindPrevious" />: without bookmarks there is nothing to find.
	/// </summary>
	[Test]
	public void FindPrevious_Needs_A_Bookmark()
	{
		// Arrange
		LineBookmarks sut = new()
		{
			Document = new("One\nTwo\nThree")
		};

		// Act
		int? found = sut.FindPrevious(3);

		// Assert
		found
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="LineBookmarks.FindPrevious" />: returns the last bookmarked line before the line.
	/// </summary>
	[TestCase(7, 5)]
	[TestCase(5, 2)]
	public void FindPrevious_Returns_The_Previous_Bookmarked_Line(int line, int expected)
	{
		// Arrange
		LineBookmarks sut = new()
		{
			Document = new(string.Join('\n', Enumerable.Repeat("Line", 7)))
		};

		sut.Toggle(2);

		sut.Toggle(5);

		// Act
		int? found = sut.FindPrevious(line);

		// Assert
		found
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="LineBookmarks.GetLines" />: a bookmark moves with its line when lines are added and removed above it.
	/// </summary>
	[Test]
	public void GetLines_Follows_The_Line_Through_The_Edits_Above()
	{
		// Arrange
		TextDocument document = new("One\nTwo\nThree\nFour");

		LineBookmarks sut = new()
		{
			Document = document
		};

		sut.Toggle(3);

		// Act
		document.Insert(0, "Zero\nHalf\n");

		document.Remove(0, "Zero\n".Length);

		// Assert
		sut.GetLines()
			.Should()
			.Equal(4);
	}

	/// <summary>
	/// <see cref="LineBookmarks.GetLines" />: a deletion that takes the start of a bookmarked line away leaves
	/// the bookmark on the line joined in its place.
	/// </summary>
	[Test]
	public void GetLines_Keeps_The_Bookmark_Of_A_Line_Whose_Start_Is_Deleted()
	{
		// Arrange
		TextDocument document = new("One\nTwo\nThree");

		LineBookmarks sut = new()
		{
			Document = document
		};

		sut.Toggle(2);

		// Act
		document.Remove(1, "ne\nT".Length);

		// Assert
		sut.GetLines()
			.Should()
			.Equal(1);
	}

	/// <summary>
	/// <see cref="LineBookmarks.GetLines" />: a line break put at the start of a bookmarked line takes the bookmark down
	/// with the text, and one put further leaves the bookmark on the first part.
	/// </summary>
	[TestCase(0, 3)]
	[TestCase(1, 2)]
	public void GetLines_Keeps_The_Bookmark_With_The_Start_Of_The_Text(int column, int expected)
	{
		// Arrange
		TextDocument document = new("One\nTwo\nThree");

		LineBookmarks sut = new()
		{
			Document = document
		};

		sut.Toggle(2);

		// Act
		document.Insert(document.GetLineByNumber(2).Offset + column, "\n");

		// Assert
		sut.GetLines()
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="LineBookmarks.GetLines" />: lines joined by an edit show their bookmarks once.
	/// </summary>
	[Test]
	public void GetLines_Merges_The_Bookmarks_Of_Joined_Lines()
	{
		// Arrange
		TextDocument document = new("One\nTwo\nThree");

		LineBookmarks sut = new()
		{
			Document = document
		};

		sut.Toggle(2);

		sut.Toggle(3);

		// Act
		document.Remove(document.GetLineByNumber(2).EndOffset, 1);

		// Assert
		sut.GetLines()
			.Should()
			.Equal(2);
	}

	/// <summary>
	/// <see cref="LineBookmarks.Toggle" />: without a document there is no line to bookmark.
	/// </summary>
	[Test]
	public void Toggle_Needs_A_Document()
	{
		// Arrange
		LineBookmarks sut = new();

		// Act
		sut.Toggle(1);

		// Assert
		sut.GetLines()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="LineBookmarks.Toggle" />: reports a change once, both when it sets a bookmark and when it removes one.
	/// </summary>
	[Test]
	public void Toggle_Raises_Changed([Values] bool isBookmarked)
	{
		// Arrange
		LineBookmarks sut = new()
		{
			Document = new("One\nTwo\nThree")
		};

		if (isBookmarked)
		{
			sut.Toggle(2);
		}

		int changeCount = 0;

		sut.Changed += (_, _) => changeCount++;

		// Act
		sut.Toggle(2);

		// Assert
		changeCount
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="LineBookmarks.Toggle" />: removes every bookmark of a line, those that came with a joined line too.
	/// </summary>
	[Test]
	public void Toggle_Removes_Every_Bookmark_Of_A_Line([Values] bool isJoined)
	{
		// Arrange
		TextDocument document = new("One\nTwo\nThree");

		LineBookmarks sut = new()
		{
			Document = document
		};

		sut.Toggle(2);

		if (isJoined)
		{
			sut.Toggle(3);

			document.Remove(document.GetLineByNumber(2).EndOffset, 1);
		}

		// Act
		sut.Toggle(2);

		// Assert
		sut.GetLines()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="LineBookmarks.Toggle" />: sets a bookmark on a line without one.
	/// </summary>
	[Test]
	public void Toggle_Sets_A_Bookmark_On_A_Line_Without_One()
	{
		// Arrange
		LineBookmarks sut = new()
		{
			Document = new("One\nTwo\nThree")
		};

		// Act
		sut.Toggle(2);

		// Assert
		sut.GetLines()
			.Should()
			.Equal(2);
	}

	/// <summary>
	/// <see cref="LineBookmarks.Toggle" />: a line out of the document gets no bookmark and reports no change.
	/// </summary>
	[Test]
	public void Toggle_Skips_A_Line_Out_Of_The_Document([Values(0, 4)] int line)
	{
		// Arrange
		LineBookmarks sut = new()
		{
			Document = new("One\nTwo\nThree")
		};

		int changeCount = 0;

		sut.Changed += (_, _) => changeCount++;

		// Act
		sut.Toggle(line);

		// Assert
		sut.GetLines()
			.Should()
			.BeEmpty();

		changeCount
			.Should()
			.Be(0);
	}
	#endregion
}
