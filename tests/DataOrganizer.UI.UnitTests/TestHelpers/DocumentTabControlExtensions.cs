using Avalonia.Controls;
using Avalonia.VisualTree;
using DataOrganizer.UI.Controls;
using System.Linq;

namespace DataOrganizer.UI.UnitTests.TestHelpers;

/// <summary>
/// Parts of a document tab control that a test finds.
/// </summary>
internal static class DocumentTabControlExtensions
{
	#region Methods
	/// <summary>
	/// Returns the button that adds a tab.
	/// </summary>
	public static Button GetAddButton(this DocumentTabControl control)
	{
		return control
			.GetVisualDescendants()
			.OfType<Button>()
			.Single(x => x.Name == "PART_AddButton");
	}

	/// <summary>
	/// Returns the close button of a tab.
	/// </summary>
	public static Button GetCloseButton(this DocumentTabControl control, int index)
	{
		return control
			.ContainerFromIndex(index)!
			.GetVisualDescendants()
			.OfType<Button>()
			.Single();
	}

	/// <summary>
	/// Returns the text block of the tab header that shows the text.
	/// </summary>
	public static TextBlock GetHeaderText(this DocumentTabControl control, string text)
	{
		return control
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.Single(x => x.Text == text && x.FindAncestorOfType<TabItem>() is not null);
	}
	#endregion
}
