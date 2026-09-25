using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Dto;
using DataOrganizer.Views;
using System.Linq;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentEditor)}"" type")]
internal class DocumentEditorTests
{
	#region Data
	/// <summary>
	/// Name of the text editor in the markup.
	/// </summary>
	private const string EditorName = "Editor";

	/// <summary>
	/// Name of the caption of the encoding in the markup.
	/// </summary>
	private const string EncodingCaptionName = "EncodingCaption";

	/// <summary>
	/// Name of the scroll viewer in the template of the text editor.
	/// </summary>
	private const string ScrollViewerName = "PART_ScrollViewer";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DocumentEditor.CaptureViewState" />: the reported state is not restored back,
	/// so a later scroll stays where it is.
	/// </summary>
	[AvaloniaTest]
	public void CaptureViewState_Does_Not_Scroll_The_View_Back()
	{
		// Arrange
		DocumentEditor sut = new()
		{
			Document = CreateDocument(lineCount: 1000)
		};

		Show(sut);

		ScrollViewer scrollViewer = GetScrollViewer(sut);

		scrollViewer.Offset = new(0.0, 500.0);

		Dispatcher.UIThread.RunJobs();

		Vector offset = new(0.0, 800.0);

		// Act
		sut.CaptureViewState();

		scrollViewer.Offset = offset;

		Dispatcher.UIThread.RunJobs();

		// Assert
		scrollViewer.Offset
			.Should()
			.Be(offset);
	}

	/// <summary>
	/// <see cref="DocumentEditor.CaptureViewState" />: a view state that is still to be restored is not overwritten.
	/// </summary>
	[AvaloniaTest]
	public void CaptureViewState_Keeps_A_Pending_View_State()
	{
		// Arrange
		DocumentEditor sut = new()
		{
			Document = CreateDocument(lineCount: 100)
		};

		Show(sut);

		DocumentViewState state = new()
		{
			CaretPosition = new(line: 3, column: 1),
			ScrollOffset = new(0.0, 100.0),
			SelectionLength = 4,
			SelectionStart = 20
		};

		sut.ViewState = state;

		// Act
		sut.CaptureViewState();

		// Assert
		sut.ViewState
			.Should()
			.Be(state);
	}

	/// <summary>
	/// <see cref="DocumentEditor.CaptureViewState" />: reports the caret, the selection and the offset the editor shows.
	/// </summary>
	[AvaloniaTest]
	public void CaptureViewState_Reports_The_Caret_The_Selection_And_The_Offset()
	{
		// Arrange
		DocumentEditor sut = new()
		{
			Document = CreateDocument(lineCount: 1000)
		};

		Show(sut);

		TextEditor editor = sut.GetControl<TextEditor>(EditorName);

		editor.Select(20, 4);

		Vector offset = new(0.0, 500.0);

		GetScrollViewer(sut).Offset = offset;

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.CaptureViewState();

		// Assert
		sut.ViewState
			.Should()
			.Be(new DocumentViewState
			{
				CaretPosition = editor.TextArea.Caret.Position,
				ScrollOffset = offset,
				SelectionLength = 4,
				SelectionStart = 20
			});
	}

	/// <summary>
	/// <see cref="DocumentEditor.DocumentFontSize" />: a notch of the wheel with Ctrl changes the size by one step.
	/// </summary>
	[AvaloniaTest]
	public void DocumentFontSize_Changes_By_One_Step_Per_Ctrl_Wheel_Notch()
	{
		// Arrange
		DocumentEditor sut = new()
		{
			Document = CreateDocument(lineCount: 100),
			DocumentFontSize = 14.0
		};

		Window window = Show(sut);

		TextEditor editor = sut.GetControl<TextEditor>(EditorName);

		// Act
		window.MouseWheel(Center(window, editor), new(0.0, 1.0), RawInputModifiers.Control);

		// Assert
		sut.DocumentFontSize
			.Should()
			.Be(14.5);
	}

	/// <summary>
	/// <see cref="DocumentEditor.DocumentFontSize" />: a spin of the font size spinner changes the size by one step.
	/// </summary>
	[AvaloniaTest]
	public void DocumentFontSize_Changes_By_One_Step_Per_Spin()
	{
		// Arrange
		DocumentEditor sut = new()
		{
			DocumentFontSize = 14.0
		};

		Show(sut);

		ButtonSpinner spinner = sut
			.GetVisualDescendants()
			.OfType<ButtonSpinner>()
			.Single();

		// Act
		spinner.RaiseEvent(new SpinEventArgs(Spinner.SpinEvent, SpinDirection.Increase));

		// Assert
		sut.DocumentFontSize
			.Should()
			.Be(14.5);
	}

	/// <summary>
	/// <see cref="DocumentEditor.EncodingName" />: the name of the encoding reaches the status bar.
	/// </summary>
	[AvaloniaTest]
	public void EncodingName_Reaches_The_Status_Bar()
	{
		// Arrange
		DocumentEditor sut = new()
		{
			EncodingName = "UTF-8-BOM"
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextBlock>(EncodingCaptionName).Text
			.Should()
			.Be("UTF-8-BOM");
	}

	/// <summary>
	/// <see cref="DocumentEditor.IsReadOnly" />: the read-only mode reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void IsReadOnly_Reaches_The_Editor([Values] bool isReadOnly)
	{
		// Arrange
		DocumentEditor sut = new()
		{
			IsReadOnly = isReadOnly
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextEditor>(EditorName).IsReadOnly
			.Should()
			.Be(isReadOnly);
	}

	/// <summary>
	/// <see cref="DocumentEditor" />: detaching the control and attaching it again, as a tab switch does,
	/// keeps the selection and the scroll position.
	/// </summary>
	[AvaloniaTest]
	public void Keeps_The_View_When_Attached_Again()
	{
		// Arrange
		DocumentEditor sut = new()
		{
			Document = CreateDocument(lineCount: 1000)
		};

		Window window = Show(sut);

		TextEditor editor = sut.GetControl<TextEditor>(EditorName);

		editor.Select(20, 4);

		Vector offset = new(0.0, 500.0);

		GetScrollViewer(sut).Offset = offset;

		Dispatcher.UIThread.RunJobs();

		// Act
		window.Content = null;

		Dispatcher.UIThread.RunJobs();

		window.Content = sut;

		Dispatcher.UIThread.RunJobs();

		// Assert
		editor.SelectionStart
			.Should()
			.Be(20);

		editor.SelectionLength
			.Should()
			.Be(4);

		GetScrollViewer(sut).Offset
			.Should()
			.Be(offset);
	}

	/// <summary>
	/// <see cref="DocumentEditor.ShowEndOfLine" />: the glyphs of line endings reach the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowEndOfLine_Reaches_The_Editor([Values] bool isShown)
	{
		// Arrange
		DocumentEditor sut = new()
		{
			ShowEndOfLine = isShown
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextEditor>(EditorName).Options.ShowEndOfLine
			.Should()
			.Be(isShown);
	}

	/// <summary>
	/// <see cref="DocumentEditor.ShowSpaces" />: the glyphs of spaces reach the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowSpaces_Reaches_The_Editor([Values] bool isShown)
	{
		// Arrange
		DocumentEditor sut = new()
		{
			ShowSpaces = isShown
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextEditor>(EditorName).Options.ShowSpaces
			.Should()
			.Be(isShown);
	}

	/// <summary>
	/// <see cref="DocumentEditor.ShowTabs" />: the glyphs of tabs reach the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowTabs_Reaches_The_Editor([Values] bool isShown)
	{
		// Arrange
		DocumentEditor sut = new()
		{
			ShowTabs = isShown
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextEditor>(EditorName).Options.ShowTabs
			.Should()
			.Be(isShown);
	}

	/// <summary>
	/// <see cref="DocumentEditor" />: the status bar shows the encoding only when one is given.
	/// </summary>
	[AvaloniaTest]
	public void StatusBar_Shows_The_Encoding_Only_When_It_Is_Given([Values] bool isGiven)
	{
		// Arrange
		DocumentEditor sut = new()
		{
			EncodingName = isGiven ? "UTF-8" : null
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextBlock>(EncodingCaptionName).IsVisible
			.Should()
			.Be(isGiven);
	}

	/// <summary>
	/// <see cref="DocumentEditor.ToolBarContent" />: the content placed in the toolbar keeps the data context of the control.
	/// </summary>
	[AvaloniaTest]
	public void ToolBarContent_Keeps_The_Data_Context_Of_The_Control()
	{
		// Arrange
		object dataContext = new();

		Button button = new();

		DocumentEditor sut = new()
		{
			DataContext = dataContext,
			ToolBarContent = button
		};

		// Act
		Show(sut);

		// Assert
		button.DataContext
			.Should()
			.BeSameAs(dataContext);
	}

	/// <summary>
	/// <see cref="DocumentEditor.ViewState" />: a selection and a caret beyond the document are brought inside it.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Clamps_A_Selection_Beyond_The_Document()
	{
		// Arrange
		TextDocument document = CreateDocument(lineCount: 10);

		DocumentEditor sut = new()
		{
			Document = document
		};

		Show(sut);

		// Act
		sut.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 500, column: 40),
			ScrollOffset = new(0.0, 100000.0),
			SelectionLength = 50,
			SelectionStart = 80
		};

		Dispatcher.UIThread.RunJobs();

		// Assert
		TextEditor editor = sut.GetControl<TextEditor>(EditorName);

		editor.SelectionStart
			.Should()
			.Be(80);

		editor.SelectionLength
			.Should()
			.Be(document.TextLength - 80);

		editor.TextArea.Caret.Line
			.Should()
			.Be(document.LineCount);
	}

	/// <summary>
	/// <see cref="DocumentEditor.ViewState" />: a document set right after the state is the one it is restored on,
	/// not the one it would have been lost with.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Is_Restored_On_A_Document_Set_After_It()
	{
		// Arrange
		DocumentEditor sut = new()
		{
			Document = new()
		};

		Show(sut);

		Vector offset = new(0.0, 500.0);

		// Act
		sut.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 3, column: 1),
			ScrollOffset = offset,
			SelectionLength = 4,
			SelectionStart = 20
		};

		sut.Document = CreateDocument(lineCount: 1000);

		Dispatcher.UIThread.RunJobs();

		// Assert
		TextEditor editor = sut.GetControl<TextEditor>(EditorName);

		editor.SelectionStart
			.Should()
			.Be(20);

		editor.SelectionLength
			.Should()
			.Be(4);

		GetScrollViewer(sut).Offset
			.Should()
			.Be(offset);
	}

	/// <summary>
	/// <see cref="DocumentEditor.ViewState" />: the offset is restored, and the caret line does not scroll it away.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Restores_The_Offset_Instead_Of_The_Caret_Line()
	{
		// Arrange
		DocumentEditor sut = new()
		{
			Document = CreateDocument(lineCount: 1000)
		};

		Show(sut);

		Vector offset = new(0.0, 500.0);

		// Act
		sut.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 900, column: 1),
			ScrollOffset = offset,
			SelectionLength = 0,
			SelectionStart = 0
		};

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetScrollViewer(sut).Offset
			.Should()
			.Be(offset);
	}

	/// <summary>
	/// <see cref="DocumentEditor.ViewState" />: restores the selection and the caret.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Restores_The_Selection_And_The_Caret()
	{
		// Arrange
		DocumentEditor sut = new()
		{
			Document = CreateDocument(lineCount: 100)
		};

		Show(sut);

		// Act
		sut.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 3, column: 1),
			ScrollOffset = default,
			SelectionLength = 4,
			SelectionStart = 20
		};

		Dispatcher.UIThread.RunJobs();

		// Assert
		TextEditor editor = sut.GetControl<TextEditor>(EditorName);

		editor.SelectionStart
			.Should()
			.Be(20);

		editor.SelectionLength
			.Should()
			.Be(4);

		editor.TextArea.Caret.Location
			.Should()
			.Be(new TextLocation(3, 1));
	}

	/// <summary>
	/// <see cref="DocumentEditor.ViewState" />: a value set before the control is loaded is restored once it is.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Set_Before_Loading_Is_Restored_Once_Loaded()
	{
		// Arrange
		Vector offset = new(0.0, 500.0);

		DocumentEditor sut = new()
		{
			Document = CreateDocument(lineCount: 1000),
			ViewState = new DocumentViewState
			{
				CaretPosition = new(line: 3, column: 1),
				ScrollOffset = offset,
				SelectionLength = 4,
				SelectionStart = 20
			}
		};

		// Act
		Show(sut);

		// Assert
		GetScrollViewer(sut).Offset
			.Should()
			.Be(offset);
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
	/// Returns the scroll viewer of the text editor.
	/// </summary>
	private static ScrollViewer GetScrollViewer(DocumentEditor editor)
	{
		return editor
			.GetControl<TextEditor>(EditorName)
			.GetVisualDescendants()
			.OfType<ScrollViewer>()
			.First(static x => x.Name == ScrollViewerName);
	}

	/// <summary>
	/// Shows the editor in a window of a fixed size and lets the layout settle.
	/// </summary>
	private static Window Show(DocumentEditor editor)
	{
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
