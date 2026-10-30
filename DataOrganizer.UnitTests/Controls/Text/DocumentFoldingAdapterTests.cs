using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AwesomeAssertions;
using DataOrganizer.Controls.Text;
using System.Collections.Generic;
using System.Linq;
using TextProcessing.Dto;
using TextProcessing.Helpers;
using TextProcessing.UnitTests.Fakes;

namespace DataOrganizer.UnitTests.Controls.Text;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentFoldingAdapter)}"" type")]
internal class DocumentFoldingAdapterTests
{
	#region Methods
	/// <summary>
	/// <see cref="DocumentFoldingAdapter.CreateNewFoldings" />: each block of the text reaches the document with its bounds
	/// and its name, and starts unfolded.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_The_Bounds_And_The_Name_Of_Each_Block()
	{
		// Arrange
		DocumentFoldingAdapter sut = new(new CompositeFoldingStrategy(new FixedFoldingStrategy(new TextFolding
		{
			EndOffset = 3,
			Name = "block",
			StartOffset = 1
		})));

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(new TextDocument("a\nb\nc"), out _);

		// Assert
		foldings.Select(static x => (x.StartOffset, x.EndOffset, x.Name, x.DefaultClosed))
			.Should()
			.Equal((1, 3, "block", false));
	}

	/// <summary>
	/// <see cref="DocumentFoldingAdapter.CreateNewFoldings" />: the ways read any text, so there is no error to report.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Reports_No_Error()
	{
		// Arrange
		DocumentFoldingAdapter sut = new(new CompositeFoldingStrategy(new FixedFoldingStrategy(new TextFolding
		{
			EndOffset = 3,
			StartOffset = 1
		})));

		// Act
		sut.CreateNewFoldings(new TextDocument("a\nb\nc"), out int firstErrorOffset);

		// Assert
		firstErrorOffset
			.Should()
			.Be(-1);
	}
	#endregion
}
