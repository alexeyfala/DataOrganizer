using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
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
	/// Name of the editor with the halves in the markup.
	/// </summary>
	private const string EditorName = "Editor";

	/// <summary>
	/// Name of the caption of the encoding in the markup.
	/// </summary>
	private const string EncodingCaptionName = "EncodingCaption";

	/// <summary>
	/// Language of <see cref="PowerShellText" />.
	/// </summary>
	private const string PowerShellLanguage = "powershell";

	/// <summary>
	/// A line of PowerShell.
	/// </summary>
	private const string PowerShellText = "if ($value) { Write-Host 'Text' }";

	/// <summary>
	/// Name of the scroll viewer in the template of the text editor.
	/// </summary>
	private const string ScrollViewerName = "PART_ScrollViewer";

	/// <summary>
	/// Name of the panel with the buttons of the control in the toolbar in the markup.
	/// </summary>
	private const string ToolBarName = "ToolBar";
	#endregion

	#region Methods	
	/// <summary>
	/// <see cref="SplitDocumentEditor.ActiveEditor" />: every command of the context menu acts on the active half.
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

		DocumentTextEditor splitEditor = sut.GetControl<SplitDocumentEditor>(EditorName).SecondaryEditor!;

		// Act
		splitEditor
			.TextArea
			.Focus();

		// Assert
		ICommand?[] commands = GetFlyoutCommands(sut.GetControl<SplitDocumentEditor>(EditorName).ContextFlyout);

		commands
			.Should()
			.NotBeEmpty()
			.And
			.BeSubsetOf(GetCommands(splitEditor));
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.ActiveEditor" />: the status bar shows the caret of the active half.
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

		DocumentTextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

		DocumentTextEditor splitEditor = sut.GetControl<SplitDocumentEditor>(EditorName).SecondaryEditor!;

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
	/// <see cref="SplitDocumentEditor.ActiveEditor" />: the scroll buttons of the toolbar act on the active half.
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

		DocumentTextEditor splitEditor = sut.GetControl<SplitDocumentEditor>(EditorName).SecondaryEditor!;

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
	/// <see cref="SplitDocumentEditor.ActiveEditor" />: the scroll buttons of the toolbar act on the upper half
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
		DocumentTextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

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
	/// <see cref="SplitDocumentEditor.ActiveEditor" />: a new active half reports its caret, selection and offset at once.
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

		DocumentTextEditor splitEditor = sut.GetControl<SplitDocumentEditor>(EditorName).SecondaryEditor!;

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

		DocumentTextEditor splitEditor = sut.GetControl<SplitDocumentEditor>(EditorName).SecondaryEditor!;

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

		TextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

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
	/// <see cref="Control.ContextFlyout" />: the buttons of the menu check their commands again whenever it opens,
	/// as a selection changed while it was closed tells no command.
	/// </summary>
	[AvaloniaTest]
	public void ContextFlyout_Checks_The_Commands_When_Opened()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = CreateDocument(lineCount: 10)
		};

		Show(sut);

		SplitDocumentEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName);

		FlyoutBase flyout = editor.ContextFlyout!;

		Button copy = ((Control)((Flyout)flyout).Content!)
			.GetLogicalDescendants()
			.OfType<Button>()
			.Single(x => x.Command == editor.PrimaryEditor.CopyCommand);

		editor.PrimaryEditor.Select(0, 4);

		flyout.ShowAt(editor);

		Dispatcher.UIThread.RunJobs();

		flyout.Hide();

		Dispatcher.UIThread.RunJobs();

		editor.PrimaryEditor.Select(0, 0);

		// Act
		flyout.ShowAt(editor);

		Dispatcher.UIThread.RunJobs();

		// Assert
		copy.IsEffectivelyEnabled
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DocumentEditorView.Dispose" />: the highlighting of the editor goes away.
	/// </summary>
	[AvaloniaTest]
	public void Dispose_Removes_The_Highlighting()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Document = new(PowerShellText),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		// Act
		sut.Dispose();

		// Assert
		HasHighlighting(sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor)
			.Should()
			.BeFalse();
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

		TextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

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
		sut.GetControl<SplitDocumentEditor>(EditorName).IsReadOnly
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
		sut.GetControl<SplitDocumentEditor>(EditorName).IsSensitive
			.Should()
			.Be(isSensitive);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.IsSplit" />: the split reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Reaches_The_Editor([Values] bool isSplit)
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = isSplit
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<SplitDocumentEditor>(EditorName).IsSplit
			.Should()
			.Be(isSplit);
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

		TextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

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
		other.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor.Bookmarks.Toggle(3);

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

		DocumentTextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

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

		DocumentTextEditor splitEditor = sut.GetControl<SplitDocumentEditor>(EditorName).SecondaryEditor!;

		// Act
		splitEditor.Bookmarks.Toggle(3);

		// Assert
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
		sut.GetControl<SplitDocumentEditor>(EditorName).ShowEndOfLine
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
		sut.GetControl<SplitDocumentEditor>(EditorName).ShowSpaces
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
		sut.GetControl<SplitDocumentEditor>(EditorName).ShowTabs
			.Should()
			.Be(isShown);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.SplitShare" />: a drag of the splitter comes back from the editor.
	/// </summary>
	[AvaloniaTest]
	public void SplitShare_Follows_A_Drag_Of_The_Splitter()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true
		};

		Window window = Show(sut);

		Point start = Center(window, sut.GetVisualDescendants().OfType<GridSplitter>().Single());

		Point end = start.WithY(start.Y + 100.0);

		// Act
		window.MouseDown(start, MouseButton.Left);

		window.MouseMove(end);

		window.MouseUp(end, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.SplitShare
			.Should()
			.BeGreaterThan(0.5);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.SplitShare" />: the share of the upper half reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void SplitShare_Reaches_The_Editor()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true,
			SplitShare = 0.25
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<SplitDocumentEditor>(EditorName).SplitShare
			.Should()
			.Be(0.25);
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
	/// <see cref="DocumentEditorView.SyntaxLanguage" />: the language of the text reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void SyntaxLanguage_Reaches_The_Editor()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<SplitDocumentEditor>(EditorName).SyntaxLanguage
			.Should()
			.Be(PowerShellLanguage);
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
		TextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

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
		TextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

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
		sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor.Bookmarks.GetLines()
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
		sut.GetControl<SplitDocumentEditor>(EditorName).SecondaryEditor!.Bookmarks.GetLines()
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
	/// <see cref="DocumentEditorView.ViewState" />: a split that came before the document, as on a new opening of the file,
	/// opens the lower half at the restored offset too.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Restores_The_Offset_Into_The_Lower_Half()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			IsSplit = true
		};

		Show(sut);

		Vector offset = new(0.0, 500.0);

		// Act
		sut.Document = CreateDocument(lineCount: 1000);

		sut.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 30, column: 1),
			ScrollOffset = offset,
			SelectionLength = 0,
			SelectionStart = 0
		};

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetSplitScrollViewer(sut).Offset
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
		TextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

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
	/// <see cref="DocumentEditorView.WordWrap" />: the wrapping of long lines reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void WordWrap_Reaches_The_Editor([Values] bool isWrapped)
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			WordWrap = isWrapped
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<SplitDocumentEditor>(EditorName).WordWrap
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
	/// Returns the commands of the buttons of a flyout that open no flyout, with those of the flyouts the others open.
	/// </summary>
	private static ICommand?[] GetFlyoutCommands(FlyoutBase? flyout)
	{
		if (flyout is not Flyout { Content: Control content })
		{
			return [];
		}

		return [.. content
			.GetLogicalDescendants()
			.OfType<Button>()
			.SelectMany(static x => x.Flyout is null ? [x.Command] : GetFlyoutCommands(x.Flyout))];
	}

	/// <summary>
	/// Returns the scroll viewer of the text editor of the upper half.
	/// </summary>
	private static ScrollViewer GetScrollViewer(DocumentEditorView editor)
	{
		return editor
			.GetControl<SplitDocumentEditor>(EditorName)
			.PrimaryEditor
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
			.GetControl<SplitDocumentEditor>(EditorName)
			.SecondaryEditor!
			.GetVisualDescendants()
			.OfType<ScrollViewer>()
			.First(static x => x.Name == ScrollViewerName);
	}

	/// <summary>
	/// <c>True</c> when the editor has the syntax highlighting.
	/// </summary>
	private static bool HasHighlighting(TextEditor editor)
	{
		return editor
			.TextArea
			.TextView
			.LineTransformers
			.OfType<TextMateColoringTransformer>()
			.Any();
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
