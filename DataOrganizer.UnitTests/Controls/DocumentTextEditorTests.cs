using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Styling;
using DataOrganizer.Controls;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentTextEditor)}"" type")]
internal class DocumentTextEditorTests
{
	#region Methods
	/// <summary>
	/// <see cref="BookmarkMargin" />: stands left of the line numbers, as in Visual Studio.
	/// </summary>
	[AvaloniaTest]
	public void BookmarkMargin_Stands_Left_Of_The_Line_Numbers()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("One\nTwo\nThree")
		};

		// Act
		Show(sut);

		// Assert
		BookmarkMargin margin = sut
			.TextArea
			.LeftMargins
			.OfType<BookmarkMargin>()
			.Single();

		LineNumberMargin lineNumbers = sut
			.TextArea
			.LeftMargins
			.OfType<LineNumberMargin>()
			.Single();

		margin.Bounds.Width
			.Should()
			.BePositive();

		margin.Bounds.Right
			.Should()
			.BeLessThanOrEqualTo(lineNumbers.Bounds.Left);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ClearBookmarksCommand" />: there is something to remove only with a bookmark.
	/// </summary>
	[AvaloniaTest]
	public void ClearBookmarksCommand_Needs_A_Bookmark([Values] bool hasBookmark)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 3)
		};

		if (hasBookmark)
		{
			sut.Bookmarks.Toggle(2);
		}

		// Act
		bool canExecute = sut.ClearBookmarksCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(hasBookmark);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ClearBookmarksCommand" />: removes every bookmark.
	/// </summary>
	[AvaloniaTest]
	public void ClearBookmarksCommand_Removes_Every_Bookmark()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 3)
		};

		sut.Bookmarks.Toggle(1);

		sut.Bookmarks.Toggle(3);

		// Act
		sut.ClearBookmarksCommand.Execute(null);

		// Assert
		sut.Bookmarks.GetLines()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ConvertLineEndingsCommand" />: every line break takes the style, including those
	/// of empty lines, where a carriage return meets the line feed of the next line.
	/// </summary>
	[AvaloniaTest]
	[TestCase(LineEnding.CrLf, "A\r\n\r\nB\r\n\r\nC")]
	[TestCase(LineEnding.Lf, "A\n\nB\n\nC")]
	[TestCase(LineEnding.Cr, "A\r\rB\r\rC")]
	public void ConvertLineEndingsCommand_Brings_Every_Line_Break_To_The_Style(LineEnding lineEnding, string expected)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("A\r\rB\n\nC")
		};

		// Act
		sut.ConvertLineEndingsCommand.Execute(lineEnding);

		// Assert
		sut.Text
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ConvertLineEndingsCommand" />: a line break just typed counts at once,
	/// while the status catches up with it only after a delay.
	/// </summary>
	[AvaloniaTest]
	public void ConvertLineEndingsCommand_Follows_An_Edit_At_Once()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("First")
		};

		sut.Document.Insert(sut.Document.TextLength, "\nSecond");

		// Act
		bool canExecute = sut.ConvertLineEndingsCommand.CanExecute(LineEnding.CrLf);

		// Assert
		canExecute
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ConvertLineEndingsCommand" />: the line breaks cannot be changed in read-only mode.
	/// </summary>
	[AvaloniaTest]
	public void ConvertLineEndingsCommand_Is_Denied_In_Read_Only_Mode([Values] bool isReadOnly)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("First\nSecond"),
			IsReadOnly = isReadOnly
		};

		// Act
		bool canExecute = sut.ConvertLineEndingsCommand.CanExecute(LineEnding.CrLf);

		// Assert
		canExecute
			.Should()
			.Be(!isReadOnly);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ConvertLineEndingsCommand" />: a single undo brings back every line break.
	/// </summary>
	[AvaloniaTest]
	public void ConvertLineEndingsCommand_Is_Undone_In_One_Step()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("First\r\nSecond\nThird")
		};

		sut.ConvertLineEndingsCommand.Execute(LineEnding.Cr);

		// Act
		sut.UndoCommand.Execute(null);

		// Assert
		sut.Text
			.Should()
			.Be("First\r\nSecond\nThird");
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ConvertLineEndingsCommand" />: a document is not converted to the style it has.
	/// </summary>
	[AvaloniaTest]
	[TestCase(LineEnding.CrLf, true)]
	[TestCase(LineEnding.Lf, false)]
	[TestCase(LineEnding.Cr, true)]
	public void ConvertLineEndingsCommand_Needs_Another_Style(LineEnding lineEnding, bool expected)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("First\nSecond")
		};

		// Act
		bool canExecute = sut.ConvertLineEndingsCommand.CanExecute(lineEnding);

		// Assert
		canExecute
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ConvertLineEndingsCommand" />: there is something to convert only in a document
	/// with line breaks.
	/// </summary>
	[AvaloniaTest]
	public void ConvertLineEndingsCommand_Needs_Line_Breaks([Values] bool hasLineBreaks)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new(hasLineBreaks ? "First\nSecond" : "First")
		};

		// Act
		bool canExecute = sut.ConvertLineEndingsCommand.CanExecute(LineEnding.CrLf);

		// Assert
		canExecute
			.Should()
			.Be(hasLineBreaks);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ConvertLineEndingsCommand" />: the status shows the new style without the delay
	/// that follows an edit.
	/// </summary>
	[AvaloniaTest]
	public void ConvertLineEndingsCommand_Updates_The_Status_At_Once()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("First\r\nSecond\nThird")
		};

		// Act
		sut.ConvertLineEndingsCommand.Execute(LineEnding.Lf);

		// Assert
		sut.Status.LineEnding
			.Should()
			.Be(LineEnding.Lf);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.CutCommand" />: the selection cannot be cut in read-only mode.
	/// </summary>
	[AvaloniaTest]
	public void CutCommand_Is_Denied_In_Read_Only_Mode([Values] bool isReadOnly)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text"),
			IsReadOnly = isReadOnly
		};

		sut.Select(0, 4);

		// Act
		bool canExecute = sut.CutCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(!isReadOnly);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.CutCommand" />: there is something to cut only when text is selected.
	/// </summary>
	[AvaloniaTest]
	public void CutCommand_Needs_A_Selection([Values] bool isSelected)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text")
		};

		if (isSelected)
		{
			sut.Select(0, 4);
		}

		// Act
		bool canExecute = sut.CutCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(isSelected);
	}

	/// <summary>
	/// <see cref="TextEditor.Document" />: the bookmarks pass to a new document and start there without the bookmarks
	/// of the old one.
	/// </summary>
	[AvaloniaTest]
	public void Document_Resets_The_Bookmarks()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("One\nTwo\nThree")
		};

		sut.Bookmarks.Toggle(2);

		// Act
		sut.Document = new("Four\nFive\nSix");

		// Assert
		sut.Bookmarks.Document
			.Should()
			.BeSameAs(sut.Document);

		sut.Bookmarks.GetLines()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="TextEditor.IsReadOnly" />: the undo keys leave the text as it is in read-only mode.
	/// </summary>
	[AvaloniaTest]
	public void IsReadOnly_Denies_The_Undo_Gesture([Values] bool isReadOnly)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text")
		};

		Window window = Show(sut);

		sut.Document.Insert(0, "New ");

		sut.IsReadOnly = isReadOnly;

		sut.TextArea.Focus();

		// Ctrl+Z, or ⌘+Z on macOS
		RawInputModifiers modifiers = OperatingSystem.IsMacOS()
			? RawInputModifiers.Meta
			: RawInputModifiers.Control;

		// Act
		window.KeyPressQwerty(PhysicalKey.Z, modifiers);

		window.KeyReleaseQwerty(PhysicalKey.Z, modifiers);

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.Text
			.Should()
			.Be(isReadOnly ? "New Some text" : "Some text");
	}

	/// <summary>
	/// <see cref="TextEditor.IsReadOnly" />: an edit made before read-only mode can be undone once the mode is off.
	/// </summary>
	[AvaloniaTest]
	public void IsReadOnly_Keeps_The_Undo_History()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text")
		};

		sut.Document.Insert(0, "New ");

		sut.IsReadOnly = true;

		sut.IsReadOnly = false;

		// Act
		sut.UndoCommand.Execute(null);

		// Assert
		sut.Text
			.Should()
			.Be("Some text");
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.NextBookmarkCommand" />: a bookmarked line out of view comes into view.
	/// </summary>
	[AvaloniaTest]
	public void NextBookmarkCommand_Brings_The_Line_Into_View()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 1000)
		};

		Show(sut);

		sut.Bookmarks.Toggle(800);

		// Act
		sut.NextBookmarkCommand.Execute(null);

		Dispatcher.UIThread.RunJobs();

		// Assert
		TextView textView = sut.TextArea.TextView;

		textView.GetVisualTopByDocumentLine(800)
			.Should()
			.BeInRange(
				textView.VerticalOffset,
				textView.VerticalOffset + sut.ViewportHeight - textView.DefaultLineHeight);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.NextBookmarkCommand" />: the caret goes to the start of the next bookmarked line,
	/// and the selection goes away.
	/// </summary>
	[AvaloniaTest]
	public void NextBookmarkCommand_Moves_The_Caret_To_The_Start_Of_The_Next_Bookmarked_Line()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 10)
		};

		sut.Bookmarks.Toggle(3);

		sut.Bookmarks.Toggle(7);

		sut.Select(sut.Document.GetLineByNumber(5).Offset, 3);

		// Act
		sut.NextBookmarkCommand.Execute(null);

		// Assert
		sut.TextArea.Caret.Location
			.Should()
			.Be(new TextLocation(7, 1));

		sut.SelectionLength
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.NextBookmarkCommand" />: there is somewhere to go only with a bookmark.
	/// </summary>
	[AvaloniaTest]
	public void NextBookmarkCommand_Needs_A_Bookmark([Values] bool hasBookmark)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 3)
		};

		if (hasBookmark)
		{
			sut.Bookmarks.Toggle(2);
		}

		// Act
		bool canExecute = sut.NextBookmarkCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(hasBookmark);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.NextBookmarkCommand" />: runs on F2, as in Notepad++.
	/// </summary>
	[AvaloniaTest]
	public void NextBookmarkCommand_Runs_On_F2()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 3)
		};

		Window window = Show(sut);

		sut.Bookmarks.Toggle(2);

		sut.Bookmarks.Toggle(3);

		sut.TextArea.Focus();

		// Act
		window.KeyPressQwerty(PhysicalKey.F2, RawInputModifiers.None);

		window.KeyReleaseQwerty(PhysicalKey.F2, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		// Assert
		// From the first line the next bookmark is on the second line, while the previous one would be on the third.
		sut.TextArea.Caret.Line
			.Should()
			.Be(2);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.PasteCommand" />: nothing can be pasted in read-only mode.
	/// </summary>
	[AvaloniaTest]
	public void PasteCommand_Is_Denied_In_Read_Only_Mode([Values] bool isReadOnly)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text"),
			IsReadOnly = isReadOnly
		};

		// Act
		bool canExecute = sut.PasteCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(!isReadOnly);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.PreviousBookmarkCommand" />: the caret goes to the start of the previous bookmarked
	/// line, and the selection goes away.
	/// </summary>
	[AvaloniaTest]
	public void PreviousBookmarkCommand_Moves_The_Caret_To_The_Start_Of_The_Previous_Bookmarked_Line()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 10)
		};

		sut.Bookmarks.Toggle(3);

		sut.Bookmarks.Toggle(7);

		sut.Select(sut.Document.GetLineByNumber(5).Offset, 3);

		// Act
		sut.PreviousBookmarkCommand.Execute(null);

		// Assert
		sut.TextArea.Caret.Location
			.Should()
			.Be(new TextLocation(3, 1));

		sut.SelectionLength
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.PreviousBookmarkCommand" />: there is somewhere to go only with a bookmark.
	/// </summary>
	[AvaloniaTest]
	public void PreviousBookmarkCommand_Needs_A_Bookmark([Values] bool hasBookmark)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 3)
		};

		if (hasBookmark)
		{
			sut.Bookmarks.Toggle(2);
		}

		// Act
		bool canExecute = sut.PreviousBookmarkCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(hasBookmark);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.PreviousBookmarkCommand" />: runs on Shift+F2, as in Notepad++.
	/// </summary>
	[AvaloniaTest]
	public void PreviousBookmarkCommand_Runs_On_Shift_F2()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 3)
		};

		Window window = Show(sut);

		sut.Bookmarks.Toggle(2);

		sut.Bookmarks.Toggle(3);

		sut.TextArea.Focus();

		// Act
		window.KeyPressQwerty(PhysicalKey.F2, RawInputModifiers.Shift);

		window.KeyReleaseQwerty(PhysicalKey.F2, RawInputModifiers.Shift);

		Dispatcher.UIThread.RunJobs();

		// Assert
		// From the first line the previous bookmark goes round to the third line, while the next one would be on the second.
		sut.TextArea.Caret.Line
			.Should()
			.Be(3);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.RedoCommand" />: an undone edit cannot be redone in read-only mode.
	/// </summary>
	[AvaloniaTest]
	public void RedoCommand_Is_Denied_In_Read_Only_Mode([Values] bool isReadOnly)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text")
		};

		sut.Document.Insert(0, "New ");

		sut.Document.UndoStack.Undo();

		sut.IsReadOnly = isReadOnly;

		// Act
		bool canExecute = sut.RedoCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(!isReadOnly);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.RedoCommand" />: there is something to redo only after an edit is undone.
	/// </summary>
	[AvaloniaTest]
	public void RedoCommand_Needs_An_Undone_Edit([Values] bool isUndone)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text")
		};

		sut.Document.Insert(0, "New ");

		if (isUndone)
		{
			sut.Document.UndoStack.Undo();
		}

		// Act
		bool canExecute = sut.RedoCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(isUndone);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ShowEndOfLine" />: turns the glyphs of line endings on and off.
	/// </summary>
	[AvaloniaTest]
	public void ShowEndOfLine_Sets_The_Engine_Option([Values] bool isShown)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			ShowEndOfLine = !isShown
		};

		// Act
		sut.ShowEndOfLine = isShown;

		// Assert
		sut.Options.ShowEndOfLine
			.Should()
			.Be(isShown);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ShowSpaces" />: turns the glyphs of spaces on and off.
	/// </summary>
	[AvaloniaTest]
	public void ShowSpaces_Sets_The_Engine_Option([Values] bool isShown)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			ShowSpaces = !isShown
		};

		// Act
		sut.ShowSpaces = isShown;

		// Assert
		sut.Options.ShowSpaces
			.Should()
			.Be(isShown);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ShowTabs" />: turns the glyphs of tabs on and off.
	/// </summary>
	[AvaloniaTest]
	public void ShowTabs_Sets_The_Engine_Option([Values] bool isShown)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			ShowTabs = !isShown
		};

		// Act
		sut.ShowTabs = isShown;

		// Assert
		sut.Options.ShowTabs
			.Should()
			.Be(isShown);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.Status" />: the number of characters follows an edit.
	/// </summary>
	[AvaloniaTest]
	public void Status_Counts_The_Characters_After_An_Edit()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("First\nSecond")
		};

		// Act
		sut.Document.Insert(sut.Document.TextLength, "\nThird");

		// Assert
		sut.Status.TextLength
			.Should()
			.Be(18);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.Status" />: a rectangular selection is counted without the gaps between its rows.
	/// </summary>
	[AvaloniaTest]
	public void Status_Counts_The_Characters_Of_A_Rectangular_Selection()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("abcdef\nabcdef")
		};

		Show(sut);

		// Act
		sut.TextArea.Selection = new RectangleSelection(
			sut.TextArea,
			new(line: 1, column: 2),
			new(line: 2, column: 4));

		// Assert
		sut.Status.SelectionLength
			.Should()
			.Be(4);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.Status" />: the number of lines follows an edit.
	/// </summary>
	[AvaloniaTest]
	public void Status_Counts_The_Lines_After_An_Edit()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("First\nSecond")
		};

		// Act
		sut.Document.Insert(sut.Document.TextLength, "\nThird");

		// Assert
		sut.Status.LineCount
			.Should()
			.Be(3);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.Status" />: counts the selected characters.
	/// </summary>
	[AvaloniaTest]
	public void Status_Counts_The_Selected_Characters()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text")
		};

		// Act
		sut.Select(5, 4);

		// Assert
		sut.Status.SelectionLength
			.Should()
			.Be(4);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.Status" />: counts the lines the selection touches.
	/// </summary>
	[AvaloniaTest]
	public void Status_Counts_The_Selected_Lines()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("First\nSecond\nThird")
		};

		// Act
		sut.Select(2, 8);

		// Assert
		sut.Status.SelectionLineCount
			.Should()
			.Be(2);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.Status" />: a new document puts the caret at its start, drops the selection
	/// and brings its own lines.
	/// </summary>
	[AvaloniaTest]
	public void Status_Follows_A_New_Document()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("First\r\nSecond\r\nThird")
		};

		sut.Select(7, 6);

		// Act
		sut.Document = new("First\nSecond");

		// Assert
		sut.Status
			.Should()
			.Be(new DocumentStatus
			{
				CaretOffset = 0,
				Column = 1,
				Line = 1,
				LineCount = 2,
				LineEnding = LineEnding.Lf,
				SelectionLength = 0,
				SelectionLineCount = 0,
				TextLength = 12
			});
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.Status" />: reports the line and the column of the caret.
	/// </summary>
	[AvaloniaTest]
	public void Status_Follows_The_Caret()
	{
		// Arrange, Act
		DocumentTextEditor sut = new()
		{
			Document = new("First\nSecond"),
			CaretOffset = 9
		};

		// Assert
		sut.Status.Line
			.Should()
			.Be(2);

		sut.Status.Column
			.Should()
			.Be(4);

		sut.Status.CaretOffset
			.Should()
			.Be(9);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.Status" />: an edit before the caret moves its offset, while its line and column stay.
	/// </summary>
	[AvaloniaTest]
	public void Status_Follows_The_Caret_After_An_Edit_Before_It()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("First\nSecond"),
			CaretOffset = 8
		};

		// Act
		sut.Document.Insert(5, "!");

		// Assert
		sut.Status.CaretOffset
			.Should()
			.Be(9);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.Status" />: the line endings of a new document are found at once.
	/// </summary>
	[AvaloniaTest]
	[TestCase("First", LineEnding.None)]
	[TestCase("First\r\nSecond\r\n", LineEnding.CrLf)]
	[TestCase("First\nSecond\n", LineEnding.Lf)]
	[TestCase("First\rSecond\r", LineEnding.Cr)]
	[TestCase("First\r\nSecond\nThird", LineEnding.Mixed)]
	public void Status_Reports_The_Line_Ending(string text, LineEnding expected)
	{
		// Arrange, Act
		DocumentTextEditor sut = new()
		{
			Document = new(text)
		};

		// Assert
		sut.Status.LineEnding
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ToggleBookmarkCommand" />: runs on Ctrl+F2, with ⌘ for Ctrl on macOS, as in Notepad++.
	/// </summary>
	[AvaloniaTest]
	public void ToggleBookmarkCommand_Runs_On_Ctrl_F2()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 3)
		};

		Window window = Show(sut);

		sut.CaretOffset = sut.Document.GetLineByNumber(2).Offset;

		sut.TextArea.Focus();

		RawInputModifiers modifiers = OperatingSystem.IsMacOS()
			? RawInputModifiers.Meta
			: RawInputModifiers.Control;

		// Act
		window.KeyPressQwerty(PhysicalKey.F2, modifiers);

		window.KeyReleaseQwerty(PhysicalKey.F2, modifiers);

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.Bookmarks.GetLines()
			.Should()
			.Equal(2);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ToggleBookmarkCommand" />: sets a bookmark on the caret line without one and removes
	/// the bookmark of the caret line with one.
	/// </summary>
	[AvaloniaTest]
	public void ToggleBookmarkCommand_Toggles_The_Bookmark_Of_The_Caret_Line([Values] bool isBookmarked)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = CreateDocument(lineCount: 3)
		};

		if (isBookmarked)
		{
			sut.Bookmarks.Toggle(2);
		}

		sut.CaretOffset = sut.Document.GetLineByNumber(2).Offset;

		// Act
		sut.ToggleBookmarkCommand.Execute(null);

		// Assert
		int[] expected = isBookmarked ? [] : [2];

		sut.Bookmarks.GetLines()
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.TransformCommand" />: the case changes in the selected text only.
	/// </summary>
	[AvaloniaTest]
	[TestCase(TextTransform.UpperCase, "one TWO three")]
	[TestCase(TextTransform.LowerCase, "one two three")]
	[TestCase(TextTransform.TitleCase, "one Two three")]
	[TestCase(TextTransform.InvertCase, "one TwO three")]
	public void TransformCommand_Changes_The_Case_Of_The_Selection(TextTransform transform, string expected)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("one tWo three")
		};

		sut.Select(4, 3);

		// Act
		sut.TransformCommand.Execute(transform);

		// Assert
		sut.Text
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.TransformCommand" />: words in capitals get title case too,
	/// though .NET takes them for abbreviations and keeps them.
	/// </summary>
	[AvaloniaTest]
	public void TransformCommand_Converts_Words_In_Capitals_To_Title_Case()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("ПРИВЕТ МИР")
		};

		sut.SelectAll();

		// Act
		sut.TransformCommand.Execute(TextTransform.TitleCase);

		// Assert
		sut.Text
			.Should()
			.Be("Привет Мир");
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.TransformCommand" />: the transformations of whole lines work without a selection.
	/// </summary>
	[AvaloniaTest]
	public void TransformCommand_Is_Allowed_Without_A_Selection(
		[Values(
			TextTransform.RemoveTrailingWhitespace,
			TextTransform.LeadingTabsToSpaces,
			TextTransform.LeadingSpacesToTabs)] TextTransform transform)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text")
		};

		// Act
		bool canExecute = sut.TransformCommand.CanExecute(transform);

		// Assert
		canExecute
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.TransformCommand" />: no transformation is allowed in read-only mode.
	/// </summary>
	[AvaloniaTest]
	public void TransformCommand_Is_Denied_In_Read_Only_Mode([Values] TextTransform transform, [Values] bool isReadOnly)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text"),
			IsReadOnly = isReadOnly
		};

		sut.SelectAll();

		// Act
		bool canExecute = sut.TransformCommand.CanExecute(transform);

		// Assert
		canExecute
			.Should()
			.Be(!isReadOnly);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.TransformCommand" />: the case and the leading spaces change only in a selection,
	/// so that a stray click does not rewrite the whole document.
	/// </summary>
	[AvaloniaTest]
	public void TransformCommand_Needs_A_Selection(
		[Values(
			TextTransform.UpperCase,
			TextTransform.LowerCase,
			TextTransform.TitleCase,
			TextTransform.InvertCase,
			TextTransform.RemoveLeadingWhitespace)] TextTransform transform,
		[Values] bool isSelected)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text")
		};

		if (isSelected)
		{
			sut.Select(0, 4);
		}

		// Act
		bool canExecute = sut.TransformCommand.CanExecute(transform);

		// Assert
		canExecute
			.Should()
			.Be(isSelected);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.TransformCommand" />: the leading spaces and tabs go from every line
	/// the selection touches.
	/// </summary>
	[AvaloniaTest]
	public void TransformCommand_Removes_Leading_Whitespace_Of_The_Selected_Lines()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("  one\n\ttwo\n  three")
		};

		// From the start of the first line into the second one.
		sut.Select(0, 8);

		// Act
		sut.TransformCommand.Execute(TextTransform.RemoveLeadingWhitespace);

		// Assert
		sut.Text
			.Should()
			.Be("one\ntwo\n  three");
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.TransformCommand" />: Ctrl+U makes the selection lower case and Ctrl+Shift+U
	/// upper case, with ⌘ for Ctrl on macOS.
	/// </summary>
	[AvaloniaTest]
	public void TransformCommand_Runs_On_The_Case_Keys([Values] bool isShifted)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("one tWo three")
		};

		Window window = Show(sut);

		sut.Select(4, 3);

		sut.TextArea.Focus();

		RawInputModifiers modifiers = OperatingSystem.IsMacOS()
			? RawInputModifiers.Meta
			: RawInputModifiers.Control;

		if (isShifted)
		{
			modifiers |= RawInputModifiers.Shift;
		}

		// Act
		window.KeyPressQwerty(PhysicalKey.U, modifiers);

		window.KeyReleaseQwerty(PhysicalKey.U, modifiers);

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.Text
			.Should()
			.Be(isShifted ? "one TWO three" : "one two three");
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.TransformCommand" />: without a selection the transformations of whole lines
	/// take every line, and the indentation ones leave the rest of the line alone.
	/// </summary>
	[AvaloniaTest]
	[TestCase(TextTransform.RemoveTrailingWhitespace, "one  \ntwo\t\nthree", "one\ntwo\nthree")]
	[TestCase(TextTransform.LeadingTabsToSpaces, "\tone\n\t\ttwo\tend", "    one\n        two\tend")]
	[TestCase(TextTransform.LeadingSpacesToTabs, "    one\n        two    end", "\tone\n\t\ttwo    end")]
	public void TransformCommand_Takes_The_Whole_Document_Without_A_Selection(
		TextTransform transform,
		string text,
		string expected)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new(text)
		};

		// Act
		sut.TransformCommand.Execute(transform);

		// Assert
		sut.Text
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.UndoCommand" />: an edit cannot be undone in read-only mode.
	/// </summary>
	[AvaloniaTest]
	public void UndoCommand_Is_Denied_In_Read_Only_Mode([Values] bool isReadOnly)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text")
		};

		sut.Document.Insert(0, "New ");

		sut.IsReadOnly = isReadOnly;

		// Act
		bool canExecute = sut.UndoCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(!isReadOnly);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.UndoCommand" />: executed in read-only mode anyway, it leaves the text as it is.
	/// </summary>
	[AvaloniaTest]
	public void UndoCommand_Keeps_The_Text_In_Read_Only_Mode([Values] bool isReadOnly)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text")
		};

		sut.Document.Insert(0, "New ");

		sut.IsReadOnly = isReadOnly;

		// Act
		sut.UndoCommand.Execute(null);

		// Assert
		sut.Text
			.Should()
			.Be(isReadOnly ? "New Some text" : "Some text");
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.UndoCommand" />: there is something to undo only after an edit.
	/// </summary>
	[AvaloniaTest]
	public void UndoCommand_Needs_An_Edit([Values] bool isEdited)
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("Some text")
		};

		if (isEdited)
		{
			sut.Document.Insert(0, "New ");
		}

		// Act
		bool canExecute = sut.UndoCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(isEdited);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.UpdateLineEnding" />: a line ending of another style brought in by an edit
	/// makes the endings mixed.
	/// </summary>
	[AvaloniaTest]
	public void UpdateLineEnding_Follows_An_Edit()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new("First\r\nSecond")
		};

		sut.Document.Insert(sut.Document.TextLength, "\nThird");

		// Act
		sut.UpdateLineEnding();

		// Assert
		sut.Status.LineEnding
			.Should()
			.Be(LineEnding.Mixed);
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
