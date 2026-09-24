using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Styling;
using DataOrganizer.Controls;
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
