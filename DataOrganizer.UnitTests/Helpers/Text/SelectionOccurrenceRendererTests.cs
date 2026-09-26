using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Styling;
using DataOrganizer.Helpers.Text;
using System.Linq;

namespace DataOrganizer.UnitTests.Helpers.Text;

[TestFixture(Description = $@"Tests of ""{nameof(SelectionOccurrenceRenderer)}"" type")]
internal class SelectionOccurrenceRendererTests
{
	internal static readonly int[] sourceArray = new[] { 4, 15 };
	#region Methods
	/// <summary>
	/// <see cref="SelectionOccurrenceRenderer.Draw" />: paints a shape over each occurrence.
	/// </summary>
	[AvaloniaTest]
	public void Draw_Paints_A_Shape_Over_Each_Occurrence()
	{
		// Arrange
		TextEditor editor = new()
		{
			Document = new("log Log logger LOG")
		};

		Show(editor);

		editor.Select(0, 3);

		Dispatcher.UIThread.RunJobs();

		TextView textView = editor.TextArea.TextView;

		SelectionOccurrenceRenderer sut = new(editor.TextArea);

		DrawingGroup drawing = new();

		// Act
		using (DrawingContext context = drawing.Open())
		{
			sut.Draw(textView, context);
		}

		// Assert
		Point[] centers = [.. sourceArray.Select(x => BackgroundGeometryBuilder
			.GetRectsForSegment(textView, new SimpleSegment(x, 3))
			.Single()
			.Center)];

		drawing.Children
			.Select(static x => x.GetBounds())
			.Should()
			.SatisfyRespectively(
				x => x.Contains(centers[0]).Should().BeTrue(),
				x => x.Contains(centers[1]).Should().BeTrue());
	}

	/// <summary>
	/// <see cref="SelectionOccurrenceRenderer.FindOccurrences" />: only the visible lines are looked through.
	/// </summary>
	[AvaloniaTest]
	public void FindOccurrences_Looks_At_The_Visible_Lines_Only()
	{
		// Arrange
		TextEditor editor = new()
		{
			Document = new(string.Join('\n', Enumerable.Repeat("log", 1000)))
		};

		Show(editor);

		editor.Select(0, 3);

		Dispatcher.UIThread.RunJobs();

		TextView textView = editor.TextArea.TextView;

		SelectionOccurrenceRenderer sut = new(editor.TextArea);

		// Act
		int[] offsets = [.. sut
			.FindOccurrences(textView)
			.Select(static x => x.Offset)];

		// Assert
		offsets
			.Should()
			.Equal(textView.VisualLines
				.Skip(1)
				.Select(static x => x.FirstDocumentLine.Offset));
	}

	/// <summary>
	/// <see cref="SelectionOccurrenceRenderer.FindOccurrences" />: a piece other than a word matches inside words too,
	/// in any case.
	/// </summary>
	[AvaloniaTest]
	[TestCase("log Log logger dialog LOG", 1, 2, new[] { 5, 9, 19, 23 })]
	[TestCase("a b A B ab", 0, 3, new[] { 4 })]
	public void FindOccurrences_Matches_Inside_Words_For_Another_Piece(
		string text,
		int start,
		int length,
		int[] expected)
	{
		// Arrange
		TextEditor editor = new()
		{
			Document = new(text)
		};

		Show(editor);

		editor.Select(start, length);

		Dispatcher.UIThread.RunJobs();

		SelectionOccurrenceRenderer sut = new(editor.TextArea);

		// Act
		int[] offsets = [.. sut
			.FindOccurrences(editor.TextArea.TextView)
			.Select(static x => x.Offset)];

		// Assert
		offsets
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="SelectionOccurrenceRenderer.FindOccurrences" />: a selected word matches whole words only, in any case.
	/// </summary>
	[AvaloniaTest]
	[TestCase("log Log logger dialog LOG", 0, 3, new[] { 4, 22 })]
	[TestCase("file_name file_name2 file_name", 0, 9, new[] { 21 })]
	[TestCase("мир Мир мирный МИР", 0, 3, new[] { 4, 15 })]
	public void FindOccurrences_Matches_Whole_Words_For_A_Selected_Word(
		string text,
		int start,
		int length,
		int[] expected)
	{
		// Arrange
		TextEditor editor = new()
		{
			Document = new(text)
		};

		Show(editor);

		editor.Select(start, length);

		Dispatcher.UIThread.RunJobs();

		SelectionOccurrenceRenderer sut = new(editor.TextArea);

		// Act
		int[] offsets = [.. sut
			.FindOccurrences(editor.TextArea.TextView)
			.Select(static x => x.Offset)];

		// Assert
		offsets
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="SelectionOccurrenceRenderer.FindOccurrences" />: only a selected piece of one line that is not
	/// whitespace is looked for.
	/// </summary>
	[AvaloniaTest]
	[TestCase(0, 0)]
	[TestCase(3, 2)]
	[TestCase(5, 7)]
	public void FindOccurrences_Needs_A_Piece_Of_One_Line(int start, int length)
	{
		// Arrange
		TextEditor editor = new()
		{
			Document = new("log  log\nlog  log\nlog  log")
		};

		Show(editor);

		editor.Select(start, length);

		Dispatcher.UIThread.RunJobs();

		SelectionOccurrenceRenderer sut = new(editor.TextArea);

		// Act
		SimpleSegment[] occurrences = [.. sut.FindOccurrences(editor.TextArea.TextView)];

		// Assert
		occurrences
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="SelectionOccurrenceRenderer.FindOccurrences" />: a rectangular selection is not looked for.
	/// </summary>
	[AvaloniaTest]
	public void FindOccurrences_Skips_A_Rectangular_Selection()
	{
		// Arrange
		TextEditor editor = new()
		{
			Document = new("log log")
		};

		Show(editor);

		editor.TextArea.Selection = new RectangleSelection(
			editor.TextArea,
			new TextViewPosition(1, 1),
			new TextViewPosition(1, 4));

		Dispatcher.UIThread.RunJobs();

		SelectionOccurrenceRenderer sut = new(editor.TextArea);

		// Act
		SimpleSegment[] occurrences = [.. sut.FindOccurrences(editor.TextArea.TextView)];

		// Assert
		occurrences
			.Should()
			.BeEmpty();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Shows the editor in its theme in a window of a fixed size and lets the layout settle.
	/// </summary>
	private static void Show(TextEditor editor)
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
	}
	#endregion
}
