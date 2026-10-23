using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Styling;
using DataOrganizer.Controls.Text;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using System.Linq;

namespace DataOrganizer.UnitTests.Controls.Text;

[TestFixture(Description = $@"Tests of ""{nameof(TextEditorBase)}"" type")]
internal class TextEditorBaseTests
{
	#region Data
	/// <summary>
	/// Name of the scroll viewer in the template of the text editor.
	/// </summary>
	private const string ScrollViewerName = "PART_ScrollViewer";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="TextArea.Caret" />: a click in the gap between the line numbers and the text puts the caret
	/// at the start of the line.
	/// </summary>
	[AvaloniaTest]
	public void Caret_Goes_To_The_Line_Start_On_A_Click_Before_The_Text()
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 3)
		};

		Window window = Show(sut);

		TextView textView = sut.TextArea.TextView;

		// In the middle of the gap, level with the second line.
		Point point = textView.TranslatePoint(
			new(
				-textView.Margin.Left / 2.0,
				textView.DefaultLineHeight * 1.5),
			window) ?? default;

		// Act
		window.Click(point);

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.TextArea.Caret.Offset
			.Should()
			.Be(sut.Document.GetLineByNumber(2).Offset);
	}

	/// <summary>
	/// <see cref="TextEditorBase.CopyCommand" />: there is something to copy only when text is selected.
	/// </summary>
	[AvaloniaTest]
	public void CopyCommand_Needs_A_Selection([Values] bool isSelected)
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = new("Some text")
		};

		if (isSelected)
		{
			sut.Select(0, 4);
		}

		// Act
		bool canExecute = sut.CopyCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(isSelected);
	}

	/// <summary>
	/// <see cref="TextEditorBase" />: a double click of the right button on spaces leaves them unselected, as the engine
	/// does.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_Of_The_Right_Button_On_Spaces_Leaves_Them_Unselected()
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = new("\"word1\"    \"word2\"")
		};

		Window window = Show(sut);

		Point point = sut.TextArea.TextView.GetColumnCenter(8);

		// Act
		for (int i = 0; i < 2; i++)
		{
			window.Click(point, MouseButton.Right);
		}

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.SelectionLength
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="TextEditorBase" />: a double click on a word selects the word.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_On_A_Word_Selects_The_Word()
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = new("\"word1\"    \"word2\"")
		};

		Window window = Show(sut);

		Point point = sut.TextArea.TextView.GetColumnCenter(3);

		// Act
		for (int i = 0; i < 2; i++)
		{
			window.Click(point);
		}

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.SelectedText
			.Should()
			.Be("word1");
	}

	/// <summary>
	/// <see cref="TextEditorBase" />: a double click on a space or a tab selects the whole run of them, which ends at
	/// the line break.
	/// </summary>
	[AvaloniaTest]
	[TestCase("\"word1\"    \"word2\"", 7, "    ")]
	[TestCase("\"word1\"    \"word2\"", 10, "    ")]
	[TestCase("a\t\tb", 2, "\t\t")]
	[TestCase("    indent", 1, "    ")]
	[TestCase("a  \n  b", 2, "  ")]
	public void DoubleClick_On_Spaces_Selects_The_Whole_Run(string text, int offset, string expected)
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = new(text)
		};

		Window window = Show(sut);

		Point point = sut.TextArea.TextView.GetColumnCenter(offset);

		// Act
		for (int i = 0; i < 2; i++)
		{
			window.Click(point);
		}

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.SelectedText
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="TextEditorBase" />: a double click with Shift on spaces leaves to the engine the selection that goes on
	/// from the caret.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_With_Shift_On_Spaces_Extends_The_Selection_From_The_Caret()
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = new("\"word1\"    \"word2\"")
		};

		Window window = Show(sut);

		Point point = sut.TextArea.TextView.GetColumnCenter(8);

		// Act
		for (int i = 0; i < 2; i++)
		{
			window.MouseDown(point, MouseButton.Left, RawInputModifiers.Shift);

			window.MouseUp(point, MouseButton.Left, RawInputModifiers.Shift);
		}

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.SelectionStart
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="TextEditorBase.FindCommand" />: every button of the search panel, also one that shows a glyph alone, has
	/// a name for UI Automation instead of the name of its content type.
	/// </summary>
	[AvaloniaTest]
	public void FindCommand_Opens_A_Search_Panel_With_A_Name_On_Every_Button()
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 10)
		};

		Show(sut);

		// Act
		sut.FindCommand.Execute(null);

		// The panel builds its buttons in the layout pass.
		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.SearchPanel
			.GetVisualDescendants()
			.OfType<Button>()
			.Select(static x => ControlAutomationPeer
				.CreatePeerForElement(x)
				.GetName())
			.Should()
			.NotBeEmpty()
			.And
			.AllSatisfy(static x => x
				.Should()
				.NotBeNullOrEmpty()
				.And
				.NotStartWith("Avalonia."));
	}

	/// <summary>
	/// <see cref="TextEditorBase.FindCommand" />: opens the search panel.
	/// </summary>
	[AvaloniaTest]
	public void FindCommand_Opens_The_Search_Panel()
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 10)
		};

		Show(sut);

		// Act
		sut.FindCommand.Execute(null);

		// Assert
		sut.SearchPanel.IsOpened
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="TemplatedControl.FontSize" />: a notch of the wheel changes the size by one step only with Ctrl.
	/// </summary>
	[AvaloniaTest]
	public void FontSize_Changes_On_A_Wheel_Notch_Only_With_Ctrl([Values] bool isCtrlPressed)
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 100),
			FontSize = 14.0
		};

		Window window = Show(sut);

		RawInputModifiers modifiers = isCtrlPressed
			? RawInputModifiers.Control
			: RawInputModifiers.None;

		// Act
		window.MouseWheel(window.CenterOf(sut), new(0.0, 1.0), modifiers);

		// Assert
		sut.FontSize
			.Should()
			.Be(isCtrlPressed ? 14.5 : 14.0);
	}

	/// <summary>
	/// <see cref="TemplatedControl.FontSize" />: the gap between the line numbers and the text grows with the font size.
	/// </summary>
	[AvaloniaTest]
	[TestCase(10.0, 4.0)]
	[TestCase(40.0, 16.0)]
	public void FontSize_Sets_The_Gap_Before_The_Text(double fontSize, double expected)
	{
		// Arrange, Act
		TestTextEditor sut = new()
		{
			FontSize = fontSize
		};

		// Assert
		sut.TextArea.TextView.Margin.Left
			.Should()
			.BeApproximately(expected, 1e-9);
	}

	/// <summary>
	/// <see cref="LineNumberMargin" />: the pointer stays an arrow over the line numbers after a click in the text,
	/// which gives the text area the I-beam.
	/// </summary>
	[AvaloniaTest]
	public void LineNumberMargin_Keeps_The_Arrow_Cursor_After_A_Click_In_The_Text()
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 3)
		};

		Window window = Show(sut);

		TextView textView = sut.TextArea.TextView;

		// In the text of the second line.
		Point point = textView.TranslatePoint(
			new(
				textView.WideSpaceWidth * 3.0,
				textView.DefaultLineHeight * 1.5),
			window) ?? default;

		// Act
		window.Click(point);

		// Assert
		LineNumberMargin lineNumbers = sut
			.TextArea
			.LeftMargins
			.OfType<LineNumberMargin>()
			.Single();

		// A local keeps the assertion from being skipped by the null-conditional operator when there is no cursor.
		string? cursor = lineNumbers
			.Cursor?
			.ToString();

		cursor
			.Should()
			.Be(nameof(StandardCursorType.Arrow));
	}

	/// <summary>
	/// <see cref="TextEditorBase.OccurrenceSource" />: the occurrences follow the new source, and the selection of the
	/// former one no longer reaches them.
	/// </summary>
	[AvaloniaTest]
	public void OccurrenceSource_Leaves_The_Former_Source_Behind()
	{
		// Arrange
		TextDocument document = new("log Log end End");

		TestTextEditor formerSource = new()
		{
			Document = document
		};

		TestTextEditor source = new()
		{
			Document = document
		};

		TestTextEditor sut = new()
		{
			Document = document,
			OccurrenceSource = formerSource.TextArea
		};

		Show(sut);

		// Act
		sut.OccurrenceSource = source.TextArea;

		source.Select(0, 3);

		formerSource.Select(8, 3);

		Dispatcher.UIThread.RunJobs();

		// Assert
		TextView textView = sut.TextArea.TextView;

		textView.BackgroundRenderers
			.OfType<SelectionOccurrenceRenderer>()
			.Single()
			.FindOccurrences(textView)
			.Select(static x => x.Offset)
			.Should()
			.Equal(0, 4);
	}

	/// <summary>
	/// <see cref="TextEditorBase.OccurrenceSource" />: a selection in the source repaints the background of the editor,
	/// where the occurrences are.
	/// </summary>
	[AvaloniaTest]
	public void OccurrenceSource_Repaints_The_Occurrences_On_A_Selection()
	{
		// Arrange
		TextDocument document = new("log Log");

		TestTextEditor source = new()
		{
			Document = document
		};

		IBackgroundRenderer background = Substitute.For<IBackgroundRenderer>();

		background
			.Layer
			.Returns(KnownLayer.Background);

		TestTextEditor sut = new()
		{
			Document = document,
			OccurrenceSource = source.TextArea
		};

		Show(sut);

		TextView textView = sut.TextArea.TextView;

		textView
			.BackgroundRenderers
			.Add(background);

		// The background is painted once with the new renderer before the selection.
		PaintFrame();

		background.ClearReceivedCalls();

		// Act
		source.Select(0, 3);

		PaintFrame();

		// Assert
		// The number of paints depends on the frames.
		background
			.Received()
			.Draw(textView, Arg.Any<DrawingContext>());
	}

	/// <summary>
	/// <see cref="ScrollMarkMargin" />: belongs to the logical tree of the editor, which gives the tips of the marks
	/// their styles.
	/// </summary>
	[AvaloniaTest]
	public void ScrollMarkMargin_Belongs_To_The_Logical_Tree_Of_The_Editor()
	{
		// Arrange, Act
		TestTextEditor sut = new();

		// Assert
		sut
			.GetLogicalChildren()
			.OfType<ScrollMarkMargin>()
			.Should()
			.ContainSingle();
	}

	/// <summary>
	/// <see cref="ScrollMarkMargin" />: takes a column of its own at the right edge, next to the scroll viewer.
	/// </summary>
	[AvaloniaTest]
	public void ScrollMarkMargin_Takes_A_Column_At_The_Right_Edge()
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 100)
		};

		// Act
		Show(sut);

		// Assert
		ScrollMarkMargin margin = sut
			.GetVisualChildren()
			.OfType<ScrollMarkMargin>()
			.Single();

		margin.Bounds.Width
			.Should()
			.BePositive();

		margin.Bounds.Right
			.Should()
			.Be(sut.Bounds.Width);

		ScrollViewer scrollViewer = GetScrollViewer(sut);

		double? right = scrollViewer.TranslatePoint(new(scrollViewer.Bounds.Width, 0.0), sut)?.X;

		right
			.Should()
			.Be(margin.Bounds.Left);
	}

	/// <summary>
	/// <see cref="TextEditorBase.ScrollToEndCommand" />: scrolls to the end and moves the caret to the last line.
	/// </summary>
	[AvaloniaTest]
	public void ScrollToEndCommand_Moves_The_Caret_To_The_Last_Line()
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 1000)
		};

		Show(sut);

		// Act
		sut.ScrollToEndCommand.Execute(null);

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.TextArea.Caret.Line
			.Should()
			.Be(sut.LineCount);

		ScrollViewer scrollViewer = GetScrollViewer(sut);

		scrollViewer.Offset.Y
			.Should()
			.Be(scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
	}

	/// <summary>
	/// <see cref="TextEditorBase.ScrollToTopCommand" />: scrolls to the top and moves the caret to the first line.
	/// </summary>
	[AvaloniaTest]
	public void ScrollToTopCommand_Moves_The_Caret_To_The_First_Line()
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 1000)
		};

		Show(sut);

		sut.ScrollToEndCommand.Execute(null);

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.ScrollToTopCommand.Execute(null);

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.TextArea.Caret.Line
			.Should()
			.Be(1);

		GetScrollViewer(sut).Offset.Y
			.Should()
			.Be(0.0);
	}

	/// <summary>
	/// <see cref="TextEditorBase.ScrollViewer" />: is the scroll viewer of the template.
	/// </summary>
	[AvaloniaTest]
	public void ScrollViewer_Comes_From_The_Template()
	{
		// Arrange
		TestTextEditor sut = new();

		// Act
		Show(sut);

		// Assert
		// A local keeps the assertion from being skipped by the null-conditional operator when there is none.
		string? name = sut.ScrollViewer?.Name;

		name
			.Should()
			.Be(ScrollViewerName);
	}

	/// <summary>
	/// <see cref="TextEditorBase.SelectAllCommand" />: there is something to select only when the document has text.
	/// </summary>
	[AvaloniaTest]
	public void SelectAllCommand_Needs_Text([Values] bool hasText)
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = new(hasText ? "Some text" : string.Empty)
		};

		// Act
		bool canExecute = sut.SelectAllCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(hasText);
	}

	/// <summary>
	/// <see cref="TextEditorBase.SelectAllCommand" />: selects the text from its start to its end.
	/// </summary>
	[AvaloniaTest]
	public void SelectAllCommand_Selects_The_Whole_Text()
	{
		// Arrange
		TextDocument document = CreateDocument(lineCount: 10);

		TestTextEditor sut = new()
		{
			Document = document
		};

		// Act
		sut.SelectAllCommand.Execute(null);

		// Assert
		sut.SelectionStart
			.Should()
			.Be(0);

		sut.SelectionLength
			.Should()
			.Be(document.TextLength);
	}

	/// <summary>
	/// <see cref="TextArea.Selection" />: the occurrences of the selected text are painted.
	/// </summary>
	[AvaloniaTest]
	public void Selection_Highlights_Its_Occurrences()
	{
		// Arrange
		TestTextEditor sut = new()
		{
			Document = new("log log")
		};

		Show(sut);

		// Act
		sut.Select(0, 3);

		Dispatcher.UIThread.RunJobs();

		// Assert
		TextView textView = sut.TextArea.TextView;

		textView.BackgroundRenderers
			.OfType<SelectionOccurrenceRenderer>()
			.Single()
			.FindOccurrences(textView)
			.Select(static x => x.Offset)
			.Should()
			.Equal(4);
	}

	/// <summary>
	/// <see cref="TextEditorBase.SpinCommand" />: the font size changes by a step and stays between the limits.
	/// </summary>
	[AvaloniaTest]
	[TestCase(14.0, SpinDirection.Increase, 14.5)]
	[TestCase(14.0, SpinDirection.Decrease, 13.5)]
	[TestCase(64.0, SpinDirection.Increase, 64.0)]
	[TestCase(6.0, SpinDirection.Decrease, 6.0)]
	public void SpinCommand_Changes_The_Font_Size_Within_Limits(
		double fontSize,
		SpinDirection direction,
		double expected)
	{
		// Arrange
		TestTextEditor sut = new()
		{
			FontSize = fontSize
		};

		// Act
		sut.SpinCommand.Execute(new(Spinner.SpinEvent, direction));

		// Assert
		sut.FontSize
			.Should()
			.Be(expected);
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
	/// Lets the posted work run and paints a frame of the dirty controls.
	/// </summary>
	private static void PaintFrame()
	{
		Dispatcher.UIThread.RunJobs();

		AvaloniaHeadlessPlatform.ForceRenderTimerTick();

		Dispatcher.UIThread.RunJobs();
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
