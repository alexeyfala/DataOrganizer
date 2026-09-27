using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using System.Linq;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards the look of the double line splitter: a line along the edge of each side, with the grip of the single line splitter")]
internal class DoubleLineGridSplitterThemeTests
{
	#region Data
	/// <summary>
	/// Name of the template part that shows where to grab the splitter.
	/// </summary>
	private const string GripPartName = "PART_Grip";

	/// <summary>
	/// Name of the template part that draws the lines along the edges of the sides.
	/// </summary>
	private const string LinesPartName = "PART_Lines";

	/// <summary>
	/// Resource key of the primary brush of the theme.
	/// </summary>
	private const string PrimaryBrushKey = "MaterialPrimaryMidBrush";

	/// <summary>
	/// Resource key of the theme under test.
	/// </summary>
	private const string ThemeKey = "DoubleLineGridSplitterTheme";
	#endregion

	#region Methods
	/// <summary>
	/// The grip of a splitter between rows lies flat.
	/// </summary>
	[AvaloniaTest]
	public void Grip_Lies_Flat_Between_Rows()
	{
		// Arrange
		GridSplitter sut = new()
		{
			ResizeDirection = GridResizeDirection.Rows,
			Theme = GetTheme()
		};

		// Act
		Show(sut);

		// Assert
		Size grip = GetPart(sut, GripPartName).Bounds.Size;

		grip.Width
			.Should()
			.BeGreaterThan(grip.Height);
	}

	/// <summary>
	/// The grip shows while the pointer is over the splitter.
	/// </summary>
	[AvaloniaTest]
	public void Grip_Shows_On_Hover()
	{
		// Arrange
		GridSplitter sut = new()
		{
			ResizeDirection = GridResizeDirection.Rows,
			Theme = GetTheme()
		};

		Window window = Show(sut);

		// Act
		window.MouseMove(Center(window, sut));

		Dispatcher.UIThread.RunJobs();

		// Assert
		Border grip = GetPart(sut, GripPartName);

		GetStyledValue(grip, Visual.OpacityProperty)
			.Should()
			.Be(1.0);
	}

	/// <summary>
	/// The grip of a splitter at rest stays hidden.
	/// </summary>
	[AvaloniaTest]
	public void Grip_Stays_Hidden_At_Rest()
	{
		// Arrange
		GridSplitter sut = new()
		{
			ResizeDirection = GridResizeDirection.Rows,
			Theme = GetTheme()
		};

		// Act
		Show(sut);

		// Assert
		Border grip = GetPart(sut, GripPartName);

		GetStyledValue(grip, Visual.OpacityProperty)
			.Should()
			.Be(0.0);
	}

	/// <summary>
	/// The grip takes the primary brush while the splitter is pressed.
	/// </summary>
	[AvaloniaTest]
	public void Grip_Takes_The_Primary_Brush_When_Pressed()
	{
		// Arrange
		GridSplitter sut = new()
		{
			ResizeDirection = GridResizeDirection.Rows,
			Theme = GetTheme()
		};

		Window window = Show(sut);

		// Act
		window.MouseDown(Center(window, sut), MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		Border grip = GetPart(sut, GripPartName);

		GetStyledValue(grip, Border.BackgroundProperty)
			.Should()
			.BeSameAs(Application.Current!.FindResource(PrimaryBrushKey));
	}

	/// <summary>
	/// Between columns, the lines lie on the left and the right edge.
	/// </summary>
	[AvaloniaTest]
	public void Lines_Lie_On_Both_Edges_Between_Columns()
	{
		// Arrange
		GridSplitter sut = new()
		{
			ResizeDirection = GridResizeDirection.Columns,
			Theme = GetTheme()
		};

		// Act
		Show(sut);

		// Assert
		GetPart(sut, LinesPartName).BorderThickness
			.Should()
			.Be(new Thickness(
				horizontal: 1.0,
				vertical: 0.0));
	}

	/// <summary>
	/// Between rows, the lines lie on the upper and the lower edge.
	/// </summary>
	[AvaloniaTest]
	public void Lines_Lie_On_Both_Edges_Between_Rows()
	{
		// Arrange
		GridSplitter sut = new()
		{
			ResizeDirection = GridResizeDirection.Rows,
			Theme = GetTheme()
		};

		// Act
		Show(sut);

		// Assert
		GetPart(sut, LinesPartName).BorderThickness
			.Should()
			.Be(new Thickness(
				horizontal: 0.0,
				vertical: 1.0));
	}

	/// <summary>
	/// The lines take the primary brush while the splitter is pressed.
	/// </summary>
	[AvaloniaTest]
	public void Lines_Take_The_Primary_Brush_When_Pressed()
	{
		// Arrange
		GridSplitter sut = new()
		{
			ResizeDirection = GridResizeDirection.Rows,
			Theme = GetTheme()
		};

		Window window = Show(sut);

		// Act
		window.MouseDown(Center(window, sut), MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		Border lines = GetPart(sut, LinesPartName);

		GetStyledValue(lines, Border.BorderBrushProperty)
			.Should()
			.BeSameAs(Application.Current!.FindResource(PrimaryBrushKey));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the point in the middle of the control.
	/// </summary>
	private static Point Center(Visual root, Visual target)
	{
		return target.TranslatePoint(
			new(
				target.Bounds.Width / 2.0,
				target.Bounds.Height / 2.0),
			root) ?? default;
	}

	/// <summary>
	/// Returns the part of the template of the splitter with the name.
	/// </summary>
	private static Border GetPart(GridSplitter splitter, string name)
	{
		return splitter
			.GetVisualDescendants()
			.OfType<Border>()
			.Single(x => x.Name == name);
	}

	/// <summary>
	/// Returns the value the styles give a property, which a transition reaches only later.
	/// </summary>
	private static T GetStyledValue<T>(AvaloniaObject target, StyledProperty<T> property) => target.GetBaseValue(property).Value;

	/// <summary>
	/// Returns the theme under test.
	/// </summary>
	private static ControlTheme GetTheme() => (ControlTheme)Application.Current!.FindResource(ThemeKey)!;

	/// <summary>
	/// Shows the splitter between two cells of a grid in a window of a fixed size and lets the layout settle.
	/// </summary>
	private static Window Show(GridSplitter splitter)
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

		Window window = new()
		{
			Content = grid,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		return window;
	}
	#endregion
}
