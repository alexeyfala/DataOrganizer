using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.NUnit;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.UI.Behaviors.Text;
using DataOrganizer.UI.Controls.Text;
using System.Linq;

namespace DataOrganizer.UI.UnitTests.Behaviors.Text;

[TestFixture(Description = $@"Tests of ""{nameof(MatchHighlightBehavior)}"" type")]
internal class MatchHighlightBehaviorTests
{
	#region Methods
	/// <summary>
	/// <see cref="MatchHighlightBehavior.QueryProperty" />: the matches of a new text are highlighted.
	/// </summary>
	[AvaloniaTest]
	public void Query_Highlights_The_Matches_Of_A_New_Text()
	{
		// Arrange
		TextBlock textBlock = new()
		{
			Text = "Log and log"
		};

		Interaction
			.GetBehaviors(textBlock)
			.Add(new MatchHighlightBehavior());

		MatchHighlightBehavior.SetQuery(textBlock, "log");

		// Act
		textBlock.Text = "Blog";

		// Assert
		textBlock.Inlines!
			.Cast<Run>()
			.Select(static x => (x.Text, x.Background))
			.Should()
			.Equal(("B", null), ("log", TextHighlight.Brush));
	}

	/// <summary>
	/// <see cref="MatchHighlightBehavior.QueryProperty" />: the text stays whole, as the tip of a trimmed name reads it.
	/// </summary>
	[AvaloniaTest]
	public void Query_Keeps_The_Whole_Text()
	{
		// Arrange
		TextBlock textBlock = new()
		{
			Text = "Log and log"
		};

		Interaction
			.GetBehaviors(textBlock)
			.Add(new MatchHighlightBehavior());

		// Act
		MatchHighlightBehavior.SetQuery(textBlock, "log");

		// Assert
		textBlock.Text
			.Should()
			.Be("Log and log");
	}

	/// <summary>
	/// <see cref="MatchHighlightBehavior.QueryProperty" />: each match is painted with the text highlight.
	/// </summary>
	[AvaloniaTest]
	public void Query_Paints_The_Matches_With_The_Text_Highlight()
	{
		// Arrange
		TextBlock textBlock = new()
		{
			Text = "Log and log"
		};

		Interaction
			.GetBehaviors(textBlock)
			.Add(new MatchHighlightBehavior());

		// Act
		MatchHighlightBehavior.SetQuery(textBlock, "log");

		// Assert
		textBlock.Inlines!
			.Select(static x => x.Background)
			.Should()
			.Equal(TextHighlight.Brush, null, TextHighlight.Brush);
	}

	/// <summary>
	/// <see cref="MatchHighlightBehavior.QueryProperty" />: a query set on an ancestor reaches the text blocks inside it.
	/// </summary>
	[AvaloniaTest]
	public void Query_Reaches_The_Text_Blocks_Inside_The_Ancestor()
	{
		// Arrange
		TextBlock textBlock = new()
		{
			Text = "Log and log"
		};

		Interaction
			.GetBehaviors(textBlock)
			.Add(new MatchHighlightBehavior());

		Border ancestor = new()
		{
			Child = textBlock
		};

		// Act
		MatchHighlightBehavior.SetQuery(ancestor, "log");

		// Assert
		textBlock.Inlines!
			.Select(static x => x.Background)
			.Should()
			.Equal(TextHighlight.Brush, null, TextHighlight.Brush);
	}

	/// <summary>
	/// <see cref="MatchHighlightBehavior.QueryProperty" />: a removed query leaves the text without runs.
	/// </summary>
	[AvaloniaTest]
	public void Query_Removed_Leaves_The_Text_Without_Runs()
	{
		// Arrange
		TextBlock textBlock = new()
		{
			Text = "Log and log"
		};

		Interaction
			.GetBehaviors(textBlock)
			.Add(new MatchHighlightBehavior());

		MatchHighlightBehavior.SetQuery(textBlock, "log");

		// Act
		MatchHighlightBehavior.SetQuery(textBlock, null);

		// Assert
		textBlock.Inlines
			.Should()
			.BeNull();
	}
	#endregion
}
