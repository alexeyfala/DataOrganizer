using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.TextMate;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Behaviors.Styling;
using DataOrganizer.Controls;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using Shared.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentTextEditor)}"" type")]
internal class DocumentTextEditorTests
{
	#region Data
	/// <summary>
	/// A block of PowerShell whose braces stand on lines of their own, which folds from its first line to its last.
	/// </summary>
	private const string FoldedText = "if ($value)\n{\n    Write-Host 'Text'\n}";

	/// <summary>
	/// Two blocks of PowerShell, one inside the other, which fold from line 1 to line 7 and from line 3 to line 6.
	/// </summary>
	private const string NestedText = "if ($a)\n{\n    if ($b)\n    {\n        Write-Host 'Text'\n    }\n}";

	/// <summary>
	/// Language of <see cref="PowerShellText" />.
	/// </summary>
	private const string PowerShellLanguage = "powershell";

	/// <summary>
	/// A line of PowerShell that starts with a keyword, followed by words of other kinds.
	/// </summary>
	private const string PowerShellText = "if ($value) { Write-Host 'Text' }";

	/// <summary>
	/// Modifier of the keys of the commands: ⌘ on macOS and Ctrl elsewhere.
	/// </summary>
	private static readonly RawInputModifiers CommandModifiers = OperatingSystem.IsMacOS()
		? RawInputModifiers.Meta
		: RawInputModifiers.Control;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="StyledElement.ActualThemeVariant" />: the words take the colors of the new theme of the application.
	/// </summary>
	[AvaloniaTest]
	public async Task ActualThemeVariant_Changes_The_Colors_Of_The_Words()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(PowerShellText),
			SyntaxLanguage = PowerShellLanguage
		};

		Window window = Show(sut);

		window.RequestedThemeVariant = ThemeVariant.Light;

		await WaitForColors(sut, static x => x.Distinct().Count() > 1);

		Color lightColor = GetWordColors(sut)[0];

		// Act
		window.RequestedThemeVariant = ThemeVariant.Dark;

		// Assert
		bool isRecolored = await WaitForColors(sut, x => x.Length > 1 && x[0] != lightColor);

		isRecolored
			.Should()
			.BeTrue();
	}

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
	/// <see cref="DocumentTextEditor.Bookmarks" />: a bookmark set in one editor of a document shows in another one.
	/// </summary>
	[AvaloniaTest]
	public void Bookmarks_Are_Shared_By_The_Editors_Of_A_Document()
	{
		// Arrange
		TextDocument document = new("One\nTwo\nThree");

		DocumentTextEditor other = new()
		{
			Document = document
		};

		DocumentTextEditor sut = new()
		{
			Document = document
		};

		// Act
		other.Bookmarks.Toggle(2);

		// Assert
		sut.Bookmarks.GetLines()
			.Should()
			.Equal(2);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.CanFold" />: the text folds while it has a language with a grammar.
	/// </summary>
	[AvaloniaTest]
	public void CanFold_Follows_The_Language([Values] bool hasLanguage)
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = hasLanguage ? null : PowerShellLanguage
		};

		// Act
		sut.SyntaxLanguage = hasLanguage ? PowerShellLanguage : null;

		// Assert
		sut.CanFold
			.Should()
			.Be(hasLanguage);
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
	/// <see cref="DocumentTextEditor.Dispose" />: a disposed editor gets no highlighting for a language set later.
	/// </summary>
	[AvaloniaTest]
	public void Dispose_Keeps_The_Text_Plain_Afterwards()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new(PowerShellText)
		};

		sut.Dispose();

		// Act
		sut.SyntaxLanguage = PowerShellLanguage;

		// Assert
		HasHighlighting(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.Dispose" />: the folding goes away with the highlighting.
	/// </summary>
	[AvaloniaTest]
	public void Dispose_Removes_The_Folding()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		sut.Dispose();

		// Assert
		HasFolding(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.Dispose" />: the highlighting goes away with its tokenizer.
	/// </summary>
	[AvaloniaTest]
	public void Dispose_Removes_The_Highlighting()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new(PowerShellText),
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		sut.Dispose();

		// Assert
		HasHighlighting(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="TextEditor.Document" />: a document that comes back brings its bookmarks with it.
	/// </summary>
	[AvaloniaTest]
	public void Document_Brings_Its_Bookmarks_Back()
	{
		// Arrange
		TextDocument document = new("One\nTwo\nThree");

		DocumentTextEditor sut = new()
		{
			Document = document
		};

		sut.Bookmarks.Toggle(2);

		sut.Document = new("Four\nFive\nSix");

		// Act
		sut.Document = document;

		// Assert
		sut.Bookmarks.GetLines()
			.Should()
			.Equal(2);
	}

	/// <summary>
	/// <see cref="TextEditor.Document" />: the highlighting moves on to a new document.
	/// </summary>
	[AvaloniaTest]
	public async Task Document_Keeps_The_Highlighting_On_A_New_Document()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new("First"),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		// Act
		sut.Document = new(PowerShellText);

		// Assert
		bool isColored = await WaitForColors(sut, static x => x.Distinct().Count() > 1);

		isColored
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="TextEditor.Document" />: without a document the folding goes away.
	/// </summary>
	[AvaloniaTest]
	public void Document_Missing_Removes_The_Folding()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		sut.Document = null;

		// Assert
		HasFolding(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="TextEditor.Document" />: without a document the highlighting goes away.
	/// </summary>
	[AvaloniaTest]
	public void Document_Missing_Removes_The_Highlighting()
	{
		// Arrange
		DocumentTextEditor sut = new()
		{
			Document = new(PowerShellText),
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		sut.Document = null;

		// Assert
		HasHighlighting(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="TextEditor.Document" />: the folding moves on to a new document and finds its blocks.
	/// </summary>
	[AvaloniaTest]
	public void Document_Moves_The_Folding_To_A_New_Document()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new("First"),
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		sut.Document = new(FoldedText);

		// Assert
		GetFoldedLines(sut)
			.Should()
			.Equal((1, 4));
	}

	/// <summary>
	/// <see cref="TextEditor.Document" />: the editor shows the bookmarks of the new document, not those of the old one.
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
	/// <see cref="DocumentTextEditor.FoldAllCommand" />: the caret that leaves a long block for its first line comes into
	/// view.
	/// </summary>
	[AvaloniaTest]
	public void FoldAllCommand_Brings_The_Caret_Into_View()
	{
		// Arrange
		// A block from line 100 to line 300 among lines without indentation.
		string text = string.Join('\n', Enumerable
			.Range(1, 600)
			.Select(static x => x is > 100 and <= 300 ? $"    Line {x:D4}" : $"Line {x:D4}"));

		using DocumentTextEditor sut = new()
		{
			Document = new(text),
			SyntaxLanguage = PowerShellLanguage
		};

		Show(sut);

		sut.CaretOffset = sut.Document.GetLineByNumber(250).Offset;

		sut.ScrollToLine(250);

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.FoldAllCommand.Execute(null);

		Dispatcher.UIThread.RunJobs();

		// Assert
		TextView textView = sut.TextArea.TextView;

		textView.GetVisualTopByDocumentLine(100)
			.Should()
			.BeInRange(
				textView.VerticalOffset,
				textView.VerticalOffset + sut.ViewportHeight - textView.DefaultLineHeight);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.FoldAllCommand" />: a Ctrl+M that a key binding takes starts no chord, so the Ctrl+L
	/// after it folds nothing.
	/// </summary>
	[AvaloniaTest]
	public void FoldAllCommand_Does_Not_Run_On_Ctrl_L_After_A_Ctrl_M_Of_A_Binding()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		sut.KeyBindings.Add(new KeyBinding
		{
			Command = new RelayCommand(static () => { }),
			Gesture = new KeyGesture(Key.M, OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control)
		});

		Window window = Show(sut);

		sut.TextArea.Focus();

		Press(window, PhysicalKey.M, CommandModifiers);

		// Act
		Press(window, PhysicalKey.L, CommandModifiers);

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetFoldings(sut).Select(static x => x.IsFolded)
			.Should()
			.Equal(false);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.FoldAllCommand" />: a key that a key binding takes ends a chord as well, so the Ctrl+L
	/// after it folds nothing.
	/// </summary>
	[AvaloniaTest]
	public void FoldAllCommand_Does_Not_Run_On_Ctrl_L_After_A_Key_Of_A_Binding()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		Window window = Show(sut);

		sut.TextArea.Focus();

		Press(window, PhysicalKey.M, CommandModifiers);

		// The key of the bookmarks.
		Press(window, PhysicalKey.F2, CommandModifiers);

		// Act
		Press(window, PhysicalKey.L, CommandModifiers);

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetFoldings(sut).Select(static x => x.IsFolded)
			.Should()
			.Equal(false);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.FoldAllCommand" />: a key other than the second key of a chord ends the chord, so the
	/// Ctrl+L after it folds nothing.
	/// </summary>
	[AvaloniaTest]
	public void FoldAllCommand_Does_Not_Run_On_Ctrl_L_After_Another_Key()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		Window window = Show(sut);

		sut.TextArea.Focus();

		Press(window, PhysicalKey.M, CommandModifiers);

		Press(window, PhysicalKey.A, RawInputModifiers.None);

		// Act
		Press(window, PhysicalKey.L, CommandModifiers);

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetFoldings(sut).Select(static x => x.IsFolded)
			.Should()
			.Equal(false);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.FoldAllCommand" />: a chord ends when the focus leaves the editor, so the Ctrl+L on
	/// its return folds nothing.
	/// </summary>
	[AvaloniaTest]
	public void FoldAllCommand_Does_Not_Run_On_Ctrl_L_After_The_Focus_Leaves()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		Window window = Show(sut);

		sut.TextArea.Focus();

		Press(window, PhysicalKey.M, CommandModifiers);

		window.FocusManager!.Focus(null, NavigationMethod.Unspecified, KeyModifiers.None);

		sut.TextArea.Focus();

		// Act
		Press(window, PhysicalKey.L, CommandModifiers);

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetFoldings(sut).Select(static x => x.IsFolded)
			.Should()
			.Equal(false);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.FoldAllCommand" />: every block folds, the inner ones too.
	/// </summary>
	[AvaloniaTest]
	public void FoldAllCommand_Folds_Every_Block()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(NestedText),
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		sut.FoldAllCommand.Execute(null);

		// Assert
		GetFoldings(sut).Select(static x => x.IsFolded)
			.Should()
			.Equal(true, true);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.FoldAllCommand" />: the caret inside the blocks goes to the end of the first line of the
	/// outermost one, which stays in view, and the selection goes away.
	/// </summary>
	[AvaloniaTest]
	public void FoldAllCommand_Moves_The_Caret_Out_Of_The_Folded_Text()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(NestedText),
			SyntaxLanguage = PowerShellLanguage
		};

		sut.Select(sut.Document.GetLineByNumber(5).Offset, 3);

		// Act
		sut.FoldAllCommand.Execute(null);

		// Assert
		sut.CaretOffset
			.Should()
			.Be(sut.Document.GetLineByNumber(1).EndOffset);

		sut.SelectionLength
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.FoldAllCommand" />: there is something to fold only with an unfolded block.
	/// </summary>
	[AvaloniaTest]
	public void FoldAllCommand_Needs_An_Unfolded_Block([Values] bool hasUnfoldedBlock)
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		if (!hasUnfoldedBlock)
		{
			GetFoldings(sut)
				.Single()
				.IsFolded = true;
		}

		// Act
		bool canExecute = sut.FoldAllCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(hasUnfoldedBlock);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.FoldAllCommand" />: the key of a modifier held through a chord repeats, and the chord
	/// waits through it for its second key.
	/// </summary>
	[AvaloniaTest]
	public void FoldAllCommand_Runs_On_Ctrl_M_Ctrl_L_Through_A_Repeated_Ctrl()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		Window window = Show(sut);

		sut.TextArea.Focus();

		Press(window, PhysicalKey.M, CommandModifiers);

		window.KeyPressQwerty(PhysicalKey.ControlLeft, CommandModifiers);

		// Act
		Press(window, PhysicalKey.L, CommandModifiers);

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetFoldings(sut).Select(static x => x.IsFolded)
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.FoldAllCommand" />: runs on Ctrl+M, Ctrl+L while no block is folded, as in Visual
	/// Studio, with ⌘ for Ctrl on macOS.
	/// </summary>
	[AvaloniaTest]
	public void FoldAllCommand_Runs_On_Ctrl_M_Ctrl_L_Without_A_Folded_Block()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(NestedText),
			SyntaxLanguage = PowerShellLanguage
		};

		Window window = Show(sut);

		sut.TextArea.Focus();

		Press(window, PhysicalKey.M, CommandModifiers);

		// Act
		Press(window, PhysicalKey.L, CommandModifiers);

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetFoldings(sut).Select(static x => x.IsFolded)
			.Should()
			.Equal(true, true);
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
	/// <see cref="Visual.IsVisible" />: a hidden editor gives up its folding and takes it again when shown.
	/// </summary>
	[AvaloniaTest]
	public void IsVisible_Keeps_The_Folding_Only_While_Shown([Values] bool isVisible)
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			IsVisible = !isVisible,
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		sut.IsVisible = isVisible;

		// Assert
		HasFolding(sut)
			.Should()
			.Be(isVisible);
	}

	/// <summary>
	/// <see cref="Visual.IsVisible" />: a hidden editor gives up its highlighting and takes it again when shown.
	/// </summary>
	[AvaloniaTest]
	public void IsVisible_Keeps_The_Highlighting_Only_While_Shown([Values] bool isVisible)
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(PowerShellText),
			IsVisible = !isVisible,
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		sut.IsVisible = isVisible;

		// Assert
		HasHighlighting(sut)
			.Should()
			.Be(isVisible);
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
	/// <see cref="DocumentTextEditor.SyntaxLanguage" />: the words of the language take colors of their kinds.
	/// </summary>
	[AvaloniaTest]
	public async Task SyntaxLanguage_Colors_The_Words()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(PowerShellText)
		};

		Show(sut);

		// Act
		sut.SyntaxLanguage = PowerShellLanguage;

		// Assert
		bool isColored = await WaitForColors(sut, static x => x.Distinct().Count() > 1);

		isColored
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.SyntaxLanguage" />: the blocks of the text fold by the rules of the language.
	/// </summary>
	[AvaloniaTest]
	public void SyntaxLanguage_Folds_The_Blocks_Of_The_Text()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText)
		};

		// Act
		sut.SyntaxLanguage = PowerShellLanguage;

		// Assert
		GetFoldedLines(sut)
			.Should()
			.Equal((1, 4));
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.SyntaxLanguage" />: a text too long for the highlighting stays plain.
	/// </summary>
	[AvaloniaTest]
	public void SyntaxLanguage_Leaves_A_Too_Long_Text_Plain()
	{
		// Arrange, Act
		// Past the longest text that gets the highlighting.
		DocumentTextEditor sut = new()
		{
			Document = new(new string('x', (5 * 1024 * 1024) + 1)),
			SyntaxLanguage = PowerShellLanguage
		};

		// Assert
		HasHighlighting(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.SyntaxLanguage" />: a text too long for the highlighting does not fold either.
	/// </summary>
	[AvaloniaTest]
	public void SyntaxLanguage_Leaves_A_Too_Long_Text_Unfolded()
	{
		// Arrange, Act
		// Past the longest text that gets the highlighting.
		DocumentTextEditor sut = new()
		{
			Document = new(new string('x', (5 * 1024 * 1024) + 1)),
			SyntaxLanguage = PowerShellLanguage
		};

		// Assert
		HasFolding(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.SyntaxLanguage" />: a shown editor of a dark theme colors the words for a dark background.
	/// </summary>
	[AvaloniaTest]
	public async Task SyntaxLanguage_Takes_The_Colors_Of_A_Dark_Theme()
	{
		// Arrange
		using DocumentTextEditor lightEditor = new()
		{
			Document = new(PowerShellText)
		};

		using DocumentTextEditor sut = new()
		{
			Document = new(PowerShellText)
		};

		Show(lightEditor).RequestedThemeVariant = ThemeVariant.Light;

		Show(sut).RequestedThemeVariant = ThemeVariant.Dark;

		lightEditor.SyntaxLanguage = PowerShellLanguage;

		await WaitForColors(lightEditor, static x => x.Distinct().Count() > 1);

		Color lightColor = GetWordColors(lightEditor)[0];

		// Act
		sut.SyntaxLanguage = PowerShellLanguage;

		// Assert
		bool isDark = await WaitForColors(sut, x => x.Distinct().Count() > 1 && x[0] != lightColor);

		isDark
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.SyntaxLanguage" />: the blocks of the text fold by the rules of the new language.
	/// </summary>
	[AvaloniaTest]
	public void SyntaxLanguage_Takes_The_Rules_Of_A_New_Language()
	{
		// Arrange
		// PowerShell folds the marked block, while JSON has no markers and folds by indentation alone.
		using DocumentTextEditor sut = new()
		{
			Document = new("#region A\nx\n    y\n#endregion"),
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		sut.SyntaxLanguage = "json";

		// Assert
		GetFoldedLines(sut)
			.Should()
			.Equal((2, 3));
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.SyntaxLanguage" />: without a language that has a grammar the text does not fold.
	/// </summary>
	[AvaloniaTest]
	[TestCase(null)]
	[TestCase("unknown")]
	public void SyntaxLanguage_Without_A_Grammar_Folds_Nothing(string? language)
	{
		// Arrange, Act
		DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = language
		};

		// Assert
		HasFolding(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.SyntaxLanguage" />: without a language that has a grammar the text stays plain.
	/// </summary>
	[AvaloniaTest]
	[TestCase(null)]
	[TestCase("unknown")]
	public void SyntaxLanguage_Without_A_Grammar_Leaves_The_Text_Plain(string? language)
	{
		// Arrange, Act
		DocumentTextEditor sut = new()
		{
			Document = new(PowerShellText),
			SyntaxLanguage = language
		};

		// Assert
		HasHighlighting(sut)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="TextEditor.TextChanged" />: a pause after an edit brings the blocks that fold up to date.
	/// </summary>
	[AvaloniaTest]
	public async Task TextChanged_Finds_The_Blocks_After_A_Pause()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new("First\nSecond"),
			SyntaxLanguage = PowerShellLanguage
		};

		Func<bool> isFolded = () =>
		{
			// The pause ends on a timer, which posts the pass over the lines to the UI thread.
			Dispatcher.UIThread.RunJobs();

			return GetFoldedLines(sut).Length > 0;
		};

		// Act
		sut.Document.Insert(sut.Document.TextLength, "\n    Third");

		// Assert
		bool result = await isFolded.WaitAsync(millisecondsDelay: 10, maxRepeats: 1000);

		result
			.Should()
			.BeTrue();
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
	/// <see cref="DocumentTextEditor.ToggleFoldingCommand" />: a chord starts with Ctrl only, as an M without it types into
	/// the text.
	/// </summary>
	[AvaloniaTest]
	public void ToggleFoldingCommand_Does_Not_Run_On_M_M()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		Window window = Show(sut);

		sut.TextArea.Focus();

		Press(window, PhysicalKey.M, RawInputModifiers.None);

		// Act
		Press(window, PhysicalKey.M, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetFoldings(sut).Select(static x => x.IsFolded)
			.Should()
			.Equal(false);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ToggleFoldingCommand" />: a key other than the second key of a chord is left to the
	/// text as usual.
	/// </summary>
	[AvaloniaTest]
	public void ToggleFoldingCommand_Leaves_Another_Key_After_Ctrl_M_Alone()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		Window window = Show(sut);

		sut.TextArea.Focus();

		Press(window, PhysicalKey.M, CommandModifiers);

		KeyEventArgs args = new()
		{
			Key = Key.A,
			RoutedEvent = InputElement.KeyDownEvent,
			Source = sut.TextArea
		};

		// Act
		sut.TextArea.RaiseEvent(args);

		// Assert
		args.Handled
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ToggleFoldingCommand" />: the caret inside a block that folds goes to the end of the
	/// first line of the block, which stays in view.
	/// </summary>
	[AvaloniaTest]
	public void ToggleFoldingCommand_Moves_The_Caret_Out_Of_A_Block_It_Folds()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		sut.CaretOffset = sut.Document.GetLineByNumber(3).Offset;

		// Act
		sut.ToggleFoldingCommand.Execute(null);

		// Assert
		sut.CaretOffset
			.Should()
			.Be(sut.Document.GetLineByNumber(1).EndOffset);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ToggleFoldingCommand" />: there is something to fold only with a block at the caret line.
	/// </summary>
	[AvaloniaTest]
	public void ToggleFoldingCommand_Needs_A_Block_At_The_Caret([Values] bool hasBlock)
	{
		// Arrange
		// The block ends before the last line.
		using DocumentTextEditor sut = new()
		{
			Document = new($"{FoldedText}\nWrite-Host 'End'"),
			SyntaxLanguage = PowerShellLanguage
		};

		sut.CaretOffset = sut.Document.GetLineByNumber(hasBlock ? 1 : 5).Offset;

		// Act
		bool canExecute = sut.ToggleFoldingCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(hasBlock);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ToggleFoldingCommand" />: runs on Ctrl+M, Ctrl+M, as in Visual Studio, with or without
	/// Ctrl on the second key and with ⌘ for Ctrl on macOS.
	/// </summary>
	[AvaloniaTest]
	public void ToggleFoldingCommand_Runs_On_Ctrl_M_Ctrl_M([Values] bool isCtrlHeld)
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		Window window = Show(sut);

		sut.TextArea.Focus();

		Press(window, PhysicalKey.M, CommandModifiers);

		// Act
		Press(window, PhysicalKey.M, isCtrlHeld ? CommandModifiers : RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetFoldings(sut).Select(static x => x.IsFolded)
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.ToggleFoldingCommand" />: folds the block of the caret line when it is unfolded and
	/// unfolds it when it is folded.
	/// </summary>
	[AvaloniaTest]
	public void ToggleFoldingCommand_Toggles_The_Block_Of_The_Caret_Line([Values] bool isFolded)
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		if (isFolded)
		{
			GetFoldings(sut)
				.Single()
				.IsFolded = true;
		}

		// Act
		sut.ToggleFoldingCommand.Execute(null);

		// Assert
		GetFoldings(sut).Select(static x => x.IsFolded)
			.Should()
			.Equal(!isFolded);
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
	/// <see cref="DocumentTextEditor.UnfoldAllCommand" />: there is something to unfold only with a folded block.
	/// </summary>
	[AvaloniaTest]
	public void UnfoldAllCommand_Needs_A_Folded_Block([Values] bool hasFoldedBlock)
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(FoldedText),
			SyntaxLanguage = PowerShellLanguage
		};

		if (hasFoldedBlock)
		{
			GetFoldings(sut)
				.Single()
				.IsFolded = true;
		}

		// Act
		bool canExecute = sut.UnfoldAllCommand.CanExecute(null);

		// Assert
		canExecute
			.Should()
			.Be(hasFoldedBlock);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.UnfoldAllCommand" />: runs on Ctrl+M, Ctrl+L while a block is folded, even when another
	/// one is not, as in Visual Studio.
	/// </summary>
	[AvaloniaTest]
	public void UnfoldAllCommand_Runs_On_Ctrl_M_Ctrl_L_With_A_Folded_Block()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(NestedText),
			SyntaxLanguage = PowerShellLanguage
		};

		Window window = Show(sut);

		GetFoldings(sut)[1].IsFolded = true;

		sut.TextArea.Focus();

		Press(window, PhysicalKey.M, CommandModifiers);

		// Act
		Press(window, PhysicalKey.L, CommandModifiers);

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetFoldings(sut).Select(static x => x.IsFolded)
			.Should()
			.Equal(false, false);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.UnfoldAllCommand" />: every block unfolds, the inner ones too.
	/// </summary>
	[AvaloniaTest]
	public void UnfoldAllCommand_Unfolds_Every_Block()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new(NestedText),
			SyntaxLanguage = PowerShellLanguage
		};

		FoldingSection[] blocks = GetFoldings(sut);

		blocks[0].IsFolded = true;

		blocks[1].IsFolded = true;

		// Act
		sut.UnfoldAllCommand.Execute(null);

		// Assert
		GetFoldings(sut).Select(static x => x.IsFolded)
			.Should()
			.Equal(false, false);
	}

	/// <summary>
	/// <see cref="DocumentTextEditor.UpdateFoldings" />: a block that an edit makes folds.
	/// </summary>
	[AvaloniaTest]
	public void UpdateFoldings_Follows_An_Edit()
	{
		// Arrange
		using DocumentTextEditor sut = new()
		{
			Document = new("First\nSecond"),
			SyntaxLanguage = PowerShellLanguage
		};

		sut.Document.Insert(sut.Document.TextLength, "\n    Third");

		// Act
		sut.UpdateFoldings();

		// Assert
		GetFoldedLines(sut)
			.Should()
			.Equal((2, 3));
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
	/// Returns the numbers of the first and the last lines of the blocks that fold.
	/// </summary>
	private static (int Start, int End)[] GetFoldedLines(TextEditor editor)
	{
		TextDocument document = editor.Document;

		return [.. GetFoldings(editor).Select(x => (
			document.GetLineByOffset(x.StartOffset).LineNumber,
			document.GetLineByOffset(x.EndOffset).LineNumber))];
	}

	/// <summary>
	/// Returns the blocks that fold; none without the folding.
	/// </summary>
	private static FoldingSection[] GetFoldings(TextEditor editor)
	{
		if (editor
			.TextArea
			.LeftMargins
			.OfType<FoldingMargin>()
			.SingleOrDefault() is not { } margin)
		{
			return [];
		}

		return [.. margin.FoldingManager.AllFoldings];
	}

	/// <summary>
	/// Returns the colors of the parts the first line of the view is drawn in.
	/// </summary>
	private static Color[] GetWordColors(TextEditor editor)
	{
		TextView view = editor.TextArea.TextView;

		if (!view.VisualLinesValid || view.VisualLines.Count == 0)
		{
			return [];
		}

		return [.. view
			.VisualLines[0]
			.Elements
			.Select(static x => x.TextRunProperties.ForegroundBrush)
			.OfType<ISolidColorBrush>()
			.Select(static x => x.Color)];
	}

	/// <summary>
	/// <c>True</c> when the editor has the folding.
	/// </summary>
	private static bool HasFolding(TextEditor editor)
	{
		return editor
			.TextArea
			.LeftMargins
			.OfType<FoldingMargin>()
			.Any();
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
	/// Presses and releases a key.
	/// </summary>
	private static void Press(Window window, PhysicalKey key, RawInputModifiers modifiers)
	{
		window.KeyPressQwerty(key, modifiers);

		window.KeyReleaseQwerty(key, modifiers);
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

	/// <summary>
	/// Waits until the colors of the first line meet a condition, as the tokenizer colors the words on a thread of its own.
	/// </summary>
	private static ValueTask<bool> WaitForColors(TextEditor editor, Func<Color[], bool> condition)
	{
		Func<bool> isMet = () =>
		{
			// The tokenizer posts the redraws of the colored lines to the UI thread.
			Dispatcher.UIThread.RunJobs();

			return condition(GetWordColors(editor));
		};

		return isMet.WaitAsync(millisecondsDelay: 10, maxRepeats: 1000);
	}
	#endregion
}
