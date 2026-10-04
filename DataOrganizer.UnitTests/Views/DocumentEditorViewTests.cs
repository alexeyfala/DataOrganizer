using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AvaloniaEdit.TextMate;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Controls;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Messages.Documents;
using DataOrganizer.Views;
using Shared.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
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
	/// Name of the status bar block with the encoding of the text in the markup.
	/// </summary>
	private const string EncodingBlockName = "EncodingBlock";

	/// <summary>
	/// A block of PowerShell whose braces stand on lines of their own, which folds from its first line to its last.
	/// </summary>
	private const string FoldedText = "if ($value)\n{\n    Write-Host 'Text'\n}";

	/// <summary>
	/// Name of the status bar block with the language of the text in the markup.
	/// </summary>
	private const string LanguageBlockName = "LanguageBlock";

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
	/// <see cref="DocumentEditorView.CaptureViewState" />: a block that the engine folds by itself, as on a click on its
	/// marker, reaches the view state after a pause, as the engine tells no fold and neither the caret nor the view moves.
	/// </summary>
	[AvaloniaTest]
	public async Task CaptureViewState_Follows_A_Fold_Of_The_Engine_After_A_Pause()
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = CreateBlockDocument(blockCount: 10),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		FoldingSection block = GetFoldingMargin(sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor)
			.FoldingManager
			.AllFoldings
			.ElementAt(1);

		Func<bool> isReported = () =>
		{
			// The pause ends on a timer, which posts the capture to the UI thread.
			Dispatcher.UIThread.RunJobs();

			return sut.ViewState?.FoldedBlocks is [int start] && start == block.StartOffset;
		};

		// Act
		block.IsFolded = true;

		// Assert
		bool result = await isReported.WaitAsync(millisecondsDelay: 10, maxRepeats: 1000);

		result
			.Should()
			.BeTrue();
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
	/// <see cref="DocumentEditorView.CaptureViewState" />: a text with every block folded is kept as no unfolded block,
	/// whatever the number of its blocks.
	/// </summary>
	[AvaloniaTest]
	public void CaptureViewState_Reports_Every_Block_Folded_As_No_Unfolded_Block()
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = CreateBlockDocument(blockCount: 10),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		foreach (FoldingSection block in GetFoldingMargin(sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor)
			.FoldingManager
			.AllFoldings)
		{
			block.IsFolded = true;
		}

		// Act
		sut.CaptureViewState();

		// Assert
		// Locals keep the assertions from being skipped by the null-conditional operator when there is no state.
		int[]? foldedBlocks = sut.ViewState?.FoldedBlocks;

		int[]? unfoldedBlocks = sut.ViewState?.UnfoldedBlocks;

		foldedBlocks
			.Should()
			.BeNull();

		unfoldedBlocks
			.Should()
			.BeEmpty();
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
	/// <see cref="DocumentEditorView.CaptureViewState" />: keeps the folded blocks or the unfolded ones, whichever are fewer.
	/// </summary>
	[AvaloniaTest]
	public void CaptureViewState_Reports_The_Fewer_Of_The_Folded_And_The_Unfolded_Blocks([Values] bool isMostlyFolded)
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = CreateBlockDocument(blockCount: 10),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		FoldingSection[] blocks = [.. GetFoldingMargin(sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor)
			.FoldingManager
			.AllFoldings];

		foreach (FoldingSection block in blocks)
		{
			block.IsFolded = isMostlyFolded;
		}

		// One block stands apart from all the others.
		blocks[1].IsFolded = !isMostlyFolded;

		int start = blocks[1].StartOffset;

		// Act
		sut.CaptureViewState();

		// Assert
		DocumentViewState? state = sut.ViewState;

		int[]? kept = isMostlyFolded ? state?.UnfoldedBlocks : state?.FoldedBlocks;

		int[]? left = isMostlyFolded ? state?.FoldedBlocks : state?.UnfoldedBlocks;

		kept
			.Should()
			.Equal(start);

		left
			.Should()
			.BeNull();
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
	/// <see cref="Control.ContextFlyout" />: the submenu of the folding opens only while the text of the active half folds.
	/// </summary>
	[AvaloniaTest]
	public void ContextFlyout_Enables_The_Folding_Only_While_The_Text_Folds([Values] bool hasLanguage)
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = hasLanguage ? null : PowerShellLanguage
		};

		Show(sut);

		SplitDocumentEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName);

		// The submenu is the button whose flyout holds the command of the block at the caret.
		Button folding = ((Control)((Flyout)editor.ContextFlyout!).Content!)
			.GetLogicalDescendants()
			.OfType<Button>()
			.Single(x => GetFlyoutCommands(x.Flyout).Contains(editor.PrimaryEditor.ToggleFoldingCommand));

		// Act
		sut.SyntaxLanguage = hasLanguage ? PowerShellLanguage : null;

		// Assert
		folding.IsEnabled
			.Should()
			.Be(hasLanguage);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.DefaultEncoding" />: the encoding that the text takes by default reaches the status bar.
	/// </summary>
	[AvaloniaTest]
	public void DefaultEncoding_Reaches_The_Status_Bar()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			DefaultEncoding = "windows-1251",
			EncodingName = "Windows-1251"
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<ChoiceSelector>(EncodingBlockName).DefaultChoice
			.Should()
			.Be("windows-1251");
	}

	/// <summary>
	/// <see cref="DocumentEditorView.DefaultSyntaxLanguage" />: the language that the text takes by default reaches the status bar.
	/// </summary>
	[AvaloniaTest]
	public void DefaultSyntaxLanguage_Reaches_The_Status_Bar()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			DefaultSyntaxLanguage = PowerShellLanguage
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<ChoiceSelector>(LanguageBlockName).DefaultChoice
			.Should()
			.Be(PowerShellLanguage);
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
		sut.GetControl<ChoiceSelector>(EncodingBlockName).Caption
			.Should()
			.Be("UTF-8-BOM");
	}

	/// <summary>
	/// <see cref="DocumentEditorView.Encoding" />: an encoding chosen in the status bar becomes the encoding of the text.
	/// </summary>
	[AvaloniaTest]
	public void Encoding_Follows_The_Status_Bar()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Encoding = "windows-1251",
			EncodingName = "Windows-1251"
		};

		Show(sut);

		// Act
		sut
			.GetControl<ChoiceSelector>(EncodingBlockName)
			.SetCurrentValue(ChoiceSelector.SelectedChoiceProperty, "cp866");

		// Assert
		sut.Encoding
			.Should()
			.Be("cp866");
	}

	/// <summary>
	/// <see cref="DocumentEditorView.Encoding" />: the encoding of the text reaches the status bar.
	/// </summary>
	[AvaloniaTest]
	public void Encoding_Reaches_The_Status_Bar()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			Encoding = "cp866",
			EncodingName = "CP866"
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<ChoiceSelector>(EncodingBlockName).SelectedChoice
			.Should()
			.Be("cp866");
	}

	/// <summary>
	/// <see cref="DocumentEditorView.FindUnreadableEncodingsCommand" />: the command reaches the list of encodings, which
	/// runs it as it opens.
	/// </summary>
	[AvaloniaTest]
	public void FindUnreadableEncodingsCommand_Reaches_The_Status_Bar()
	{
		// Arrange
		RelayCommand command = new(static () => { });

		DocumentEditorView sut = new()
		{
			EncodingName = "UTF-8",
			FindUnreadableEncodingsCommand = command
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<ChoiceSelector>(EncodingBlockName).FlyoutOpeningCommand
			.Should()
			.BeSameAs(command);
	}

	/// <summary>
	/// <see cref="FoldingMargin" />: the folding markers keep the gray of the line numbers.
	/// </summary>
	[AvaloniaTest]
	public void FoldingMargin_Keeps_The_Gray_Of_The_Line_Numbers()
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		Show(sut);

		// Assert
		DocumentTextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

		GetFoldingMargin(editor).FoldingMarkerBrush
			.Should()
			.BeSameAs(editor.LineNumbersForeground);
	}

	/// <summary>
	/// <see cref="FoldingMargin" />: the folding markers have no fill of their own and take the color of the text on hover.
	/// </summary>
	[AvaloniaTest]
	public void FoldingMargin_Takes_The_Fill_And_The_Hover_Of_The_Theme()
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		Show(sut);

		// Assert
		FoldingMargin margin = GetFoldingMargin(sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor);

		// Locals keep the assertions from being skipped by the null-conditional operator when a brush is not a color.
		Color? fill = (margin.FoldingMarkerBackgroundBrush as ISolidColorBrush)?.Color;

		Color? hoverFill = (margin.SelectedFoldingMarkerBackgroundBrush as ISolidColorBrush)?.Color;

		fill
			.Should()
			.Be(Colors.Transparent);

		hoverFill
			.Should()
			.Be(Colors.Transparent);

		margin.SelectedFoldingMarkerBrush
			.Should()
			.BeSameAs(sut.FindResource("MaterialBodyBrush"));
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
	/// <see cref="DocumentEditorView.LanguageChoices" />: the status bar offers plain text first and then every language with
	/// a grammar.
	/// </summary>
	[AvaloniaTest]
	public void LanguageChoices_Offer_Plain_Text_First_And_Then_Every_Language()
	{
		// Arrange
		DocumentEditorView sut = new();

		// Act
		Show(sut);

		// Assert
		string?[] expected = [null, .. SyntaxRegistry.Instance.Languages.Select(static x => x.Id)];

		sut.GetControl<ChoiceSelector>(LanguageBlockName).Choices!.Select(static x => x.Id)
			.Should()
			.Equal(expected);
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
	/// <see cref="DocumentEditorView" />: listens to the messages about the folding once it stands in a window.
	/// </summary>
	[AvaloniaTest]
	public void OnAttachedToVisualTree_Registers_For_The_Folding_Messages()
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
			.IsRegistered<FoldingChangedMessage>(sut)
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
	/// <see cref="DocumentEditorView" />: stops listening to the messages about the folding when it leaves the window.
	/// </summary>
	[AvaloniaTest]
	public void OnDetachedFromVisualTree_Unregisters_From_The_Folding_Messages()
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
			.IsRegistered<FoldingChangedMessage>(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DocumentEditorView.Receive(BookmarksChangedMessage)" />: the bookmarks of another editor leave the view
	/// state alone.
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

		// An editor out of any window, so that the pause after the layout of the view cannot run out before the check.
		DocumentTextEditor other = new()
		{
			Document = CreateDocument(lineCount: 100)
		};

		// Act
		other.Bookmarks.Toggle(3);

		// Assert
		sut.ViewState
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentEditorView.Receive(FoldingChangedMessage)" />: the folding of another editor leaves the view state
	/// alone.
	/// </summary>
	[AvaloniaTest]
	public void Receive_Ignores_The_Folding_Of_Another_Editor()
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = CreateBlockDocument(blockCount: 10),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		// An editor out of any window, so that the pause after the layout of the view cannot run out before the check.
		using DocumentTextEditor other = new()
		{
			Document = CreateBlockDocument(blockCount: 10),
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		other.FoldAllCommand.Execute(null);

		// Assert
		sut.ViewState
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentEditorView.Receive(FoldingChangedMessage)" />: the folding of the inactive half leaves the view
	/// state alone, as each half folds on its own and the state is that of the active one.
	/// </summary>
	[AvaloniaTest]
	public void Receive_Ignores_The_Folding_Of_The_Inactive_Half()
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = CreateBlockDocument(blockCount: 10),
			IsSplit = true,
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		// Act
		sut.GetControl<SplitDocumentEditor>(EditorName).SecondaryEditor!.FoldAllCommand.Execute(null);

		// Assert
		sut.ViewState
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentEditorView.Receive(BookmarksChangedMessage)" />: a change of the bookmarks reaches the view state
	/// at once, as a click on the bookmark margin moves neither the caret nor the view.
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
	/// <see cref="DocumentEditorView.Receive(BookmarksChangedMessage)" />: a bookmark set in the lower half reaches the view
	/// state too, as both halves share the bookmarks of the document.
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
	/// <see cref="DocumentEditorView.Receive(FoldingChangedMessage)" />: a fold by a command reaches the view state at once,
	/// as it may fold only blocks out of view, which the view does not draw anew.
	/// </summary>
	[AvaloniaTest]
	public void Receive_Reports_The_Folding_At_Once()
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = CreateBlockDocument(blockCount: 10),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		// Act
		sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor.FoldAllCommand.Execute(null);

		// Assert
		int[]? unfoldedBlocks = sut.ViewState?.UnfoldedBlocks;

		unfoldedBlocks
			.Should()
			.BeEmpty();
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

		ChoiceSelector block = sut.GetControl<ChoiceSelector>(EncodingBlockName);

		double width = block.Bounds.Width;

		// Act
		window.Width = 400.0;

		Dispatcher.UIThread.RunJobs();

		// Assert
		block.Bounds.Width
			.Should()
			.Be(width);

		double? right = block.TranslatePoint(new(width, 0.0), sut)?.X;

		right
			.Should()
			.BeLessThanOrEqualTo(sut.Bounds.Width);
	}

	/// <summary>
	/// <see cref="DocumentEditorView" />: the status bar offers every encoding that a text can be read in.
	/// </summary>
	[AvaloniaTest]
	public void StatusBar_Offers_Every_Encoding()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			EncodingName = "UTF-8"
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<ChoiceSelector>(EncodingBlockName).Choices
			.Should()
			.BeSameAs(FileTextCodec.EncodingChoices);
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
		sut.GetControl<ChoiceSelector>(EncodingBlockName).IsVisible
			.Should()
			.Be(isGiven);
	}

	/// <summary>
	/// <see cref="DocumentEditorView" />: the status bar tells why an encoding cannot be chosen, while the languages can
	/// all be chosen.
	/// </summary>
	[AvaloniaTest]
	public void StatusBar_Tells_Why_An_Encoding_Cannot_Be_Chosen()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			EncodingName = "UTF-8"
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<ChoiceSelector>(EncodingBlockName).UnavailableTip
			.Should()
			.NotBeNullOrEmpty();

		sut.GetControl<ChoiceSelector>(LanguageBlockName).UnavailableTip
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentEditorView.SyntaxLanguage" />: a language chosen in the status bar becomes the language of the text.
	/// </summary>
	[AvaloniaTest]
	public void SyntaxLanguage_Follows_The_Status_Bar()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		// Act
		sut
			.GetControl<ChoiceSelector>(LanguageBlockName)
			.SetCurrentValue(ChoiceSelector.SelectedChoiceProperty, "bat");

		// Assert
		sut.SyntaxLanguage
			.Should()
			.Be("bat");
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
	/// <see cref="DocumentEditorView.SyntaxLanguage" />: the language of the text reaches the status bar.
	/// </summary>
	[AvaloniaTest]
	public void SyntaxLanguage_Reaches_The_Status_Bar()
	{
		// Arrange
		DocumentEditorView sut = new()
		{
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<ChoiceSelector>(LanguageBlockName).SelectedChoice
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
	/// <see cref="DocumentEditorView.UnreadableEncodings" />: the encodings that cannot read the text reach the status
	/// bar.
	/// </summary>
	[AvaloniaTest]
	public void UnreadableEncodings_Reach_The_Status_Bar()
	{
		// Arrange
		string[] unreadable = ["utf-32"];

		DocumentEditorView sut = new()
		{
			EncodingName = "UTF-8",
			UnreadableEncodings = unreadable
		};

		// Act
		Show(sut);

		// Assert
		sut.GetControl<ChoiceSelector>(EncodingBlockName).UnavailableChoices
			.Should()
			.BeSameAs(unreadable);
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
	/// <see cref="DocumentEditorView.ViewState" />: a block folded over the caret, as a click on its marker leaves it, stays
	/// folded, as the caret goes in before the block folds.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Keeps_A_Block_Folded_Over_The_Caret()
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = CreateBlockDocument(blockCount: 10),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		DocumentTextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

		int start = editor.GetBlockStarts(isFolded: false)[0];

		// Act
		// The caret inside the first block, which folds from the end of line 1 to the end of line 4.
		sut.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 3, column: 3),
			FoldedBlocks = [start],
			ScrollOffset = default,
			SelectionLength = 0,
			SelectionStart = 0
		};

		Dispatcher.UIThread.RunJobs();

		// Assert
		editor.GetBlockStarts(isFolded: true)
			.Should()
			.Equal(start);
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
	/// <see cref="DocumentEditorView.ViewState" />: restores the folded blocks, and the other blocks stay unfolded.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Restores_The_Folded_Blocks()
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = CreateBlockDocument(blockCount: 10),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		DocumentTextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

		int[] starts = editor.GetBlockStarts(isFolded: false);

		// Act
		sut.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 1, column: 1),
			FoldedBlocks = [starts[1], starts[3]],
			ScrollOffset = default,
			SelectionLength = 0,
			SelectionStart = 0
		};

		Dispatcher.UIThread.RunJobs();

		// Assert
		editor.GetBlockStarts(isFolded: true)
			.Should()
			.Equal(starts[1], starts[3]);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.ViewState" />: the restored folded blocks fold in the lower half as well.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Restores_The_Folded_Blocks_Into_The_Lower_Half()
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = CreateBlockDocument(blockCount: 10),
			IsSplit = true,
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		SplitDocumentEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName);

		int start = editor.PrimaryEditor.GetBlockStarts(isFolded: false)[1];

		// Act
		sut.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 1, column: 1),
			FoldedBlocks = [start],
			ScrollOffset = default,
			SelectionLength = 0,
			SelectionStart = 0
		};

		Dispatcher.UIThread.RunJobs();

		// Assert
		editor.SecondaryEditor!.GetBlockStarts(isFolded: true)
			.Should()
			.Equal(start);
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
	/// <see cref="DocumentEditorView.ViewState" />: a file opened again shows the lines at the top that it showed, although
	/// the folded blocks change the height of the text that the offset is measured over.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Restores_The_Place_Over_The_Folded_Blocks()
	{
		// Arrange
		using DocumentEditorView source = new()
		{
			Document = CreateBlockDocument(blockCount: 200),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(source);

		DocumentTextEditor sourceEditor = source.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

		sourceEditor.FoldAllCommand.Execute(null);

		GetScrollViewer(source).Offset = new(0.0, 1000.0);

		Dispatcher.UIThread.RunJobs();

		source.CaptureViewState();

		using DocumentEditorView sut = new()
		{
			Document = new(source.Document!.Text),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		// Act
		sut.ViewState = source.ViewState;

		Dispatcher.UIThread.RunJobs();

		// Assert
		int[] shownLines = GetShownLines(sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor, count: 5);

		shownLines
			.Should()
			.Equal(GetShownLines(sourceEditor, count: 5));

		// Every block is folded, so only the first line of each block shows.
		shownLines
			.Should()
			.OnlyContain(static x => x % 4 == 1);
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
	/// <see cref="DocumentEditorView.ViewState" />: restores the unfolded blocks, and every other block folds.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Restores_The_Unfolded_Blocks()
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = CreateBlockDocument(blockCount: 10),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		DocumentTextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

		int start = editor.GetBlockStarts(isFolded: false)[1];

		// Act
		sut.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 1, column: 1),
			ScrollOffset = default,
			SelectionLength = 0,
			SelectionStart = 0,
			UnfoldedBlocks = [start]
		};

		Dispatcher.UIThread.RunJobs();

		// Assert
		editor.GetBlockStarts(isFolded: false)
			.Should()
			.Equal(start);
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
	/// <see cref="DocumentEditorView.ViewState" />: offsets where no block starts, as an outdated state may hold, fold
	/// nothing, and the text shows every line that no restored block hides.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Shows_The_Text_Over_Blocks_That_Are_Not_There([Values] bool isMostlyFolded)
	{
		// Arrange
		using DocumentEditorView sut = new()
		{
			Document = CreateBlockDocument(blockCount: 10),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		DocumentTextEditor editor = sut.GetControl<SplitDocumentEditor>(EditorName).PrimaryEditor;

		int length = editor.Document.TextLength;

		// The second block, which holds lines 5 to 8, among offsets where no block starts.
		int[] blocks =
		[
			int.MinValue,
			-1,
			0,
			3,
			editor.GetBlockStarts(isFolded: false)[1],
			length,
			length + 1,
			int.MaxValue
		];

		// Act
		sut.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 1, column: 1),
			FoldedBlocks = isMostlyFolded ? null : blocks,
			ScrollOffset = default,
			SelectionLength = 0,
			SelectionStart = 0,
			UnfoldedBlocks = isMostlyFolded ? blocks : null
		};

		Dispatcher.UIThread.RunJobs();

		// Assert
		// Either every block but the second one folds, or the second one alone.
		GetShownLines(editor, count: 7)
			.Should()
			.Equal(isMostlyFolded ? [1, 5, 6, 7, 8, 9, 13] : [1, 2, 3, 4, 5, 9, 10]);
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
	/// Creates a document of blocks, each a numbered line followed by three indented lines, which folds from the end of
	/// the numbered line to the end of the block.
	/// </summary>
	private static TextDocument CreateBlockDocument(int blockCount)
	{
		return new(string.Join('\n', Enumerable
			.Range(1, blockCount)
			.Select(static x => $"Block {x:D4}\n    One\n    Two\n    Three")));
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
	/// Returns the margin of the folding markers of a text editor.
	/// </summary>
	private static FoldingMargin GetFoldingMargin(TextEditor editor)
	{
		return editor
			.TextArea
			.LeftMargins
			.OfType<FoldingMargin>()
			.Single();
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
	/// Returns the numbers of the lines that start the first lines of the view of an editor, from its top.
	/// </summary>
	private static int[] GetShownLines(TextEditor editor, int count)
	{
		return [.. editor
			.TextArea
			.TextView
			.VisualLines
			.Take(count)
			.Select(static x => x.FirstDocumentLine.LineNumber)];
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
