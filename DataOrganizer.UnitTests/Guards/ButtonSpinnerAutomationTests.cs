using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Styling;
using System.Linq;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that the arrows of a button spinner are named for UI Automation by tips of their own")]
internal class ButtonSpinnerAutomationTests
{
	#region Methods
	/// <summary>
	/// The two arrows of a button spinner, which both themes name by the type of their content, are named by their tips,
	/// and the names differ.
	/// </summary>
	[AvaloniaTest]
	public void Arrows_Are_Named_By_Tips_Of_Their_Own([Values] bool isFluent)
	{
		// Arrange
		ButtonSpinner spinner = new();

		Panel panel = new()
		{
			Children =
			{
				spinner
			}
		};

		// The console takes the Fluent theme this way; the editor toolbar keeps the theme of the application.
		if (isFluent)
		{
			Interaction
				.GetBehaviors(panel)
				.Add(new FluentThemeBehavior());
		}

		Show(panel);

		// Act
		(string? Name, object? Tip)[] result =
		[
			.. spinner
				.GetVisualDescendants()
				.OfType<RepeatButton>()
				.Select(static x => (ControlAutomationPeer.CreatePeerForElement(x).GetName(), ToolTip.GetTip(x)))
		];

		// Assert
		result
			.Should()
			.HaveCount(2)
			.And
			.AllSatisfy(static x => x.Name.Should().NotBeNullOrEmpty().And.Be(x.Tip as string));

		result.Select(static x => x.Name)
			.Should()
			.OnlyHaveUniqueItems();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Shows the content in a window and lets the layout settle.
	/// </summary>
	private static void Show(Control content)
	{
		Window window = new()
		{
			Content = content
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();
	}
	#endregion
}
