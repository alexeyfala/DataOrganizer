using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Linq;

namespace DataOrganizer.UnitTests.Guards;

/// <summary>
/// Shows a grid splitter under test between two cells of a grid and reads its template.
/// </summary>
internal static class GridSplitterHost
{
	#region Methods
	/// <summary>
	/// Returns the part of the template of the splitter with the name.
	/// </summary>
	public static Border GetPart(GridSplitter splitter, string name)
	{
		return splitter
			.GetVisualDescendants()
			.OfType<Border>()
			.Single(x => x.Name == name);
	}

	/// <summary>
	/// Returns the value the styles give a property, which a transition reaches only later.
	/// </summary>
	public static T GetStyledValue<T>(AvaloniaObject target, StyledProperty<T> property) => target.GetBaseValue(property).Value;

	/// <summary>
	/// Shows the splitter between two cells of a grid in a window of a fixed size and lets the layout settle.
	/// </summary>
	public static Window Show(GridSplitter splitter)
	{
		Grid grid = new()
		{
			Children =
			{
				splitter
			}
		};

		if (splitter.ResizeDirection == GridResizeDirection.Rows)
		{
			grid.RowDefinitions = new("*,Auto,*");

			Grid.SetRow(splitter, 1);
		}
		else
		{
			grid.ColumnDefinitions = new("*,Auto,*");

			Grid.SetColumn(splitter, 1);
		}

		return HeadlessWindow.Show(grid);
	}
	#endregion
}
