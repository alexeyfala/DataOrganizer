using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Controls;
using System.Linq;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(SplitDocumentEditor)}"" type")]
internal class SplitDocumentEditorTests
{
	#region Data
	/// <summary>
	/// Name of the editor of the upper half.
	/// </summary>
	private const string PrimaryEditorName = "PrimaryEditor";

	/// <summary>
	/// Name of the editor of the lower half.
	/// </summary>
	private const string SecondaryEditorName = "SecondaryEditor";

	/// <summary>
	/// Resource key of the theme of the splitter between the halves.
	/// </summary>
	private const string SplitterThemeKey = "DoubleLineGridSplitterTheme";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SplitDocumentEditor.ActiveEditor" />: a right click beside the text, where the text area takes no focus,
	/// makes its half the active one.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Follows_A_Right_Click_Beside_The_Text()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true
		};

		Window window = Show(sut);

		ScrollMarkMargin scrollMarks = sut.SecondaryEditor!
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
			.Be(SecondaryEditorName);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.ActiveEditor" />: a right click in the text makes its half the active one.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Follows_A_Right_Click_In_The_Text()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true
		};

		Window window = Show(sut);

		Point point = Center(window, sut.SecondaryEditor!);

		// Act
		window.MouseDown(point, MouseButton.Right);

		window.MouseUp(point, MouseButton.Right);

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.ActiveEditor.Name
			.Should()
			.Be(SecondaryEditorName);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.ActiveEditor" />: the focus that comes back to the upper half makes it the active one again.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Follows_The_Focus_Back_Into_The_Upper_Half()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true
		};

		Show(sut);

		sut.SecondaryEditor!
			.TextArea
			.Focus();

		// Act
		sut.PrimaryEditor
			.TextArea
			.Focus();

		// Assert
		sut.ActiveEditor.Name
			.Should()
			.Be(PrimaryEditorName);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.ActiveEditor" />: the focus that goes into the lower half makes it the active one.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Follows_The_Focus_Into_The_Lower_Half()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true
		};

		Show(sut);

		// Act
		sut.SecondaryEditor!
			.TextArea
			.Focus();

		// Assert
		sut.ActiveEditor.Name
			.Should()
			.Be(SecondaryEditorName);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.ActiveEditor" />: the text of the active half keeps the focus through a drag of the splitter.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Keeps_The_Focus_After_A_Drag_Of_The_Splitter()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true
		};

		Window window = Show(sut);

		DocumentTextEditor secondaryEditor = sut.SecondaryEditor!;

		secondaryEditor
			.TextArea
			.Focus();

		Point start = Center(window, sut.GetVisualDescendants().OfType<GridSplitter>().Single());

		Point end = start.WithY(start.Y + 50.0);

		// Act
		window.MouseDown(start, MouseButton.Left);

		window.MouseMove(end);

		window.MouseUp(end, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		secondaryEditor.TextArea.IsFocused
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.ActiveEditor" />: the upper half is the active one from the start.
	/// </summary>
	[AvaloniaTest]
	public void ActiveEditor_Starts_With_The_Upper_Half()
	{
		// Act
		SplitDocumentEditor sut = new();

		// Assert
		sut.ActiveEditor.Name
			.Should()
			.Be(PrimaryEditorName);
	}

	/// <summary>
	/// <see cref="Control.ContextFlyout" />: the menu of the control opens on a right click in the lower half too.
	/// </summary>
	[AvaloniaTest]
	public void ContextFlyout_Opens_In_The_Lower_Half()
	{
		// Arrange
		Flyout flyout = new()
		{
			Content = new TextBlock
			{
				Text = "Item"
			}
		};

		SplitDocumentEditor sut = new()
		{
			ContextFlyout = flyout,
			IsSplit = true
		};

		Window window = Show(sut);

		Point point = Center(window, sut.SecondaryEditor!);

		// Act
		window.MouseDown(point, MouseButton.Right);

		window.MouseUp(point, MouseButton.Right);

		Dispatcher.UIThread.RunJobs();

		// Assert
		flyout.IsOpen
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="Control.ContextFlyout" />: a right click on the splitter opens no menu of the text.
	/// </summary>
	[AvaloniaTest]
	public void ContextFlyout_Stays_Closed_On_The_Splitter()
	{
		// Arrange
		Flyout flyout = new()
		{
			Content = new TextBlock
			{
				Text = "Item"
			}
		};

		SplitDocumentEditor sut = new()
		{
			ContextFlyout = flyout,
			IsSplit = true
		};

		Window window = Show(sut);

		Point point = Center(window, sut.GetVisualDescendants().OfType<GridSplitter>().Single());

		// Act
		window.MouseDown(point, MouseButton.Right);

		window.MouseUp(point, MouseButton.Right);

		Dispatcher.UIThread.RunJobs();

		// Assert
		flyout.IsOpen
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.Document" />: both halves show the document.
	/// </summary>
	[AvaloniaTest]
	public void Document_Reaches_Both_Halves()
	{
		// Arrange
		TextDocument document = CreateDocument(lineCount: 10);

		SplitDocumentEditor sut = new()
		{
			Document = document,
			IsSplit = true
		};

		// Act
		Show(sut);

		// Assert
		sut.PrimaryEditor.Document
			.Should()
			.BeSameAs(document);

		sut.SecondaryEditor!.Document
			.Should()
			.BeSameAs(document);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.FontSize" />: a notch of the wheel with Ctrl in the lower half zooms both halves.
	/// </summary>
	[AvaloniaTest]
	public void FontSize_Follows_A_Ctrl_Wheel_Notch_In_The_Lower_Half()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			Document = CreateDocument(lineCount: 100),
			FontSize = 14.0,
			IsSplit = true
		};

		Window window = Show(sut);

		// Act
		window.MouseWheel(Center(window, sut.SecondaryEditor!), new(0.0, 1.0), RawInputModifiers.Control);

		// Assert
		sut.FontSize
			.Should()
			.Be(14.5);

		sut.PrimaryEditor.FontSize
			.Should()
			.Be(14.5);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.FontSize" />: a half keeps following the font size after a zoom of its own.
	/// </summary>
	[AvaloniaTest]
	public void FontSize_Reaches_A_Half_After_Its_Zoom()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			Document = CreateDocument(lineCount: 100),
			FontSize = 14.0,
			IsSplit = true
		};

		Window window = Show(sut);

		window.MouseWheel(Center(window, sut.SecondaryEditor!), new(0.0, 1.0), RawInputModifiers.Control);

		// Act
		sut.FontSize = 20.0;

		// Assert
		sut.SecondaryEditor!.FontSize
			.Should()
			.Be(20.0);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.FontSize" />: both halves take the font size.
	/// </summary>
	[AvaloniaTest]
	public void FontSize_Reaches_Both_Halves()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			FontSize = 20.0,
			IsSplit = true
		};

		// Act
		Show(sut);

		// Assert
		sut.PrimaryEditor.FontSize
			.Should()
			.Be(20.0);

		sut.SecondaryEditor!.FontSize
			.Should()
			.Be(20.0);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.Foreground" />: both halves take the brush of the text over the one of their theme.
	/// </summary>
	[AvaloniaTest]
	public void Foreground_Reaches_Both_Halves()
	{
		// Arrange
		IBrush brush = Brushes.Red;

		SplitDocumentEditor sut = new()
		{
			Foreground = brush,
			IsSplit = true
		};

		// Act
		Show(sut);

		// Assert
		sut.PrimaryEditor.Foreground
			.Should()
			.BeSameAs(brush);

		sut.SecondaryEditor!.Foreground
			.Should()
			.BeSameAs(brush);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.IsReadOnly" />: the read-only mode reaches both halves.
	/// </summary>
	[AvaloniaTest]
	public void IsReadOnly_Reaches_Both_Halves([Values] bool isReadOnly)
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsReadOnly = isReadOnly,
			IsSplit = true
		};

		// Act
		Show(sut);

		// Assert
		sut.PrimaryEditor.IsReadOnly
			.Should()
			.Be(isReadOnly);

		sut.SecondaryEditor!.IsReadOnly
			.Should()
			.Be(isReadOnly);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.IsSensitive" />: the sensitivity of the text reaches both halves.
	/// </summary>
	[AvaloniaTest]
	public void IsSensitive_Reaches_Both_Halves([Values] bool isSensitive)
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSensitive = isSensitive,
			IsSplit = true
		};

		// Act
		Show(sut);

		// Assert
		sut.PrimaryEditor.IsSensitive
			.Should()
			.Be(isSensitive);

		sut.SecondaryEditor!.IsSensitive
			.Should()
			.Be(isSensitive);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.IsSplit" />: the halves share the height equally.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Divides_The_Height_Between_The_Halves()
	{
		// Arrange
		SplitDocumentEditor sut = new();

		Show(sut);

		// Act
		sut.IsSplit = true;

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.SecondaryEditor!.Bounds.Height
			.Should()
			.BeApproximately(sut.PrimaryEditor.Bounds.Height, 1.0);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.IsSplit" />: a drag of the splitter to an edge leaves the half there in view.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Keeps_Each_Half_In_View_After_A_Drag_To_The_Edge([Values] bool isUpward)
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true
		};

		Window window = Show(sut);

		Point start = Center(window, sut.GetVisualDescendants().OfType<GridSplitter>().Single());

		Point end = start.WithY(isUpward ? -100.0 : window.Height + 100.0);

		// Act
		window.MouseDown(start, MouseButton.Left);

		window.MouseMove(end);

		window.MouseUp(end, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		DocumentTextEditor editor = isUpward ? sut.PrimaryEditor : sut.SecondaryEditor!;

		editor.Bounds.Height
			.Should()
			.BeGreaterThan(editor.TextArea.TextView.DefaultLineHeight);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.IsSplit" />: without the split the upper half takes the whole height back.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Off_Gives_The_Whole_Height_Back_To_The_Upper_Half()
	{
		// Arrange
		SplitDocumentEditor sut = new();

		Show(sut);

		double height = sut.PrimaryEditor.Bounds.Height;

		sut.IsSplit = true;

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.IsSplit = false;

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.PrimaryEditor.Bounds.Height
			.Should()
			.Be(height);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.IsSplit" />: without the split the upper half takes over the caret, the selection
	/// and the scroll position of an active lower half.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Off_Keeps_The_Place_Of_The_Active_Lower_Half()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			Document = CreateDocument(lineCount: 1000),
			IsSplit = true
		};

		Show(sut);

		DocumentTextEditor secondaryEditor = sut.SecondaryEditor!;

		secondaryEditor
			.TextArea
			.Focus();

		secondaryEditor.Select(40, 3);

		// The caret at the start of the selection, where the selection alone would not put it.
		secondaryEditor
			.TextArea
			.Caret
			.Position = new(line: 5, column: 1);

		Vector offset = new(0.0, 700.0);

		secondaryEditor.ScrollViewer!.Offset = offset;

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.IsSplit = false;

		Dispatcher.UIThread.RunJobs();

		// Assert
		DocumentTextEditor primaryEditor = sut.PrimaryEditor;

		primaryEditor.SelectionStart
			.Should()
			.Be(40);

		primaryEditor.SelectionLength
			.Should()
			.Be(3);

		primaryEditor.TextArea.Caret.Location
			.Should()
			.Be(new TextLocation(5, 1));

		primaryEditor.ScrollViewer!.Offset
			.Should()
			.Be(offset);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.IsSplit" />: without the split an active upper half stays where it is.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Off_Keeps_The_Place_Of_The_Active_Upper_Half()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			Document = CreateDocument(lineCount: 1000),
			IsSplit = true
		};

		Show(sut);

		Vector offset = new(0.0, 200.0);

		sut.PrimaryEditor.ScrollViewer!.Offset = offset;

		sut.SecondaryEditor!.ScrollViewer!.Offset = new(0.0, 700.0);

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.IsSplit = false;

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.PrimaryEditor.ScrollViewer!.Offset
			.Should()
			.Be(offset);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.IsSplit" />: without a split the upper half takes the whole height of the control.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Off_Leaves_The_Whole_Height_To_The_Upper_Half()
	{
		// Arrange
		SplitDocumentEditor sut = new();

		// Act
		Show(sut);

		// Assert
		sut.PrimaryEditor.Bounds.Height
			.Should()
			.Be(sut.Bounds.Height);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.IsSplit" />: without the split the upper half is the active one again,
	/// even when the focus has already left the lower half.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Off_Makes_The_Upper_Half_Active()
	{
		// Arrange
		Button button = new();

		SplitDocumentEditor sut = new()
		{
			IsSplit = true
		};

		DockPanel.SetDock(button, Dock.Top);

		Show(new DockPanel
		{
			Children =
			{
				button,
				sut
			}
		});

		sut.SecondaryEditor!
			.TextArea
			.Focus();

		// A split toggle takes the focus when it is clicked.
		button.Focus();

		// Act
		sut.IsSplit = false;

		// Assert
		sut.ActiveEditor.Name
			.Should()
			.Be(PrimaryEditorName);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.IsSplit" />: the lower half opens at the caret, the selection and the scroll position
	/// of the upper one.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Opens_The_Lower_Half_Where_The_Upper_One_Stands()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			Document = CreateDocument(lineCount: 1000)
		};

		Show(sut);

		DocumentTextEditor primaryEditor = sut.PrimaryEditor;

		primaryEditor.Select(20, 4);

		// The caret at the start of the selection, where the selection alone would not put it.
		primaryEditor
			.TextArea
			.Caret
			.Position = new(line: 3, column: 1);

		Vector offset = new(0.0, 500.0);

		primaryEditor.ScrollViewer!.Offset = offset;

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.IsSplit = true;

		Dispatcher.UIThread.RunJobs();

		// Assert
		DocumentTextEditor secondaryEditor = sut.SecondaryEditor!;

		secondaryEditor.SelectionStart
			.Should()
			.Be(20);

		secondaryEditor.SelectionLength
			.Should()
			.Be(4);

		secondaryEditor.TextArea.Caret.Position
			.Should()
			.Be(primaryEditor.TextArea.Caret.Position);

		secondaryEditor.ScrollViewer!.Offset
			.Should()
			.Be(offset);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.IsSplit" />: the lower half and the splitter are shown only in a split view.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Shows_The_Lower_Half_Only_When_Set([Values] bool isSplit)
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true
		};

		Show(sut);

		// Act
		sut.IsSplit = isSplit;

		// Assert
		sut.SecondaryEditor!.IsVisible
			.Should()
			.Be(isSplit);

		sut.GetVisualDescendants().OfType<GridSplitter>().Single().IsVisible
			.Should()
			.Be(isSplit);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.IsSplit" />: a new split starts in the middle, whatever shares a drag of the splitter left.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Starts_In_The_Middle_After_A_Drag_Of_The_Splitter()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true
		};

		Window window = Show(sut);

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
		sut.SecondaryEditor!.Bounds.Height
			.Should()
			.BeApproximately(sut.PrimaryEditor.Bounds.Height, 1.0);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.PrimaryEditor" />: takes the focus given to it and passes it to its text.
	/// </summary>
	[AvaloniaTest]
	public void PrimaryEditor_Takes_The_Focus()
	{
		// Arrange
		SplitDocumentEditor sut = new();

		Show(sut);

		// Act
		sut.PrimaryEditor.Focus();

		// Assert
		sut.PrimaryEditor.TextArea.IsFocused
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.SecondaryEditor" />: a document that is never split gets no lower half.
	/// </summary>
	[AvaloniaTest]
	public void SecondaryEditor_Waits_For_The_First_Split()
	{
		// Arrange
		SplitDocumentEditor sut = new();

		// Act
		Show(sut);

		// Assert
		// A local keeps the assertion from being skipped by the null-conditional operator when there is no lower half.
		string? name = sut.SecondaryEditor?.Name;

		name
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.ShowEndOfLine" />: the glyphs of line endings reach both halves.
	/// </summary>
	[AvaloniaTest]
	public void ShowEndOfLine_Reaches_Both_Halves([Values] bool isShown)
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true,
			ShowEndOfLine = isShown
		};

		// Act
		Show(sut);

		// Assert
		sut.PrimaryEditor.Options.ShowEndOfLine
			.Should()
			.Be(isShown);

		sut.SecondaryEditor!.Options.ShowEndOfLine
			.Should()
			.Be(isShown);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.ShowSpaces" />: the glyphs of spaces reach both halves.
	/// </summary>
	[AvaloniaTest]
	public void ShowSpaces_Reaches_Both_Halves([Values] bool isShown)
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true,
			ShowSpaces = isShown
		};

		// Act
		Show(sut);

		// Assert
		sut.PrimaryEditor.Options.ShowSpaces
			.Should()
			.Be(isShown);

		sut.SecondaryEditor!.Options.ShowSpaces
			.Should()
			.Be(isShown);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.ShowTabs" />: the glyphs of tabs reach both halves.
	/// </summary>
	[AvaloniaTest]
	public void ShowTabs_Reaches_Both_Halves([Values] bool isShown)
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true,
			ShowTabs = isShown
		};

		// Act
		Show(sut);

		// Assert
		sut.PrimaryEditor.Options.ShowTabs
			.Should()
			.Be(isShown);

		sut.SecondaryEditor!.Options.ShowTabs
			.Should()
			.Be(isShown);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.SplitShare" />: the share divides the height between the halves.
	/// </summary>
	[AvaloniaTest]
	public void SplitShare_Divides_The_Height_Between_The_Halves()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true,
			SplitShare = 0.25
		};

		// Act
		Show(sut);

		// Assert
		double upperHeight = sut.PrimaryEditor.Bounds.Height;

		(upperHeight / (upperHeight + sut.SecondaryEditor!.Bounds.Height))
			.Should()
			.BeApproximately(0.25, 0.01);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.SplitShare" />: a drag of the splitter sets the share of the upper half.
	/// </summary>
	[AvaloniaTest]
	public void SplitShare_Follows_A_Drag_Of_The_Splitter()
	{
		// Arrange
		SplitDocumentEditor sut = new()
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
		double upperHeight = sut.PrimaryEditor.Bounds.Height;

		sut.SplitShare
			.Should()
			.BeApproximately(upperHeight / (upperHeight + sut.SecondaryEditor!.Bounds.Height), 0.01);
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.SplitShare" />: the end of the split sets the share back to the middle.
	/// </summary>
	[AvaloniaTest]
	public void SplitShare_Returns_To_The_Middle_When_The_Split_Ends()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true,
			SplitShare = 0.25
		};

		Show(sut);

		// Act
		sut.IsSplit = false;

		// Assert
		sut.SplitShare
			.Should()
			.Be(0.5);
	}

	/// <summary>
	/// The splitter between the halves takes the double line theme.
	/// </summary>
	[AvaloniaTest]
	public void Splitter_Takes_The_Double_Line_Theme()
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true
		};

		// Act
		Show(sut);

		// Assert
		sut.GetVisualDescendants().OfType<GridSplitter>().Single().Theme
			.Should()
			.BeSameAs(Application.Current!.FindResource(SplitterThemeKey));
	}

	/// <summary>
	/// <see cref="SplitDocumentEditor.WordWrap" />: the wrapping of long lines reaches both halves.
	/// </summary>
	[AvaloniaTest]
	public void WordWrap_Reaches_Both_Halves([Values] bool isWrapped)
	{
		// Arrange
		SplitDocumentEditor sut = new()
		{
			IsSplit = true,
			WordWrap = isWrapped
		};

		// Act
		Show(sut);

		// Assert
		sut.PrimaryEditor.WordWrap
			.Should()
			.Be(isWrapped);

		sut.SecondaryEditor!.WordWrap
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
	/// Shows the content in a window of a fixed size and lets the layout settle.
	/// </summary>
	private static Window Show(Control content)
	{
		Window window = new()
		{
			Content = content,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		return window;
	}
	#endregion
}
