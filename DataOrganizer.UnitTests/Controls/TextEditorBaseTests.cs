using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Styling;
using DataOrganizer.Controls;
using System.Linq;

namespace DataOrganizer.UnitTests.Controls;

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
		window.MouseWheel(Center(window, sut), new(0.0, 1.0), modifiers);

		// Assert
		sut.FontSize
			.Should()
			.Be(isCtrlPressed ? 14.5 : 14.0);
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
