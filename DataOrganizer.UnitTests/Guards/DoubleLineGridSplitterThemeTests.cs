using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.UnitTests.Factories;
using System.Linq;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards the look of the double line splitter: a filled band between two lines, with the grip of the single line splitter")]
internal class DoubleLineGridSplitterThemeTests
{
	#region Data
	/// <summary>
	/// Resource key of the brush that fills the band between the lines.
	/// </summary>
	private const string FillBrushKey = "MaterialTextFieldBoxBackgroundBrush";

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
	/// The band between the lines is filled.
	/// </summary>
	[AvaloniaTest]
	public void Band_Is_Filled_Between_The_Lines()
	{
		// Arrange
		GridSplitter sut = new()
		{
			ResizeDirection = GridResizeDirection.Rows,
			Theme = GetTheme()
		};

		// Act
		GridSplitterHost.Show(sut);

		// Assert
		sut.GetVisualChildren().OfType<Panel>().Single().Background
			.Should()
			.BeSameAs(Application.Current!.FindResource(FillBrushKey));
	}

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
		GridSplitterHost.Show(sut);

		// Assert
		Size grip = GridSplitterHost.GetPart(sut, GripPartName).Bounds.Size;

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

		Window window = GridSplitterHost.Show(sut);

		// Act
		window.MouseMove(window.CenterOf(sut));

		Dispatcher.UIThread.RunJobs();

		// Assert
		Border grip = GridSplitterHost.GetPart(sut, GripPartName);

		GridSplitterHost.GetStyledValue(grip, Visual.OpacityProperty)
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
		GridSplitterHost.Show(sut);

		// Assert
		Border grip = GridSplitterHost.GetPart(sut, GripPartName);

		GridSplitterHost.GetStyledValue(grip, Visual.OpacityProperty)
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

		Window window = GridSplitterHost.Show(sut);

		// Act
		window.MouseDown(window.CenterOf(sut), MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		Border grip = GridSplitterHost.GetPart(sut, GripPartName);

		GridSplitterHost.GetStyledValue(grip, Border.BackgroundProperty)
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
		GridSplitterHost.Show(sut);

		// Assert
		GridSplitterHost.GetPart(sut, LinesPartName).BorderThickness
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
		GridSplitterHost.Show(sut);

		// Assert
		GridSplitterHost.GetPart(sut, LinesPartName).BorderThickness
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

		Window window = GridSplitterHost.Show(sut);

		// Act
		window.MouseDown(window.CenterOf(sut), MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		Border lines = GridSplitterHost.GetPart(sut, LinesPartName);

		GridSplitterHost.GetStyledValue(lines, Border.BorderBrushProperty)
			.Should()
			.BeSameAs(Application.Current!.FindResource(PrimaryBrushKey));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the theme under test.
	/// </summary>
	private static ControlTheme GetTheme() => (ControlTheme)Application.Current!.FindResource(ThemeKey)!;
	#endregion
}
