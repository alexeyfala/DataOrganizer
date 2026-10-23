using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Styling;
using System.Linq;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that the arrows, the page buttons and the thumb of a scroll bar stay out of UI Automation, while the scroll bar stays in it")]
internal class ScrollBarAutomationTests
{
	#region Methods
	/// <summary>
	/// The parts of a scroll bar of the Fluent theme, which names them in English, stay out of the tree of controls.
	/// </summary>
	[AvaloniaTest]
	public void Parts_Of_A_Fluent_Scroll_Bar_Stay_Out_Of_The_Tree()
	{
		// Arrange
		ScrollBar scrollBar = new();

		Panel panel = new()
		{
			Children =
			{
				scrollBar
			}
		};

		// The text editors take the Fluent theme the same way.
		Interaction
			.GetBehaviors(panel)
			.Add(new FluentThemeBehavior());

		Show(panel);

		// Act
		bool[] result = [.. GetParts(scrollBar).Select(static x => ControlAutomationPeer.CreatePeerForElement(x).IsControlElement())];

		// Assert
		result
			.Should()
			.NotBeEmpty()
			.And
			.NotContain(true);
	}

	/// <summary>
	/// The parts of a scroll bar of the Material theme, which names its arrows by the type of their content and leaves the
	/// rest unnamed, stay out of the tree of controls.
	/// </summary>
	[AvaloniaTest]
	public void Parts_Of_A_Material_Scroll_Bar_Stay_Out_Of_The_Tree()
	{
		// Arrange
		ScrollBar scrollBar = new();

		Show(scrollBar);

		// Act
		bool[] result = [.. GetParts(scrollBar).Select(static x => ControlAutomationPeer.CreatePeerForElement(x).IsControlElement())];

		// Assert
		result
			.Should()
			.NotBeEmpty()
			.And
			.NotContain(true);
	}

	/// <summary>
	/// The scroll bar itself stays in the tree of controls, with its value.
	/// </summary>
	[AvaloniaTest]
	public void Scroll_Bar_Stays_In_The_Tree()
	{
		// Arrange
		ScrollBar scrollBar = new();

		Show(scrollBar);

		// Act
		bool result = ControlAutomationPeer
			.CreatePeerForElement(scrollBar)
			.IsControlElement();

		// Assert
		result
			.Should()
			.BeTrue();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the arrows, the page buttons and the thumb of the scroll bar.
	/// </summary>
	private static Control[] GetParts(ScrollBar scrollBar)
	{
		return [.. scrollBar
			.GetVisualDescendants()
			.OfType<Control>()
			.Where(static x => x is RepeatButton or Thumb)];
	}

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
