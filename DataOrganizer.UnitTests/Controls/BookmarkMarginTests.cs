using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Styling;
using DataOrganizer.Controls;
using System.Linq;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(BookmarkMargin)}"" type")]
internal class BookmarkMarginTests
{
	#region Data
	/// <summary>
	/// Resource key of the primary brush of the theme.
	/// </summary>
	private const string PrimaryBrushKey = "MaterialPrimaryMidBrush";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="BookmarkMargin" />: a click leaves the caret where it is.
	/// </summary>
	[AvaloniaTest]
	public void Click_Leaves_The_Caret_Alone()
	{
		// Arrange
		DocumentTextEditor editor = new()
		{
			Document = CreateDocument(lineCount: 10)
		};

		Window window = Show(editor);

		BookmarkMargin sut = GetMargin(editor);

		Point point = GetLinePoint(sut, line: 5, window);

		// Act
		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);

		// Assert
		editor.CaretOffset
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="BookmarkMargin" />: a click sets the bookmark of a line without one and removes the bookmark
	/// of a line with one, in read-only mode too.
	/// </summary>
	[AvaloniaTest]
	public void Click_Toggles_The_Bookmark_Of_The_Line([Values] bool isBookmarked, [Values] bool isReadOnly)
	{
		// Arrange
		DocumentTextEditor editor = new()
		{
			Document = CreateDocument(lineCount: 10),
			IsReadOnly = isReadOnly
		};

		Window window = Show(editor);

		BookmarkMargin sut = GetMargin(editor);

		if (isBookmarked)
		{
			editor.Bookmarks.Toggle(5);
		}

		Point point = GetLinePoint(sut, line: 5, window);

		// Act
		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);

		// Assert
		int[] expected = isBookmarked ? [] : [5];

		editor.Bookmarks.GetLines()
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="BookmarkMargin" />: the margin widens with the zoom, like the line numbers.
	/// </summary>
	[AvaloniaTest]
	public void DesiredSize_Follows_The_Font_Size()
	{
		// Arrange
		DocumentTextEditor editor = new()
		{
			Document = CreateDocument(lineCount: 10),
			FontSize = 14.0
		};

		Show(editor);

		BookmarkMargin sut = GetMargin(editor);

		double width = sut.DesiredSize.Width;

		// Act
		editor.FontSize = 28.0;

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.DesiredSize.Width
			.Should()
			.BeApproximately(width * 2.0, 1.0);
	}

	/// <summary>
	/// <see cref="BookmarkMargin" />: the icon under the pointer goes away when the pointer leaves the margin.
	/// </summary>
	[AvaloniaTest]
	public void Hover_Ends_When_The_Pointer_Leaves()
	{
		// Arrange
		DocumentTextEditor editor = new()
		{
			Document = CreateDocument(lineCount: 10)
		};

		Window window = Show(editor);

		BookmarkMargin sut = GetMargin(editor);

		window.MouseMove(GetLinePoint(sut, line: 5, window));

		// Act
		window.MouseMove(new(400.0, 300.0));

		// Assert
		GetIcons(Draw(sut))
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="BookmarkMargin" />: the pointer over a bookmarked line leaves its icon as it is.
	/// </summary>
	[AvaloniaTest]
	public void Hover_Keeps_The_Icon_Of_A_Bookmarked_Line()
	{
		// Arrange
		DocumentTextEditor editor = new()
		{
			Document = CreateDocument(lineCount: 10)
		};

		Window window = Show(editor);

		BookmarkMargin sut = GetMargin(editor);

		editor.Bookmarks.Toggle(5);

		// Act
		window.MouseMove(GetLinePoint(sut, line: 5, window));

		// Assert
		GetIcons(Draw(sut))
			.Select(static x => x.Opacity)
			.Should()
			.Equal(1.0);
	}

	/// <summary>
	/// <see cref="BookmarkMargin" />: a see-through icon on the line under the pointer shows where a click puts a bookmark.
	/// </summary>
	[AvaloniaTest]
	public void Hover_Previews_A_Bookmark_On_The_Line_Under_The_Pointer()
	{
		// Arrange
		DocumentTextEditor editor = new()
		{
			Document = CreateDocument(lineCount: 10)
		};

		Window window = Show(editor);

		BookmarkMargin sut = GetMargin(editor);

		// Act
		window.MouseMove(GetLinePoint(sut, line: 5, window));

		// Assert
		(Rect Bounds, double Opacity, IBrush? Brush) icon = GetIcons(Draw(sut))
			.Should()
			.ContainSingle()
			.Which;

		icon.Opacity
			.Should()
			.BeLessThan(1.0);

		icon.Bounds.Center.Y
			.Should()
			.BeApproximately(GetLineMiddle(sut, line: 5), 1.0);
	}

	/// <summary>
	/// <see cref="BookmarkMargin.Render" />: the bookmarked lines get the icon in the primary brush of the theme.
	/// </summary>
	[AvaloniaTest]
	public void Render_Draws_The_Icons_Of_The_Bookmarked_Lines()
	{
		// Arrange
		DocumentTextEditor editor = new()
		{
			Document = CreateDocument(lineCount: 10)
		};

		Show(editor);

		BookmarkMargin sut = GetMargin(editor);

		editor.Bookmarks.Toggle(2);

		editor.Bookmarks.Toggle(4);

		// Act
		(Rect Bounds, double Opacity, IBrush? Brush)[] icons = GetIcons(Draw(sut));

		// Assert
		icons
			.Select(static x => x.Bounds.Center.Y)
			.Should()
			.SatisfyRespectively(
				x => x.Should().BeApproximately(GetLineMiddle(sut, line: 2), 1.0),
				x => x.Should().BeApproximately(GetLineMiddle(sut, line: 4), 1.0));

		icons
			.Should()
			.AllSatisfy(static x => x.Brush
				.Should()
				.BeSameAs(Application.Current!.FindResource(PrimaryBrushKey)))
			.And
			.OnlyContain(static x => x.Opacity == 1.0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a document of numbered lines.
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
	private static DrawingGroup Draw(BookmarkMargin margin)
	{
		DrawingGroup drawing = new();

		using (DrawingContext context = drawing.Open())
		{
			margin.Render(context);
		}

		return drawing;
	}

	/// <summary>
	/// Returns the bounds, the opacity and the brush of each icon the margin paints.
	/// </summary>
	private static (Rect Bounds, double Opacity, IBrush? Brush)[] GetIcons(DrawingGroup drawing)
	{
		// An icon comes as a group with its opacity around a group with its transform.
		return [.. drawing.Children
			.OfType<DrawingGroup>()
			.Select(static x => (
				x.GetBounds(),
				x.Opacity,
				x.Children
					.OfType<DrawingGroup>()
					.Single()
					.Children
					.OfType<GeometryDrawing>()
					.Single()
					.Brush))];
	}

	/// <summary>
	/// Returns the height of the middle of a line in the margin.
	/// </summary>
	private static double GetLineMiddle(BookmarkMargin margin, int line)
	{
		TextView textView = margin.TextView!;

		return textView.GetVisualTopByDocumentLine(line) - textView.VerticalOffset + (textView.DefaultLineHeight / 2.0);
	}

	/// <summary>
	/// Returns the point of the window over the middle of a line in the margin.
	/// </summary>
	private static Point GetLinePoint(BookmarkMargin margin, int line, Window window)
	{
		return margin.TranslatePoint(
			new(
				margin.Bounds.Width / 2.0,
				GetLineMiddle(margin, line)),
			window) ?? default;
	}

	/// <summary>
	/// Returns the bookmark margin of the editor.
	/// </summary>
	private static BookmarkMargin GetMargin(DocumentTextEditor editor)
	{
		return editor
			.TextArea
			.LeftMargins
			.OfType<BookmarkMargin>()
			.Single();
	}

	/// <summary>
	/// Shows the editor in its theme in a window of a fixed size and lets the layout settle.
	/// </summary>
	private static Window Show(DocumentTextEditor editor)
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
}
