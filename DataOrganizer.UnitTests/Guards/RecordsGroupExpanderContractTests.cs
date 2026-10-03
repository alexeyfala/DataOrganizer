using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Input;
using DataOrganizer.Behaviors.Styling;
using System.Linq;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that the records group expander still finds its way around Material's expander")]
internal class RecordsGroupExpanderContractTests
{
	#region Data
	/// <summary>
	/// Name of the button that Material's expander shows its header in.
	/// </summary>
	private const string HeaderButtonName = "PART_ToggleButton";

	/// <summary>
	/// Name of the root of the header that the template builds.
	/// </summary>
	private const string HeaderName = "Header";

	/// <summary>
	/// Class of the style of the expander of a records group.
	/// </summary>
	private const string StyleClass = "RecordsGroupExpanderStyle";

	/// <summary>
	/// Name of the chevron that Material's expander draws in its header button.
	/// </summary>
	private const string ThemeChevronName = "PART_ExpandIcon";
	#endregion

	#region Methods
	/// <summary>
	/// <c>RecordsGroupExpanderStyle</c>: the chevron of Material's header button stays hidden, as the header of a group
	/// has a chevron of its own.
	/// </summary>
	[AvaloniaTest]
	public void Chevron_Of_The_Theme_Stays_Hidden()
	{
		// Arrange
		Expander sut = CreateExpander();

		// Act
		Show(sut);

		// Assert
		Path chevron = sut
			.GetVisualDescendants()
			.OfType<Path>()
			.Single(static x => x.Name == ThemeChevronName);

		chevron.IsEffectivelyVisible
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ExpanderToggleCursorBehavior" />: Material's header button shows the arrow rather than the hand of the
	/// theme.
	/// </summary>
	[AvaloniaTest]
	public void Header_Button_Shows_The_Arrow_Cursor()
	{
		// Arrange
		Expander sut = CreateExpander();

		// Act
		Show(sut);

		// Assert
		ToggleButton button = sut
			.GetVisualDescendants()
			.OfType<ToggleButton>()
			.Single();

		// A local keeps the assertion from being skipped by the null-conditional operator when there is no cursor.
		string? cursor = button
			.Cursor?
			.ToString();

		cursor
			.Should()
			.Be(nameof(StandardCursorType.Arrow));
	}

	/// <summary>
	/// <see cref="ExpanderDoubleClickToggleBehavior" />: the header sits in Material's header button under the name the
	/// behaviors look for.
	/// </summary>
	[AvaloniaTest]
	public void Header_Sits_In_The_Button_The_Behaviors_Look_For()
	{
		// Arrange
		Expander sut = CreateExpander();

		// Act
		Show(sut);

		// Assert
		GetHeader(sut).Parent
			.Should()
			.BeOfType<ToggleButton>()
			.Which
			.Name
			.Should()
			.Be(HeaderButtonName);
	}

	/// <summary>
	/// <see cref="ExpanderDoubleClickToggleBehavior" />: the header toggles the expander on a double click only, as a
	/// single press stays away from Material's header button.
	/// </summary>
	[AvaloniaTest]
	public void Header_Toggles_The_Expander_On_A_Double_Click_Only([Values] bool isDoubleClick)
	{
		// Arrange
		Expander sut = CreateExpander();

		Window window = Show(sut);

		Point point = Center(window, GetHeader(sut));

		// Act
		Click(window, point);

		if (isDoubleClick)
		{
			Click(window, point);
		}

		// Assert
		sut.IsExpanded
			.Should()
			.Be(isDoubleClick);
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
	/// Presses and releases the left button of the mouse at a point of a window.
	/// </summary>
	private static void Click(Window window, Point point)
	{
		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);
	}

	/// <summary>
	/// Creates an expander set up as the one of a records group: the style and the cursor behavior on the expander, and
	/// the double click behavior on a header that a template builds from data.
	/// </summary>
	private static Expander CreateExpander()
	{
		Expander expander = new()
		{
			Classes =
			{
				StyleClass
			},
			Content = "Records",
			Header = "Group",
			HeaderTemplate = new FuncDataTemplate<string>(static (text, _) =>
			{
				// A background lets the header take the presses, as the hover gives the header of a group one.
				Grid header = new()
				{
					Background = Brushes.Transparent,
					Children =
					{
						new TextBlock
						{
							Text = text
						}
					},
					Name = HeaderName
				};

				Interaction
					.GetBehaviors(header)
					.Add(new ExpanderDoubleClickToggleBehavior());

				return header;
			})
		};

		Interaction
			.GetBehaviors(expander)
			.Add(new ExpanderToggleCursorBehavior());

		return expander;
	}

	/// <summary>
	/// Returns the root of the header that the template built.
	/// </summary>
	private static Grid GetHeader(Expander expander)
	{
		return expander
			.GetVisualDescendants()
			.OfType<Grid>()
			.Single(static x => x.Name == HeaderName);
	}

	/// <summary>
	/// Shows the expander in a window of a fixed size and lets the layout settle.
	/// </summary>
	private static Window Show(Expander expander)
	{
		Window window = new()
		{
			Content = expander,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		return window;
	}
	#endregion
}
