using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using System.Linq;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that the expander of a tree row is named for UI Automation by a tip of its state, and stays out of it in a row without children")]
internal class TreeViewItemAutomationTests
{
	#region Methods
	/// <summary>
	/// The expander of a row with children is named by its tip, and the names of a collapsed and an expanded row differ.
	/// </summary>
	[AvaloniaTest]
	public void Expander_Is_Named_By_A_Tip_Of_Its_State()
	{
		// Arrange
		TreeViewItem row = new()
		{
			Items =
			{
				new TreeViewItem()
			}
		};

		Show(row);

		// Act
		(string? Name, object? Tip)[] result =
		[
			ReadExpander(row, isExpanded: false),
			ReadExpander(row, isExpanded: true)
		];

		// Assert
		result
			.Should()
			.AllSatisfy(static x => x.Name.Should().NotBeNullOrEmpty().And.Be(x.Tip as string));

		result.Select(static x => x.Name)
			.Should()
			.OnlyHaveUniqueItems();
	}

	/// <summary>
	/// The expander, which Material only makes transparent in a row without children, stays in the tree of controls only
	/// while the row has children.
	/// </summary>
	[AvaloniaTest]
	public void Expander_Stays_In_The_Tree_Only_With_Children([Values] bool hasChildren)
	{
		// Arrange
		TreeViewItem row = new();

		if (hasChildren)
		{
			row.Items.Add(new TreeViewItem());
		}

		Show(row);

		// Act
		bool result = ControlAutomationPeer
			.CreatePeerForElement(GetExpander(row))
			.IsControlElement();

		// Assert
		result
			.Should()
			.Be(hasChildren);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the expander from the template of the row, the only toggle there.
	/// </summary>
	private static ToggleButton GetExpander(TreeViewItem row)
	{
		return row
			.GetVisualDescendants()
			.OfType<ToggleButton>()
			.Single(x => x.TemplatedParent == row);
	}

	/// <summary>
	/// Expands or collapses the row and returns the automation name and the tip of its expander.
	/// </summary>
	private static (string? Name, object? Tip) ReadExpander(TreeViewItem row, bool isExpanded)
	{
		row.IsExpanded = isExpanded;

		Dispatcher.UIThread.RunJobs();

		ToggleButton expander = GetExpander(row);

		return (ControlAutomationPeer.CreatePeerForElement(expander).GetName(), ToolTip.GetTip(expander));
	}

	/// <summary>
	/// Shows the row in a tree view of a window and lets the layout settle.
	/// </summary>
	private static void Show(TreeViewItem row)
	{
		TreeView treeView = new()
		{
			Items =
			{
				row
			}
		};

		HeadlessWindow.Show(treeView);
	}
	#endregion
}
