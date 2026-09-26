using Avalonia;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Text;
using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;

namespace DataOrganizer.Controls;

/// <summary>
/// <see cref="TextEditorBase" /> for documents.
/// </summary>
internal sealed class DocumentTextEditor : TextEditorBase
{
	#region Properties
	/// <summary>
	/// Bookmarks of the lines of the document.
	/// </summary>
	public LineBookmarks Bookmarks { get; }

	/// <summary>
	/// <c>True</c> when line endings are shown.
	/// </summary>
	public bool ShowEndOfLine
	{
		get => GetValue(ShowEndOfLineProperty);
		set => SetValue(ShowEndOfLineProperty, value);
	}

	/// <summary>
	/// <c>True</c> when spaces are shown.
	/// </summary>
	public bool ShowSpaces
	{
		get => GetValue(ShowSpacesProperty);
		set => SetValue(ShowSpacesProperty, value);
	}

	/// <summary>
	/// <c>True</c> when tabs are shown.
	/// </summary>
	public bool ShowTabs
	{
		get => GetValue(ShowTabsProperty);
		set => SetValue(ShowTabsProperty, value);
	}

	/// <summary>
	/// Caret position, selection and lines of the document.
	/// </summary>
	public DocumentStatus Status
	{
		get => _status;
		private set => SetAndRaise(StatusProperty, ref _status, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="ShowEndOfLine" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowEndOfLineProperty = AvaloniaProperty
		.Register<DocumentTextEditor, bool>(name: nameof(ShowEndOfLine));

	/// <summary>
	/// Identifies the <see cref="ShowSpaces" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowSpacesProperty = AvaloniaProperty
		.Register<DocumentTextEditor, bool>(name: nameof(ShowSpaces));

	/// <summary>
	/// Identifies the <see cref="ShowTabs" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowTabsProperty = AvaloniaProperty
		.Register<DocumentTextEditor, bool>(name: nameof(ShowTabs));
	#endregion

	#region Direct Properties
	/// <summary>
	/// Identifies the <see cref="Status" /> avalonia property.
	/// </summary>
	public static readonly DirectProperty<DocumentTextEditor, DocumentStatus> StatusProperty = AvaloniaProperty
		.RegisterDirect<DocumentTextEditor, DocumentStatus>(
			name: nameof(Status),
			getter: static x => x.Status);
	#endregion

	#region Commands
	/// <summary>
	/// Removes all bookmarks.
	/// </summary>
	public RelayCommand ClearBookmarksCommand { get; }

	/// <summary>
	/// Brings every line break of the document to the given style.
	/// </summary>
	public RelayCommand<LineEnding> ConvertLineEndingsCommand { get; }

	/// <summary>
	/// Cuts the selected text.
	/// </summary>
	public RelayCommand CutCommand { get; }

	/// <summary>
	/// Moves the caret to the next bookmarked line, going round to the first one.
	/// </summary>
	public RelayCommand NextBookmarkCommand { get; }

	/// <summary>
	/// Pastes the text from the clipboard.
	/// </summary>
	public RelayCommand PasteCommand { get; }

	/// <summary>
	/// Moves the caret to the previous bookmarked line, going round to the last one.
	/// </summary>
	public RelayCommand PreviousBookmarkCommand { get; }

	/// <summary>
	/// Redoes the last undone edit.
	/// </summary>
	public RelayCommand RedoCommand { get; }

	/// <summary>
	/// Sets or removes the bookmark of the caret line.
	/// </summary>
	public RelayCommand ToggleBookmarkCommand { get; }

	/// <summary>
	/// Transforms the selected text, or the whole document where the transformation allows it.
	/// </summary>
	public RelayCommand<TextTransform> TransformCommand { get; }

	/// <summary>
	/// Undoes the last edit.
	/// </summary>
	public RelayCommand UndoCommand { get; }
	#endregion

	#region Data
	/// <inheritdoc cref="Status" />
	private DocumentStatus _status;
	#endregion

	#region Constructors
	public DocumentTextEditor()
	{
		ClearBookmarksCommand = new(ClearBookmarks, HasBookmarks);

		ConvertLineEndingsCommand = new(ConvertLineEndings, CanConvertLineEndings);

		CutCommand = new(CutSelection, CanCutSelection);

		NextBookmarkCommand = new(GoToNextBookmark, HasBookmarks);

		PasteCommand = new(PasteText, CanPasteText);

		PreviousBookmarkCommand = new(GoToPreviousBookmark, HasBookmarks);

		RedoCommand = new(RedoEdit, CanRedoEdit);

		ToggleBookmarkCommand = new(ToggleBookmark);

		TransformCommand = new(TransformText, CanTransformText);

		UndoCommand = new(UndoEdit, CanUndoEdit);

		// The keys of Visual Studio and Notepad++, with ⌘ for Ctrl on macOS like the keys of the engine.
		KeyModifiers commandModifier = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

		KeyBindings.Add(new KeyBinding
		{
			Command = TransformCommand,
			CommandParameter = TextTransform.LowerCase,
			Gesture = new KeyGesture(Key.U, commandModifier)
		});

		KeyBindings.Add(new KeyBinding
		{
			Command = TransformCommand,
			CommandParameter = TextTransform.UpperCase,
			Gesture = new KeyGesture(Key.U, commandModifier | KeyModifiers.Shift)
		});

		KeyBindings.Add(new KeyBinding
		{
			Command = ToggleBookmarkCommand,
			Gesture = new KeyGesture(Key.F2, commandModifier)
		});

		KeyBindings.Add(new KeyBinding
		{
			Command = NextBookmarkCommand,
			Gesture = new KeyGesture(Key.F2)
		});

		KeyBindings.Add(new KeyBinding
		{
			Command = PreviousBookmarkCommand,
			Gesture = new KeyGesture(Key.F2, KeyModifiers.Shift)
		});

		// The engine undoes and redoes in read-only mode too; its bindings serve both the keys and the commands.
		foreach (RoutedCommandBinding binding in TextArea
			.DefaultInputHandler
			.CommandBindings
			.Where(static x => x.Command == ApplicationCommands.Undo || x.Command == ApplicationCommands.Redo))
		{
			binding.CanExecute += UndoRedoBinding_CanExecute;
		}

		// The engine keeps these switches in its options, which markup cannot bind to.
		this
			.GetObservable(ShowEndOfLineProperty)
			.Subscribe(ShowEndOfLineProperty_Changed);

		this
			.GetObservable(ShowSpacesProperty)
			.Subscribe(ShowSpacesProperty_Changed);

		this
			.GetObservable(ShowTabsProperty)
			.Subscribe(ShowTabsProperty_Changed);

		Bookmarks = new()
		{
			Document = Document
		};

		// Left of the line numbers, as in Visual Studio, the bookmarks keep clear of clicks at the start of a line.
		TextArea.LeftMargins.Insert(0, new BookmarkMargin(Bookmarks));

		TextArea.Caret.PositionChanged += Caret_PositionChanged;

		TextArea.SelectionChanged += TextArea_SelectionChanged;

		DocumentChanged += DocumentTextEditor_DocumentChanged;

		TextChanged += DocumentTextEditor_TextChanged;

		// The line endings take a pass over all lines, so a run of edits is followed by a single pass.
		Observable.FromEventPattern<EventHandler, EventArgs>(
			x => TextChanged += x,
			x => TextChanged -= x)
			.SetDelay(TimeSpan.FromSeconds(0.5))
			.Subscribe(_ => UpdateLineEnding());

		// The handlers above follow the changes only, so the status starts from the current state.
		Status = new()
		{
			CaretOffset = TextArea.Caret.Offset,
			Column = TextArea.Caret.Column,
			Line = TextArea.Caret.Line,
			LineCount = LineCount,
			LineEnding = FindLineEnding(Document),
			SelectionLength = TextArea.Selection.Length,
			SelectionLineCount = CountSelectedLines(TextArea.Selection),
			TextLength = Document?.TextLength ?? 0
		};
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Caret.PositionChanged" /> event handler.
	/// </summary>
	private void Caret_PositionChanged(object? sender, EventArgs e)
	{
		Status = Status with
		{
			CaretOffset = TextArea.Caret.Offset,
			Column = TextArea.Caret.Column,
			Line = TextArea.Caret.Line
		};
	}

	/// <summary>
	/// <see cref="TextEditor.DocumentChanged" /> event handler.
	/// </summary>
	private void DocumentTextEditor_DocumentChanged(object? sender, DocumentChangedEventArgs e)
	{
		// The bookmarks belong to the lines of one document.
		Bookmarks.Document = Document;

		UpdateLineEnding();
	}

	/// <summary>
	/// <see cref="TextEditor.TextChanged" /> event handler.
	/// </summary>
	private void DocumentTextEditor_TextChanged(object? sender, EventArgs e)
	{
		// An edit before the caret moves its offset while its line and column stay, and then the caret reports nothing.
		Status = Status with
		{
			CaretOffset = TextArea.Caret.Offset,
			LineCount = LineCount,
			TextLength = Document?.TextLength ?? 0
		};
	}

	/// <summary>
	/// <see cref="ShowEndOfLineProperty" /> changed handler.
	/// </summary>
	private void ShowEndOfLineProperty_Changed(bool value) => Options.ShowEndOfLine = value;

	/// <summary>
	/// <see cref="ShowSpacesProperty" /> changed handler.
	/// </summary>
	private void ShowSpacesProperty_Changed(bool value) => Options.ShowSpaces = value;

	/// <summary>
	/// <see cref="ShowTabsProperty" /> changed handler.
	/// </summary>
	private void ShowTabsProperty_Changed(bool value) => Options.ShowTabs = value;

	/// <summary>
	/// <see cref="TextArea.SelectionChanged" /> event handler.
	/// </summary>
	private void TextArea_SelectionChanged(object? sender, EventArgs e)
	{
		// SelectionLength of the editor would also count the gaps of a rectangular selection.
		Status = Status with
		{
			SelectionLength = TextArea.Selection.Length,
			SelectionLineCount = CountSelectedLines(TextArea.Selection)
		};
	}

	/// <summary>
	/// <see cref="RoutedCommandBinding.CanExecute" /> handler of undo and redo, which denies them in read-only mode.
	/// </summary>
	private void UndoRedoBinding_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
	{
		if (!IsReadOnly)
		{
			return;
		}

		e.CanExecute = false;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Finds the line endings of the document with a pass over all its lines.
	/// </summary>
	internal void UpdateLineEnding()
	{
		Status = Status with { LineEnding = FindLineEnding(Document) };
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the number of lines a selection touches.
	/// </summary>
	private static int CountSelectedLines(Selection selection)
	{
		if (selection.IsEmpty)
		{
			return 0;
		}

		return Math.Abs(selection.EndPosition.Line - selection.StartPosition.Line) + 1;
	}

	/// <summary>
	/// Returns the line break style shared by the lines of a document.
	/// </summary>
	private static LineEnding FindLineEnding(TextDocument? document)
	{
		if (document is null)
		{
			return LineEnding.None;
		}

		LineEnding found = LineEnding.None;

		foreach (DocumentLine line in document.Lines)
		{
			LineEnding ending = line.DelimiterLength switch
			{
				0 => LineEnding.None,
				2 => LineEnding.CrLf,
				_ => document.GetCharAt(line.EndOffset) == '\n' ? LineEnding.Lf : LineEnding.Cr
			};

			// Only the last line has no line break.
			if (ending == LineEnding.None || ending == found)
			{
				continue;
			}

			if (found != LineEnding.None)
			{
				return LineEnding.Mixed;
			}

			found = ending;
		}

		return found;
	}

	/// <summary>
	/// Returns the command of the engine that performs a transformation.
	/// </summary>
	private static RoutedCommand GetEngineCommand(TextTransform transform) => transform switch
	{
		TextTransform.InvertCase => AvaloniaEditCommands.InvertCase,
		TextTransform.LeadingSpacesToTabs => AvaloniaEditCommands.ConvertLeadingSpacesToTabs,
		TextTransform.LeadingTabsToSpaces => AvaloniaEditCommands.ConvertLeadingTabsToSpaces,
		TextTransform.LowerCase => AvaloniaEditCommands.ConvertToLowercase,
		TextTransform.RemoveLeadingWhitespace => AvaloniaEditCommands.RemoveLeadingWhitespace,
		TextTransform.RemoveTrailingWhitespace => AvaloniaEditCommands.RemoveTrailingWhitespace,
		TextTransform.UpperCase => AvaloniaEditCommands.ConvertToUppercase,
		_ => throw new NotImplementedException()
	};

	/// <summary>
	/// Returns the characters of a line break style.
	/// </summary>
	private static string GetNewLine(LineEnding lineEnding) => lineEnding switch
	{
		LineEnding.Cr => "\r",
		LineEnding.CrLf => "\r\n",
		LineEnding.Lf => "\n",
		_ => throw new NotImplementedException()
	};

	/// <summary>
	/// Validates <see cref="ConvertLineEndingsCommand" />.
	/// </summary>
	private bool CanConvertLineEndings(LineEnding lineEnding)
	{
		// A pass of its own, as the status catches up with an edit only after a delay.
		LineEnding current = FindLineEnding(Document);

		return !IsReadOnly && current != LineEnding.None && current != lineEnding;
	}

	/// <summary>
	/// Validates <see cref="CutCommand" />.
	/// </summary>
	private bool CanCutSelection()
	{
		// Without a selection the engine would cut the whole line of the caret.
		return CanCut && TextArea.Selection.Length > 0;
	}

	/// <summary>
	/// Validates <see cref="PasteCommand" />.
	/// </summary>
	private bool CanPasteText() => CanPaste;

	/// <summary>
	/// Validates <see cref="RedoCommand" />.
	/// </summary>
	private bool CanRedoEdit() => CanRedo;

	/// <summary>
	/// Validates <see cref="TransformCommand" />.
	/// </summary>
	private bool CanTransformText(TextTransform transform)
	{
		// The line transformations of the engine change the text in read-only mode too.
		if (IsReadOnly || Document is not { TextLength: > 0 })
		{
			return false;
		}

		// Without a selection the engine takes the whole document, which a stray click must not rewrite.
		return transform switch
		{
			TextTransform.LeadingSpacesToTabs
				or TextTransform.LeadingTabsToSpaces
				or TextTransform.RemoveTrailingWhitespace => true,
			_ => TextArea.Selection.Length > 0
		};
	}

	/// <summary>
	/// Validates <see cref="UndoCommand" />.
	/// </summary>
	private bool CanUndoEdit() => CanUndo;

	/// <summary>
	/// Removes all bookmarks.
	/// </summary>
	private void ClearBookmarks() => Bookmarks.Clear();

	/// <summary>
	/// Brings every line break of the document to a style, as one step to undo.
	/// </summary>
	private void ConvertLineEndings(LineEnding lineEnding)
	{
		string newLine = GetNewLine(lineEnding);

		// Collected before the edits and replaced from the end: a carriage return next to the line feed
		// of the next line joins the two lines for a while, so line numbers would drift, but offsets hold.
		(int Offset, int Length)[] lineBreaks = [.. Document.Lines
			.Reverse()
			.Where(x => x.DelimiterLength > 0 && Document.GetText(x.EndOffset, x.DelimiterLength) != newLine)
			.Select(static x => (Offset: x.EndOffset, Length: x.DelimiterLength))];

		using (Document.RunUpdate())
		{
			foreach ((int offset, int length) in lineBreaks)
			{
				Document.Replace(offset, length, newLine);
			}
		}

		// The status would catch up only after the delay.
		UpdateLineEnding();
	}

	/// <summary>
	/// Converts the selected text to title case, as one step to undo.
	/// </summary>
	private void ConvertSelectionToTitleCase()
	{
		TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

		using (Document.RunUpdate())
		{
			foreach (SelectionSegment segment in TextArea.Selection.Segments.Reverse())
			{
				// A word in capitals is taken for an abbreviation and kept, so the text goes to lower case first.
				string text = textInfo.ToTitleCase(textInfo.ToLower(Document.GetText(segment)));

				Document.Replace(segment.StartOffset, segment.Length, text, OffsetChangeMappingType.CharacterReplace);
			}
		}
	}

	/// <summary>
	/// Executes <see cref="ApplicationCommands.Cut" /> on the text area.
	/// </summary>
	private void CutSelection()
	{
		ApplicationCommands
			.Cut
			.Execute(null, TextArea);
	}

	/// <summary>
	/// Moves the caret to the next bookmarked line, going round to the first one.
	/// </summary>
	private void GoToNextBookmark() => MoveCaretToLine(Bookmarks.FindNext(TextArea.Caret.Line));

	/// <summary>
	/// Moves the caret to the previous bookmarked line, going round to the last one.
	/// </summary>
	private void GoToPreviousBookmark() => MoveCaretToLine(Bookmarks.FindPrevious(TextArea.Caret.Line));

	/// <summary>
	/// Validates <see cref="ClearBookmarksCommand" />, <see cref="NextBookmarkCommand" /> and
	/// <see cref="PreviousBookmarkCommand" />.
	/// </summary>
	private bool HasBookmarks() => Bookmarks.GetLines().Length > 0;

	/// <summary>
	/// Puts the caret at the start of a line and brings the line into view.
	/// </summary>
	private void MoveCaretToLine(int? line)
	{
		if (line is not { } number)
		{
			return;
		}

		// A selection left behind would stretch over the jump.
		Select(Document.GetLineByNumber(number).Offset, 0);

		ScrollTo(number, 1);
	}

	/// <summary>
	/// Executes <see cref="ApplicationCommands.Paste" /> on the text area.
	/// </summary>
	private void PasteText()
	{
		ApplicationCommands
			.Paste
			.Execute(null, TextArea);
	}

	/// <summary>
	/// Executes <see cref="ApplicationCommands.Redo" /> on the text area.
	/// </summary>
	private void RedoEdit()
	{
		ApplicationCommands
			.Redo
			.Execute(null, TextArea);
	}

	/// <summary>
	/// Sets or removes the bookmark of the caret line.
	/// </summary>
	private void ToggleBookmark() => Bookmarks.Toggle(TextArea.Caret.Line);

	/// <summary>
	/// Applies a transformation to the selected text, or to the whole document where the transformation allows it.
	/// </summary>
	private void TransformText(TextTransform transform)
	{
		// The title case command of the engine throws NotSupportedException.
		if (transform == TextTransform.TitleCase)
		{
			ConvertSelectionToTitleCase();

			return;
		}

		GetEngineCommand(transform)
			.Execute(null, TextArea);
	}

	/// <summary>
	/// Executes <see cref="ApplicationCommands.Undo" /> on the text area.
	/// </summary>
	private void UndoEdit()
	{
		ApplicationCommands
			.Undo
			.Execute(null, TextArea);
	}
	#endregion
}
