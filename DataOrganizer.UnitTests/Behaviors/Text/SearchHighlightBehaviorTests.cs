using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.NUnit;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Text;
using DataOrganizer.Helpers.Text;
using System.Linq;

namespace DataOrganizer.UnitTests.Behaviors.Text;

[TestFixture(Description = $@"Tests of ""{nameof(SearchHighlightBehavior)}"" type")]
internal class SearchHighlightBehaviorTests
{
	#region Methods
	/// <summary>
	/// <see cref="SearchHighlightBehavior.Query" />: the matches keep the text color of the theme.
	/// </summary>
	[AvaloniaTest]
	public void Query_Leaves_The_Text_Color_To_The_Theme()
	{
		// Arrange
		TextBlock textBlock = new();

		SearchHighlightBehavior sut = new()
		{
			SourceText = "Log and log"
		};

		Interaction
			.GetBehaviors(textBlock)
			.Add(sut);

		// Act
		sut.Query = "log";

		// Assert
		textBlock.Inlines
			.Should()
			.OnlyContain(static x => !x.IsSet(TextElement.ForegroundProperty));
	}

	/// <summary>
	/// <see cref="SearchHighlightBehavior.Query" />: each match is painted with the text highlight.
	/// </summary>
	[AvaloniaTest]
	public void Query_Paints_The_Matches_With_The_Text_Highlight()
	{
		// Arrange
		TextBlock textBlock = new();

		SearchHighlightBehavior sut = new()
		{
			SourceText = "Log and log"
		};

		Interaction
			.GetBehaviors(textBlock)
			.Add(sut);

		// Act
		sut.Query = "log";

		// Assert
		textBlock.Inlines!
			.Select(static x => x.Background)
			.Should()
			.Equal(TextHighlight.Brush, null, TextHighlight.Brush);
	}
	#endregion
}
