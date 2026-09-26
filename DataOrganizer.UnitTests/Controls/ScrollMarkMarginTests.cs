using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Styling;
using DataOrganizer.Controls;
using DataOrganizer.Helpers.Text;
using System.Linq;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(ScrollMarkMargin)}"" type")]
internal class ScrollMarkMarginTests
{
	#region Data
	/// <summary>
	/// Name of the scroll viewer in the template of the text editor.
	/// </summary>
	private const string ScrollViewerName = "PART_ScrollViewer";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ScrollMarkMargin" />: a click brings its place of the document to the middle of the view
	/// and leaves the caret where it is.
	/// </summary>
	[AvaloniaTest]
	public void Click_Scrolls_To_The_Place_Without_Moving_The_Caret()
	{
		// Arrange
		TestTextEditor editor = new()
		{
			Document = CreateDocument(lineCount: 1000)
		};

		Window window = Show(editor);

		ScrollMarkMargin sut = GetMargin(editor);

		ScrollViewer scrollViewer = GetScrollViewer(editor);

		// The middle of the margin is the middle of the scroll bar track.
		Point point = sut.TranslatePoint(
			new(
				sut.Bounds.Width / 2.0,
				sut.Bounds.Height / 2.0),
			window) ?? default;

		// Act
		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		scrollViewer.Offset.Y
			.Should()
			.BeApproximately(
				(scrollViewer.Extent.Height - scrollViewer.Viewport.Height) / 2.0,
				editor.TextArea.TextView.DefaultLineHeight);

		editor.TextArea.Caret.Offset
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="ScrollMarkMargin.Render" />: the caret line stands at the place of the caret in the whole document.
	/// </summary>
	[AvaloniaTest]
	public void Render_Draws_The_Caret_Line()
	{
		// Arrange
		TestTextEditor editor = new()
		{
			Document = CreateDocument(lineCount: 1000)
		};

		Show(editor);

		ScrollMarkMargin sut = GetMargin(editor);

		editor.CaretOffset = editor.Document.GetLineByNumber(500).Offset;

		// Act
		double[] rows = GetRows(Draw(sut), ((ISolidColorBrush)editor.TextArea.Foreground!).Color);

		// Assert
		rows
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeApproximately(sut.Bounds.Height / 2.0, 2.0);
	}

	/// <summary>
	/// <see cref="ScrollMarkMargin.Render" />: the occurrences of the selected text are marked in the whole document,
	/// out of the view too.
	/// </summary>
	[AvaloniaTest]
	public void Render_Marks_Every_Occurrence_In_The_Document()
	{
		// Arrange
		TestTextEditor editor = new()
		{
			Document = new(string.Join('\n', Enumerable
				.Range(1, 1000)
				.Select(static x => x is 1 or 500 or 1000 ? "needle" : $"Line {x:D4}")))
		};

		Show(editor);

		ScrollMarkMargin sut = GetMargin(editor);

		editor.Select(0, 6);

		sut.UpdateMarks();

		double height = sut.Bounds.Height;

		// Act
		double[] rows = GetRows(Draw(sut), TextHighlight.MarkBrush.Color);

		// Assert
		rows
			.Should()
			.SatisfyRespectively(
				x => x.Should().BeLessThan(height * 0.1),
				x => x.Should().BeApproximately(height / 2.0, height * 0.05),
				x => x.Should().BeGreaterThan(height * 0.9));
	}

	/// <summary>
	/// <see cref="ScrollMarkMargin.Render" />: the marks that fall on one row of pixels are drawn once.
	/// </summary>
	[AvaloniaTest]
	public void Render_Merges_The_Marks_Of_One_Row()
	{
		// Arrange
		TestTextEditor editor = new()
		{
			Document = new(string.Join('\n', Enumerable.Repeat("needle", 2000)))
		};

		Show(editor);

		ScrollMarkMargin sut = GetMargin(editor);

		editor.Select(0, 6);

		sut.UpdateMarks();

		// Act
		double[] rows = GetRows(Draw(sut), TextHighlight.MarkBrush.Color);

		// Assert
		rows
			.Should()
			.NotBeEmpty()
			.And
			.OnlyHaveUniqueItems();
	}

	/// <summary>
	/// <see cref="ScrollMarkMargin.Render" />: the marks of the visible lines fall on the thumb of the scroll bar,
	/// as in Visual Studio.
	/// </summary>
	[AvaloniaTest]
	public void Render_Puts_The_Marks_Of_The_Visible_Lines_On_The_Thumb()
	{
		// Arrange
		TestTextEditor editor = new()
		{
			Document = new(string.Join('\n', Enumerable
				.Range(1, 1000)
				.Select(static x => x <= 3 ? "needle" : $"Line {x:D4}")))
		};

		Show(editor);

		ScrollMarkMargin sut = GetMargin(editor);

		editor.Select(0, 6);

		sut.UpdateMarks();

		Thumb thumb = GetScrollViewer(editor)
			.GetTemplateDescendants()
			.OfType<ScrollBar>()
			.First(static x => x.Orientation == Orientation.Vertical)
			.GetTemplateDescendants()
			.OfType<Thumb>()
			.First();

		double thumbTop = thumb.TranslatePoint(default, sut)?.Y ?? double.NaN;

		// Act
		double[] rows = GetRows(Draw(sut), TextHighlight.MarkBrush.Color);

		// Assert
		rows
			.Should()
			.NotBeEmpty()
			.And
			.OnlyContain(x => x >= thumbTop && x <= thumbTop + thumb.Bounds.Height);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a document of numbered lines of the same length.
	/// </summary>
	private static TextDocument CreateDocument(int lineCount)
	{
		return new(string.Join('\n', Enumerable
			.Range(1, lineCount)
			.Select(static x => $"Line {x:D4}")));
	}

	/// <summary>
	/// Records what the margin paints.
	/// </summary>
	private static DrawingGroup Draw(ScrollMarkMargin margin)
	{
		DrawingGroup drawing = new();

		using (DrawingContext context = drawing.Open())
		{
			margin.Render(context);
		}

		return drawing;
	}

	/// <summary>
	/// Returns the scroll mark margin of the editor.
	/// </summary>
	private static ScrollMarkMargin GetMargin(TestTextEditor editor)
	{
		return editor
			.GetVisualChildren()
			.OfType<ScrollMarkMargin>()
			.Single();
	}

	/// <summary>
	/// Returns the middle rows of the shapes painted in the color.
	/// </summary>
	private static double[] GetRows(DrawingGroup drawing, Color color)
	{
		return [.. drawing.Children
			.OfType<GeometryDrawing>()
			.Where(x => x.Brush is ISolidColorBrush brush && brush.Color == color)
			.Select(static x => x.GetBounds().Center.Y)];
	}

	/// <summary>
	/// Returns the scroll viewer of the editor.
	/// </summary>
	private static ScrollViewer GetScrollViewer(TestTextEditor editor)
	{
		return editor
			.GetVisualDescendants()
			.OfType<ScrollViewer>()
			.First(static x => x.Name == ScrollViewerName);
	}

	/// <summary>
	/// Shows the editor in its theme in a window of a fixed size and lets the layout settle.
	/// </summary>
	private static Window Show(TestTextEditor editor)
	{
		Interaction
			.GetBehaviors(editor)
			.Add(new FluentThemeBehavior());

		Window window = new()
		{
			Content = editor,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		return window;
	}
	#endregion

	#region Nested Types
	/// <summary>
	/// Minimal concrete <see cref="TextEditorBase" />.
	/// </summary>
	private sealed class TestTextEditor : TextEditorBase;
	#endregion
}
