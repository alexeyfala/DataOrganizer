using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Styling;
using DataOrganizer.Controls;
using DataOrganizer.Dto;
using DataOrganizer.Enums;
using System;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentTextEditor)}"" type")]
internal class DocumentTextEditorTests
{
	#region Methods
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
				Column = 1,
				Line = 1,
				LineCount = 2,
				LineEnding = LineEnding.Lf,
				SelectionLength = 0
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
