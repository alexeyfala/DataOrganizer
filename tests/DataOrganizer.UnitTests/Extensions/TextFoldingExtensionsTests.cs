using AvaloniaEdit.Folding;
using AwesomeAssertions;
using DataOrganizer.Documents.TextProcessing.Dto;
using DataOrganizer.Extensions;

namespace DataOrganizer.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(TextFoldingExtensions)}"" type")]
internal class TextFoldingExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="TextFoldingExtensions.ToNewFolding" />: the block of the engine has the bounds and the name of the block
	/// of the text, and starts unfolded.
	/// </summary>
	[Test]
	public void ToNewFolding_Keeps_The_Bounds_And_The_Name()
	{
		// Arrange
		TextFolding folding = new()
		{
			EndOffset = 3,
			Name = "block",
			StartOffset = 1
		};

		// Act
		NewFolding result = folding.ToNewFolding();

		// Assert
		(result.StartOffset, result.EndOffset, result.Name, result.DefaultClosed)
			.Should()
			.Be((1, 3, "block", false));
	}
	#endregion
}
