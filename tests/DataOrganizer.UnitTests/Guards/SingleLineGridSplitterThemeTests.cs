using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.UnitTests.Factories;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards the look of the single line splitter: a hairline with a grip that shows on hover and takes the primary brush when pressed")]
internal class SingleLineGridSplitterThemeTests
{
	#region Data
	/// <summary>
	/// Name of the template part that shows where to grab the splitter.
	/// </summary>
	private const string GripPartName = "PART_Grip";

	/// <summary>
	/// Name of the template part that divides the two sides.
	/// </summary>
	private const string LinePartName = "PART_Line";

	/// <summary>
	/// Resource key of the primary brush of the theme.
	/// </summary>
	private const string PrimaryBrushKey = "MaterialPrimaryMidBrush";

	/// <summary>
	/// Resource key of the theme under test.
	/// </summary>
	private const string ThemeKey = "SingleLineGridSplitterTheme";
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
			ResizeDirection = GridResizeDirection.Columns,
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
	/// The grip of a splitter between columns stands upright.
	/// </summary>
	[AvaloniaTest]
	public void Grip_Stands_Upright_Between_Columns()
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
		Size grip = GridSplitterHost.GetPart(sut, GripPartName).Bounds.Size;

		grip.Height
			.Should()
			.BeGreaterThan(grip.Width);
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
			ResizeDirection = GridResizeDirection.Columns,
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
	/// The line between rows is a hairline across the whole width of the splitter.
	/// </summary>
	[AvaloniaTest]
	public void Line_Runs_Across_Between_Rows()
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
		GridSplitterHost.GetPart(sut, LinePartName).Bounds.Size
			.Should()
			.Be(new Size(
				width: sut.Bounds.Width,
				height: 1.0));
	}

	/// <summary>
	/// The line between columns is a hairline down the whole height of the splitter.
	/// </summary>
	[AvaloniaTest]
	public void Line_Runs_Down_Between_Columns()
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
		GridSplitterHost.GetPart(sut, LinePartName).Bounds.Size
			.Should()
			.Be(new Size(
				width: 1.0,
				height: sut.Bounds.Height));
	}

	/// <summary>
	/// The part takes the primary brush while the splitter is pressed.
	/// </summary>
	[AvaloniaTest]
	public void Part_Takes_The_Primary_Brush_When_Pressed([Values(GripPartName, LinePartName)] string partName)
	{
		// Arrange
		GridSplitter sut = new()
		{
			ResizeDirection = GridResizeDirection.Columns,
			Theme = GetTheme()
		};

		Window window = GridSplitterHost.Show(sut);

		// Act
		window.MouseDown(window.CenterOf(sut), MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		Border part = GridSplitterHost.GetPart(sut, partName);

		GridSplitterHost.GetStyledValue(part, Border.BackgroundProperty)
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
