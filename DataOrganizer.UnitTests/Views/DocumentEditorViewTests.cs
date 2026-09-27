using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Controls;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Messages.Documents;
using DataOrganizer.Views;
using System.Linq;
using System.Windows.Input;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentEditorView)}"" type")]
internal class DocumentEditorViewTests
{
	#region Data
	/// <summary>
	/// Name of the status bar block with the caret position in the markup.
	/// </summary>
	private const string CaretBlockName = "CaretBlock";

	/// <summary>
	/// Name of the text editor of the upper half in the markup.
	/// </summary>
	private const string EditorName = "Editor";

	/// <summary>
	/// Name of the grid with the halves in the markup.
	/// </summary>
	private const string EditorsHostName = "EditorsHost";

	/// <summary>
	/// Name of the caption of the encoding in the markup.
	/// </summary>
	private const string EncodingCaptionName = "EncodingCaption";

	/// <summary>
	/// Name of the scroll viewer in the template of the text editor.
	/// </summary>
	private const string ScrollViewerName = "PART_ScrollViewer";

	/// <summary>
	/// Name of the text editor of the lower half in the markup.
	/// </summary>
	private const string SplitEditorName = "SplitEditor";

	/// <summary>
	/// Name of the panel with the buttons of the control in the toolbar in the markup.
	/// </summary>
	private const string ToolBarName = "ToolBar";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DocumentEditorView.ActiveEditor" />: a right click beside the text, where the text area takes no focus,
	/// makes its half the active one.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Follows_A_Right_Click_Beside_The_Text()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true
		};

		Window window = Show(sut);

		DocumentTextEditor splitEditor = sut.GetControl<DocumentTextEditor>(SplitEditorName);

		ScrollMarkMargin scrollMarks = splitEditor
			.GetVisualDescendants()
			.OfType<ScrollMarkMargin>()
			.Single();

		Point point = Center(window, scrollMarks);

		// Act
		window.MouseDown(point, MouseButton.Right);

		window.MouseUp(point, MouseButton.Right);

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.ActiveEditor.Name
			.Should()
			.Be(SplitEditorName);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ActiveEditor" />: a right click in the text makes its half the active one.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Follows_A_Right_Click_In_The_Text()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true
		};

		Window window = Show(sut);

		DocumentTextEditor splitEditor = sut.GetControl<DocumentTextEditor>(SplitEditorName);

		Point point = Center(window, splitEditor);

		// Act
		window.MouseDown(point, MouseButton.Right);

		window.MouseUp(point, MouseButton.Right);

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.ActiveEditor.Name
			.Should()
			.Be(SplitEditorName);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ActiveEditor" />: the focus that comes back to the upper half makes it the active one again.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Follows_The_Focus_Back_Into_The_Upper_Half()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true
		};

		Show(sut);

		DocumentTextEditor editor = sut.GetControl<DocumentTextEditor>(EditorName);

		sut
			.GetControl<DocumentTextEditor>(SplitEditorName)
			.TextArea
			.Focus();

		// Act
		editor
			.TextArea
			.Focus();

		// Assert
		sut.ActiveEditor.Name
			.Should()
			.Be(EditorName);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ActiveEditor" />: the focus that goes into the lower half makes it the active one.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Follows_The_Focus_Into_The_Lower_Half()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true
		};

		Show(sut);

		DocumentTextEditor splitEditor = sut.GetControl<DocumentTextEditor>(SplitEditorName);

		// Act
		splitEditor
			.TextArea
			.Focus();

		// Assert
		sut.ActiveEditor.Name
			.Should()
			.Be(SplitEditorName);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ActiveEditor" />: every command of the context menu acts on the active half.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Reaches_The_Context_Menu()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true
		};

		Show(sut);

		DocumentTextEditor splitEditor = sut.GetControl<DocumentTextEditor>(SplitEditorName);

		// Act
		splitEditor
			.TextArea
			.Focus();

		// Assert
		ICommand?[] commands = GetMenuCommands(sut.GetControl<DocumentTextEditor>(EditorName).ContextMenu!);

		commands
			.Should()
			.NotBeEmpty()
			.And
			.BeSubsetOf(GetCommands(splitEditor));
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ActiveEditor" />: the status bar shows the caret of the active half.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Reaches_The_Status_Bar()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 100),
			IsSplit = true
		};

		Show(sut);

		DocumentTextEditor editor = sut.GetControl<DocumentTextEditor>(EditorName);

		DocumentTextEditor splitEditor = sut.GetControl<DocumentTextEditor>(SplitEditorName);

		StackPanel caretBlock = sut.GetControl<StackPanel>(CaretBlockName);

		editor
			.TextArea
			.Caret
			.Position = new(line: 3, column: 1);

		splitEditor
			.TextArea
			.Caret
			.Position = new(line: 7, column: 5);

		string?[] texts = [.. caretBlock.Children.OfType<TextBlock>().Select(static x => x.Text)];

		// Act
		splitEditor
			.TextArea
			.Focus();

		// Assert
		caretBlock.Children.OfType<TextBlock>().Select(static x => x.Text)
			.Should()
			.NotEqual(texts);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ActiveEditor" />: the scroll buttons of the toolbar act on the active half.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Reaches_The_Toolbar()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true
		};

		Show(sut);

		DocumentTextEditor splitEditor = sut.GetControl<DocumentTextEditor>(SplitEditorName);

		// Act
		splitEditor
			.TextArea
			.Focus();

		// Assert
		ICommand[] commands = [.. sut
			.GetControl<StackPanel>(ToolBarName)
			.GetVisualDescendants()
			.OfType<Button>()
			.Select(static x => x.Command)
			.OfType<ICommand>()];

		commands
			.Should()
			.Equal(splitEditor.ScrollToTopCommand, splitEditor.ScrollToEndCommand);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ActiveEditor" />: the scroll buttons of the toolbar act on the upper half
	/// before any half takes the focus.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Reaches_The_Toolbar_From_The_Start()
	{
		// Arrange
		DocumentEditorView sut = new();

		// Act
		Show(sut);

		// Assert
		DocumentTextEditor editor = sut.GetControl<DocumentTextEditor>(EditorName);

		ICommand[] commands = [.. sut
			.GetControl<StackPanel>(ToolBarName)
			.GetVisualDescendants()
			.OfType<Button>()
			.Select(static x => x.Command)
			.OfType<ICommand>()];

		commands
			.Should()
			.Equal(editor.ScrollToTopCommand, editor.ScrollToEndCommand);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ActiveEditor" />: a new active half reports its caret, selection and offset at once.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Reports_The_View_State_Of_The_New_Half()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 1000),
			IsSplit = true
		};

		Show(sut);

		DocumentTextEditor splitEditor = sut.GetControl<DocumentTextEditor>(SplitEditorName);

		splitEditor.Select(40, 3);

		Vector offset = new(0.0, 700.0);

		GetSplitScrollViewer(sut).Offset = offset;

		Dispatcher.UIThread.RunJobs();

		// Act
		splitEditor
			.TextArea
			.Focus();

		// Assert
		sut.ViewState
			.Should()
			.Be(new DocumentViewState
			{
				CaretPosition = splitEditor.TextArea.Caret.Position,
				ScrollOffset = offset,
				SelectionLength = 3,
				SelectionStart = 40
			});
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ActiveEditor" />: the upper half is the active one from the start.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Starts_With_The_Upper_Half()
	{
		// Act
		DocumentEditorView sut = new();

		// Assert
		sut.ActiveEditor.Name
			.Should()
			.Be(EditorName);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.CaptureViewState" />: the reported state is not restored back,
	/// so a later scroll stays where it is.
	/// </summary>
	[AvaloniaTest]
	public void CaptureViewState_Does_Not_Scroll_The_View_Back()
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.CaptureViewState" />: a view state that is still to be restored is not overwritten.
	/// </summary>
	[AvaloniaTest]
	public void CaptureViewState_Keeps_A_Pending_View_State()
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.CaptureViewState" />: reports the caret, the selection and the offset of the active half.
	/// </summary>
	[AvaloniaTest]
	public void CaptureViewState_Reports_The_Active_Half()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 1000),
			IsSplit = true
		};

		Show(sut);

		DocumentTextEditor splitEditor = sut.GetControl<DocumentTextEditor>(SplitEditorName);

		splitEditor
			.TextArea
			.Focus();

		splitEditor.Select(40, 3);

		Vector offset = new(0.0, 700.0);

		GetSplitScrollViewer(sut).Offset = offset;

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.CaptureViewState();

		// Assert
		sut.ViewState
			.Should()
			.Be(new DocumentViewState
			{
				CaretPosition = splitEditor.TextArea.Caret.Position,
				ScrollOffset = offset,
				SelectionLength = 3,
				SelectionStart = 40
			});
	}

	/// <summary>
	/// <see cref="DocumentEditorView.CaptureViewState" />: reports the caret, the selection and the offset the editor shows.
	/// </summary>
	[AvaloniaTest]
	public void CaptureViewState_Reports_The_Caret_The_Selection_And_The_Offset()
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.Document" />: the lower half shows the same document.
	/// </summary>
	[AvaloniaTest]
	public void Document_Reaches_The_Lower_Half()
	{
		// Arrange
		TextDocument document = CreateDocument(lineCount: 10);

		DocumentEditorView sut = new()
		{
			Document = document,
			IsSplit = true
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextEditor>(SplitEditorName).Document
			.Should()
			.BeSameAs(document);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.DocumentFontSize" />: a notch of the wheel with Ctrl changes the size by one step.
	/// </summary>
	[AvaloniaTest]
	public void DocumentFontSize_Changes_By_One_Step_Per_Ctrl_Wheel_Notch()
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.DocumentFontSize" />: a spin of the font size spinner changes the size by one step.
	/// </summary>
	[AvaloniaTest]
	public void DocumentFontSize_Changes_By_One_Step_Per_Spin()
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.DocumentFontSize" />: a notch of the wheel with Ctrl in the lower half zooms both halves.
	/// </summary>
	[AvaloniaTest]
	public void DocumentFontSize_Follows_A_Ctrl_Wheel_Notch_In_The_Lower_Half()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 100),
			DocumentFontSize = 14.0,
			IsSplit = true
		};

		Window window = Show(sut);

		TextEditor editor = sut.GetControl<TextEditor>(EditorName);

		TextEditor splitEditor = sut.GetControl<TextEditor>(SplitEditorName);

		// Act
		window.MouseWheel(Center(window, splitEditor), new(0.0, 1.0), RawInputModifiers.Control);

		// Assert
		sut.DocumentFontSize
			.Should()
			.Be(14.5);

		editor.FontSize
			.Should()
			.Be(14.5);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.EncodingName" />: the name of the encoding reaches the status bar.
	/// </summary>
	[AvaloniaTest]
	public void EncodingName_Reaches_The_Status_Bar()
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.IsReadOnly" />: the read-only mode reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void IsReadOnly_Reaches_The_Editor([Values] bool isReadOnly)
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.IsReadOnly" />: the read-only mode reaches the lower half.
	/// </summary>
	[AvaloniaTest]
	public void IsReadOnly_Reaches_The_Lower_Half([Values] bool isReadOnly)
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsReadOnly = isReadOnly,
			IsSplit = true
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextEditor>(SplitEditorName).IsReadOnly
			.Should()
			.Be(isReadOnly);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.IsSensitive" />: the sensitivity of the text reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void IsSensitive_Reaches_The_Editor([Values] bool isSensitive)
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSensitive = isSensitive
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<DocumentTextEditor>(EditorName).IsSensitive
			.Should()
			.Be(isSensitive);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.IsSensitive" />: the sensitivity of the text reaches the lower half.
	/// </summary>
	[AvaloniaTest]
	public void IsSensitive_Reaches_The_Lower_Half([Values] bool isSensitive)
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSensitive = isSensitive,
			IsSplit = true
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<DocumentTextEditor>(SplitEditorName).IsSensitive
			.Should()
			.Be(isSensitive);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.IsSplit" />: the halves share the height equally.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Divides_The_Height_Between_The_Halves()
	{
		// Arrange
		DocumentEditorView sut = new();

		Show(sut);

		TextEditor editor = sut.GetControl<TextEditor>(EditorName);

		TextEditor splitEditor = sut.GetControl<TextEditor>(SplitEditorName);

		// Act
		sut.IsSplit = true;

		Dispatcher.UIThread.RunJobs();

		// Assert
		splitEditor.Bounds.Height
			.Should()
			.BeApproximately(editor.Bounds.Height, 1.0);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.IsSplit" />: without the split the upper half takes the whole height back.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Off_Gives_The_Whole_Height_Back_To_The_Upper_Half()
	{
		// Arrange
		DocumentEditorView sut = new();

		Show(sut);

		TextEditor editor = sut.GetControl<TextEditor>(EditorName);

		double height = editor.Bounds.Height;

		sut.IsSplit = true;

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.IsSplit = false;

		Dispatcher.UIThread.RunJobs();

		// Assert
		editor.Bounds.Height
			.Should()
			.Be(height);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.IsSplit" />: without the split the upper half takes over the caret, the selection
	/// and the scroll position of an active lower half.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Off_Keeps_The_Place_Of_The_Active_Lower_Half()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 1000),
			IsSplit = true
		};

		Show(sut);

		TextEditor editor = sut.GetControl<TextEditor>(EditorName);

		TextEditor splitEditor = sut.GetControl<TextEditor>(SplitEditorName);

		splitEditor
			.TextArea
			.Focus();

		splitEditor.Select(40, 3);

		// The caret at the start of the selection, where the selection alone would not put it.
		splitEditor
			.TextArea
			.Caret
			.Position = new(line: 5, column: 1);

		Vector offset = new(0.0, 700.0);

		GetSplitScrollViewer(sut).Offset = offset;

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.IsSplit = false;

		Dispatcher.UIThread.RunJobs();

		// Assert
		editor.SelectionStart
			.Should()
			.Be(40);

		editor.SelectionLength
			.Should()
			.Be(3);

		editor.TextArea.Caret.Location
			.Should()
			.Be(new TextLocation(5, 1));

		GetScrollViewer(sut).Offset
			.Should()
			.Be(offset);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.IsSplit" />: without the split an active upper half stays where it is.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Off_Keeps_The_Place_Of_The_Active_Upper_Half()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 1000),
			IsSplit = true
		};

		Show(sut);

		Vector offset = new(0.0, 200.0);

		GetScrollViewer(sut).Offset = offset;

		GetSplitScrollViewer(sut).Offset = new(0.0, 700.0);

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.IsSplit = false;

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetScrollViewer(sut).Offset
			.Should()
			.Be(offset);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.IsSplit" />: without a split the upper half takes the whole height of the halves.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Off_Leaves_The_Whole_Height_To_The_Upper_Half()
	{
		// Arrange
		DocumentEditorView sut = new();

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextEditor>(EditorName).Bounds.Height
			.Should()
			.Be(sut.GetControl<Grid>(EditorsHostName).Bounds.Height);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.IsSplit" />: without the split the upper half is the active one again,
	/// even when the focus has already left the lower half.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Off_Makes_The_Upper_Half_Active()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true
		};

		Show(sut);

		sut
			.GetControl<DocumentTextEditor>(SplitEditorName)
			.TextArea
			.Focus();

		// The split toggle takes the focus when it is clicked.
		sut
			.GetControl<StackPanel>(ToolBarName)
			.Children
			.OfType<ToggleButton>()
			.First()
			.Focus();

		// Act
		sut.IsSplit = false;

		// Assert
		sut.ActiveEditor.Name
			.Should()
			.Be(EditorName);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.IsSplit" />: the lower half opens at the caret, the selection and the scroll position
	/// of the upper one.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Opens_The_Lower_Half_Where_The_Upper_One_Stands()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 1000)
		};

		Show(sut);

		TextEditor editor = sut.GetControl<TextEditor>(EditorName);

		TextEditor splitEditor = sut.GetControl<TextEditor>(SplitEditorName);

		editor.Select(20, 4);

		// The caret at the start of the selection, where the selection alone would not put it.
		editor
			.TextArea
			.Caret
			.Position = new(line: 3, column: 1);

		Vector offset = new(0.0, 500.0);

		GetScrollViewer(sut).Offset = offset;

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.IsSplit = true;

		Dispatcher.UIThread.RunJobs();

		// Assert
		splitEditor.SelectionStart
			.Should()
			.Be(20);

		splitEditor.SelectionLength
			.Should()
			.Be(4);

		splitEditor.TextArea.Caret.Position
			.Should()
			.Be(editor.TextArea.Caret.Position);

		GetSplitScrollViewer(sut).Offset
			.Should()
			.Be(offset);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.IsSplit" />: the lower half and the splitter are shown only in a split view.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Shows_The_Lower_Half_Only_When_Set([Values] bool isSplit)
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = isSplit
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextEditor>(SplitEditorName).IsVisible
			.Should()
			.Be(isSplit);

		sut.GetVisualDescendants().OfType<GridSplitter>().Single().IsVisible
			.Should()
			.Be(isSplit);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.IsSplit" />: a new split starts in the middle, whatever shares a drag of the splitter left.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Starts_In_The_Middle_After_A_Drag_Of_The_Splitter()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true
		};

		Window window = Show(sut);

		TextEditor editor = sut.GetControl<TextEditor>(EditorName);

		TextEditor splitEditor = sut.GetControl<TextEditor>(SplitEditorName);

		Point start = Center(window, sut.GetVisualDescendants().OfType<GridSplitter>().Single());

		Point end = start.WithY(start.Y + 100.0);

		window.MouseDown(start, MouseButton.Left);

		window.MouseMove(end);

		window.MouseUp(end, MouseButton.Left);

		sut.IsSplit = false;

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.IsSplit = true;

		Dispatcher.UIThread.RunJobs();

		// Assert
		splitEditor.Bounds.Height
			.Should()
			.BeApproximately(editor.Bounds.Height, 1.0);
	}

	/// <summary>
	/// <see cref="DocumentEditorView" />: detaching the control and attaching it again, as a tab switch does,
	/// keeps the selection and the scroll position.
	/// </summary>
	[AvaloniaTest]
	public void Keeps_The_View_When_Attached_Again()
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView" />: listens to the messages about the bookmarks once it stands in a window.
	/// </summary>
	[AvaloniaTest]
	public void OnAttachedToVisualTree_Registers_For_The_Bookmark_Messages()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 10)
		};

		// Act
		Show(sut);

		// Assert
		WeakReferenceMessenger.Default
			.IsRegistered<BookmarksChangedMessage>(sut)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorView" />: stops listening to the messages about the bookmarks when it leaves the window.
	/// </summary>
	[AvaloniaTest]
	public void OnDetachedFromVisualTree_Unregisters_From_The_Bookmark_Messages()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 10)
		};

		Window window = Show(sut);

		// Act
		window.Content = null;

		Dispatcher.UIThread.RunJobs();

		// Assert
		WeakReferenceMessenger.Default
			.IsRegistered<BookmarksChangedMessage>(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DocumentEditorView.Receive" />: the bookmarks of another editor leave the view state alone.
	/// </summary>
	[AvaloniaTest]
	public void Receive_Ignores_The_Bookmarks_Of_Another_Editor()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 100)
		};

		Show(sut);

		DocumentEditorView other = new()
		{
			Document = CreateDocument(lineCount: 100)
		};

		Show(other);

		// Act
		other.GetControl<DocumentTextEditor>(EditorName).Bookmarks.Toggle(3);

		// Assert
		sut.ViewState
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentEditorView.Receive" />: a change of the bookmarks reaches the view state at once,
	/// as a click on the bookmark margin moves neither the caret nor the view.
	/// </summary>
	[AvaloniaTest]
	public void Receive_Reports_The_Bookmarks_At_Once()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 100)
		};

		Show(sut);

		DocumentTextEditor editor = sut.GetControl<DocumentTextEditor>(EditorName);

		// Act
		editor.Bookmarks.Toggle(3);

		// Assert
		// A local keeps the assertion from being skipped by the null-conditional operator when there is no state.
		int[]? bookmarks = sut.ViewState?.Bookmarks;

		bookmarks
			.Should()
			.Equal(3);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.Receive" />: a bookmark set in the lower half reaches the view state too,
	/// as both halves share the bookmarks of the document.
	/// </summary>
	[AvaloniaTest]
	public void Receive_Reports_The_Bookmarks_Of_The_Lower_Half()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 100),
			IsSplit = true
		};

		Show(sut);

		DocumentTextEditor splitEditor = sut.GetControl<DocumentTextEditor>(SplitEditorName);

		// Act
		splitEditor.Bookmarks.Toggle(3);

		// Assert
		// A local keeps the assertion from being skipped by the null-conditional operator when there is no state.
		int[]? bookmarks = sut.ViewState?.Bookmarks;

		bookmarks
			.Should()
			.Equal(3);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ShowEndOfLine" />:the glyphs of line endings reach the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowEndOfLine_Reaches_The_Editor([Values] bool isShown)
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.ShowEndOfLine" />: the glyphs of line endings reach the lower half.
	/// </summary>
	[AvaloniaTest]
	public void ShowEndOfLine_Reaches_The_Lower_Half([Values] bool isShown)
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true,
			ShowEndOfLine = isShown
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextEditor>(SplitEditorName).Options.ShowEndOfLine
			.Should()
			.Be(isShown);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ShowSpaces" />: the glyphs of spaces reach the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowSpaces_Reaches_The_Editor([Values] bool isShown)
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.ShowSpaces" />: the glyphs of spaces reach the lower half.
	/// </summary>
	[AvaloniaTest]
	public void ShowSpaces_Reaches_The_Lower_Half([Values] bool isShown)
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true,
			ShowSpaces = isShown
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextEditor>(SplitEditorName).Options.ShowSpaces
			.Should()
			.Be(isShown);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ShowTabs" />: the glyphs of tabs reach the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowTabs_Reaches_The_Editor([Values] bool isShown)
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.ShowTabs" />: the glyphs of tabs reach the lower half.
	/// </summary>
	[AvaloniaTest]
	public void ShowTabs_Reaches_The_Lower_Half([Values] bool isShown)
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true,
			ShowTabs = isShown
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextEditor>(SplitEditorName).Options.ShowTabs
			.Should()
			.Be(isShown);
	}

	/// <summary>
	/// <see cref="DocumentEditorView" />: the lower half opens the same context menu as the upper one.
	/// </summary>
	[AvaloniaTest]
	public void SplitEditor_Shares_The_Context_Menu()
	{
		// Arrange
		DocumentEditorView sut = new();

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextEditor>(SplitEditorName).ContextMenu
			.Should()
			.NotBeNull()
			.And
			.BeSameAs(sut.GetControl<TextEditor>(EditorName).ContextMenu);
	}

	/// <summary>
	/// <see cref="DocumentEditorView" />: in a window too narrow for the status bar the block on the right edge stays whole
	/// and in view, while the blocks on the left give way.
	/// </summary>
	[AvaloniaTest]
	public void StatusBar_Keeps_The_Encoding_Whole_In_A_Narrow_Window()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			EncodingName = "UTF-8"
		};

		Window window = Show(sut);

		TextBlock caption = sut.GetControl<TextBlock>(EncodingCaptionName);

		double width = caption.Bounds.Width;

		// Act
		window.Width = 400.0;

		Dispatcher.UIThread.RunJobs();

		// Assert
		caption.Bounds.Width
			.Should()
			.Be(width);

		double? right = caption.TranslatePoint(new(width, 0.0), sut)?.X;

		right
			.Should()
			.BeLessThanOrEqualTo(sut.Bounds.Width);
	}

	/// <summary>
	/// <see cref="DocumentEditorView" />: the status bar shows the encoding only when one is given.
	/// </summary>
	[AvaloniaTest]
	public void StatusBar_Shows_The_Encoding_Only_When_It_Is_Given([Values] bool isGiven)
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.ToolBarContent" />: the content placed in the toolbar keeps the data context of the control.
	/// </summary>
	[AvaloniaTest]
	public void ToolBarContent_Keeps_The_Data_Context_Of_The_Control()
	{
		// Arrange
		object dataContext = new();

		Button button = new();

		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.ViewState" />: a selection and a caret beyond the document are brought inside it.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Clamps_A_Selection_Beyond_The_Document()
	{
		// Arrange
		TextDocument document = CreateDocument(lineCount: 10);

		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.ViewState" />: a document set right after the state is the one it is restored on,
	/// not the one it would have been lost with.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Is_Restored_On_A_Document_Set_After_It()
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.ViewState" />: restores the bookmarks.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Restores_The_Bookmarks()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 100)
		};

		Show(sut);

		// Act
		sut.ViewState = new DocumentViewState
		{
			Bookmarks = [2, 4],
			CaretPosition = new(line: 1, column: 1),
			ScrollOffset = default,
			SelectionLength = 0,
			SelectionStart = 0
		};

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.GetControl<DocumentTextEditor>(EditorName).Bookmarks.GetLines()
			.Should()
			.Equal(2, 4);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ViewState" />: the restored bookmarks show in the lower half as well.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Restores_The_Bookmarks_Into_The_Lower_Half()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 100),
			IsSplit = true
		};

		Show(sut);

		// Act
		sut.ViewState = new DocumentViewState
		{
			Bookmarks = [2, 4],
			CaretPosition = new(line: 1, column: 1),
			ScrollOffset = default,
			SelectionLength = 0,
			SelectionStart = 0
		};

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.GetControl<DocumentTextEditor>(SplitEditorName).Bookmarks.GetLines()
			.Should()
			.Equal(2, 4);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ViewState" />: the offset is restored, and the caret line does not scroll it away.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Restores_The_Offset_Instead_Of_The_Caret_Line()
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.ViewState" />: restores the selection and the caret.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Restores_The_Selection_And_The_Caret()
	{
		// Arrange
		DocumentEditorView sut = new()
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
	/// <see cref="DocumentEditorView.ViewState" />: a value set before the control is loaded is restored once it is.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Set_Before_Loading_Is_Restored_Once_Loaded()
	{
		// Arrange
		Vector offset = new(0.0, 500.0);

		DocumentEditorView sut = new()
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

	/// <summary>
	/// <see cref="DocumentEditorView.WordWrap" />: the wrapping of long lines reaches the lower half.
	/// </summary>
	[AvaloniaTest]
	public void WordWrap_Reaches_The_Lower_Half([Values] bool isWrapped)
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true,
			WordWrap = isWrapped
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<TextEditor>(SplitEditorName).WordWrap
			.Should()
			.Be(isWrapped);
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
	/// Returns the commands of a text editor.
	/// </summary>
	private static ICommand[] GetCommands(TextEditor editor)
	{
		return [.. editor
			.GetType()
			.GetProperties()
			.Where(static x => typeof(ICommand).IsAssignableFrom(x.PropertyType))
			.Select(x => (ICommand)x.GetValue(editor)!)];
	}

	/// <summary>
	/// Returns the commands of the items of a menu that open no submenu.
	/// </summary>
	private static ICommand?[] GetMenuCommands(ItemsControl menu)
	{
		return [.. menu
			.Items
			.OfType<MenuItem>()
			.SelectMany(static x => x.ItemCount > 0 ? GetMenuCommands(x) : [x.Command])];
	}

	/// <summary>
	/// Returns the scroll viewer of the text editor.
	/// </summary>
	private static ScrollViewer GetScrollViewer(DocumentEditorView editor)
	{
		return editor
			.GetControl<TextEditor>(EditorName)
			.GetVisualDescendants()
			.OfType<ScrollViewer>()
			.First(static x => x.Name == ScrollViewerName);
	}

	/// <summary>
	/// Returns the scroll viewer of the text editor of the lower half.
	/// </summary>
	private static ScrollViewer GetSplitScrollViewer(DocumentEditorView editor)
	{
		return editor
			.GetControl<TextEditor>(SplitEditorName)
			.GetVisualDescendants()
			.OfType<ScrollViewer>()
			.First(static x => x.Name == ScrollViewerName);
	}

	/// <summary>
	/// Shows the editor in a window of a fixed size and lets the layout settle.
	/// </summary>
	private static Window Show(DocumentEditorView editor)
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
