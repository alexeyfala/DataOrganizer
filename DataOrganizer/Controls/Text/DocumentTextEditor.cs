using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.TextMate;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions.Documents;
using DataOrganizer.Extensions.Text;
using DataOrganizer.Extensions.Views;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Services.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using TextMateSharp.Themes;

namespace DataOrganizer.Controls.Text;

/// <summary>
/// <see cref="TextEditorBase" /> for documents.
/// </summary>
internal sealed class DocumentTextEditor : TextEditorBase, IDisposable
{
	#region Properties
	/// <summary>
	/// Bookmarks of the lines of the document.
	/// </summary>
	public LineBookmarks Bookmarks { get; private set; }

	/// <summary>
	/// <c>True</c> while the text folds by the rules of its language.
	/// </summary>
	public bool CanFold
	{
		get => _canFold;
		private set => SetAndRaise(CanFoldProperty, ref _canFold, value);
	}

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

	/// <summary>
	/// Language of the text for the syntax highlighting; <c>null</c> for plain text.
	/// </summary>
	public string? SyntaxLanguage
	{
		get => GetValue(SyntaxLanguageProperty);
		set => SetValue(SyntaxLanguageProperty, value);
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

	/// <summary>
	/// Identifies the <see cref="SyntaxLanguage" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> SyntaxLanguageProperty = AvaloniaProperty
		.Register<DocumentTextEditor, string?>(name: nameof(SyntaxLanguage));
	#endregion

	#region Direct Properties
	/// <summary>
	/// Identifies the <see cref="CanFold" /> avalonia property.
	/// </summary>
	public static readonly DirectProperty<DocumentTextEditor, bool> CanFoldProperty = AvaloniaProperty
		.RegisterDirect<DocumentTextEditor, bool>(
			name: nameof(CanFold),
			getter: static x => x.CanFold);

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
	public RelayCommand ClearBookmarksCommand => _bookmarkNavigation.ClearBookmarksCommand;

	/// <summary>
	/// Brings every line break of the document to the given style.
	/// </summary>
	public RelayCommand<LineEnding> ConvertLineEndingsCommand { get; }

	/// <summary>
	/// Cuts the selected text.
	/// </summary>
	public RelayCommand CutCommand { get; }

	/// <summary>
	/// Folds every block of the text.
	/// </summary>
	public RelayCommand FoldAllCommand { get; }

	/// <summary>
	/// Moves the caret to the next bookmarked line, going round to the first one.
	/// </summary>
	public RelayCommand NextBookmarkCommand => _bookmarkNavigation.NextBookmarkCommand;

	/// <summary>
	/// Pastes the text from the clipboard.
	/// </summary>
	public RelayCommand PasteCommand { get; }

	/// <summary>
	/// Moves the caret to the previous bookmarked line, going round to the last one.
	/// </summary>
	public RelayCommand PreviousBookmarkCommand => _bookmarkNavigation.PreviousBookmarkCommand;

	/// <summary>
	/// Redoes the last undone edit.
	/// </summary>
	public RelayCommand RedoCommand { get; }

	/// <summary>
	/// Sets or removes the bookmark of the caret line.
	/// </summary>
	public RelayCommand ToggleBookmarkCommand => _bookmarkNavigation.ToggleBookmarkCommand;

	/// <summary>
	/// Folds or unfolds the innermost block of the caret line.
	/// </summary>
	public RelayCommand ToggleFoldingCommand { get; }

	/// <summary>
	/// Transforms the selected text, or the whole document where the transformation allows it.
	/// </summary>
	public RelayCommand<TextTransform> TransformCommand { get; }

	/// <summary>
	/// Undoes the last edit.
	/// </summary>
	public RelayCommand UndoCommand { get; }

	/// <summary>
	/// Unfolds every block of the text.
	/// </summary>
	public RelayCommand UnfoldAllCommand { get; }
	#endregion

	#region Data
	/// <summary>
	/// The longest text that gets the syntax highlighting, as a longer one would take too much time and memory.
	/// </summary>
	private const int MaxHighlightedLength = 5 * 1024 * 1024;

	/// <summary>
	/// Modifier of the keys of the commands: ⌘ on macOS, like the keys of the engine, and Ctrl elsewhere.
	/// </summary>
	internal static readonly KeyModifiers CommandModifier = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

	/// <inheritdoc cref="DocumentBookmarkNavigation" />
	private readonly DocumentBookmarkNavigation _bookmarkNavigation;

	/// <inheritdoc cref="DocumentFoldingController" />
	private readonly DocumentFoldingController _folding;

	/// <inheritdoc cref="CanFold" />
	private bool _canFold;

	/// <summary>
	/// Syntax highlighting of the text; <c>null</c> while the text is plain.
	/// </summary>
	private TextMate.Installation? _highlighting;

	/// <summary>
	/// Scope name of the grammar of <see cref="_highlighting" />.
	/// </summary>
	private string? _highlightingScope;

	/// <summary>
	/// <c>True</c> once the editor has been disposed, after which the text stays plain.
	/// </summary>
	private bool _isDisposed;

	/// <summary>
	/// Bookmarked lines of a text replaced whole until the end of its change; <c>null</c> while no change replaces it.
	/// </summary>
	private int[]? _replacedTextBookmarks;

	/// <inheritdoc cref="Status" />
	private DocumentStatus _status;
	#endregion

	#region Constructors
	public DocumentTextEditor()
	{
		_folding = new(this);

		ConvertLineEndingsCommand = new(ConvertLineEndings, CanConvertLineEndings);

		CutCommand = new(CutSelection, CanCutSelection);

		FoldAllCommand = new(_folding.FoldAll, () => _folding.CanFoldAll);

		PasteCommand = new(PasteText, CanPasteText);

		RedoCommand = new(RedoEdit, CanRedoEdit);

		ToggleFoldingCommand = new(_folding.ToggleFolding, () => _folding.CanToggleFolding);

		TransformCommand = new(TransformText, CanTransformText);

		UndoCommand = new(UndoEdit, CanUndoEdit);

		UnfoldAllCommand = new(_folding.UnfoldAll, () => _folding.CanUnfoldAll);

		// The keys of Visual Studio and Notepad++.
		KeyBindings.Add(new KeyBinding
		{
			Command = TransformCommand,
			CommandParameter = TextTransform.LowerCase,
			Gesture = new KeyGesture(Key.U, CommandModifier)
		});

		KeyBindings.Add(new KeyBinding
		{
			Command = TransformCommand,
			CommandParameter = TextTransform.UpperCase,
			Gesture = new KeyGesture(Key.U, CommandModifier | KeyModifiers.Shift)
		});

		_bookmarkNavigation = new(this);

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

		// The highlighting and the folding need a language, and a hidden editor keeps neither, as nothing of them is seen.
		this
			.GetObservable(SyntaxLanguageProperty)
			.Subscribe(SyntaxLanguageProperty_Changed);

		this
			.GetObservable(IsVisibleProperty)
			.Subscribe(IsVisibleProperty_Changed);

		// Only the colors of the words follow the theme: the background and the plain text keep those of the application.
		this
			.GetObservable(ThemeVariantScope.ActualThemeVariantProperty)
			.Subscribe(ActualThemeVariantProperty_Changed);

		Bookmarks = GetBookmarks(Document);

		// Left of the line numbers, as in Visual Studio, the bookmarks keep clear of clicks at the start of a line.
		TextArea.LeftMargins.Insert(0, new BookmarkMargin(this));

		TextArea.Caret.PositionChanged += Caret_PositionChanged;

		TextArea.SelectionChanged += TextArea_SelectionChanged;

		DocumentChanged += DocumentTextEditor_DocumentChanged;

		// The base sets the first document before this constructor runs, so it is followed from here.
		if (Document is { } document)
		{
			TextDocumentWeakEventManager.Changing.AddHandler(document, Document_Changing);
		}

		TextChanged += DocumentTextEditor_TextChanged;

		// The line endings and the blocks take a pass over all lines, so a run of edits is followed by a single pass.
		Observable.FromEventPattern<EventHandler, EventArgs>(
			x => TextChanged += x,
			x => TextChanged -= x)
			.SetDelay(TimeSpan.FromSeconds(0.5))
			.Subscribe(_ =>
			{
				UpdateLineEnding();

				UpdateFoldings();
			});

		// The handlers above follow the changes only, so the status starts from the current state.
		Status = new()
		{
			CaretOffset = TextArea.Caret.Offset,
			Column = TextArea.Caret.Column,
			Line = TextArea.Caret.Line,
			LineCount = LineCount,
			LineEnding = Document.FindLineEnding(),
			SelectionLength = TextArea.Selection.Length,
			SelectionLineCount = CountSelectedLines(TextArea.Selection),
			TextLength = Document?.TextLength ?? 0
		};
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="ThemeVariantScope.ActualThemeVariantProperty" /> changed handler.
	/// </summary>
	private void ActualThemeVariantProperty_Changed(ThemeVariant value) => _highlighting?.SetTheme(GetColorTheme(value));

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
	/// <see cref="TextDocument.Changing" /> event handler.
	/// </summary>
	private void Document_Changing(object? sender, DocumentChangeEventArgs e)
	{
		// A text replaced whole, as a file loaded into the empty document or read again in another encoding, is a new text:
		// its line endings and blocks are found at the end of the change, while typing still waits for the pause, and its
		// bookmarks are taken while they still stand on their lines.
		if (sender is TextDocument document && e.RemovalLength == document.TextLength)
		{
			_replacedTextBookmarks = Bookmarks.GetLines();
		}
	}

	/// <summary>
	/// <see cref="TextEditor.DocumentChanged" /> event handler.
	/// </summary>
	private void DocumentTextEditor_DocumentChanged(object? sender, DocumentChangedEventArgs e)
	{
		// A document may outlive the view, so its changes are followed weakly.
		if (e.OldDocument is { } oldDocument)
		{
			TextDocumentWeakEventManager.Changing.RemoveHandler(oldDocument, Document_Changing);
		}

		if (e.NewDocument is { } newDocument)
		{
			TextDocumentWeakEventManager.Changing.AddHandler(newDocument, Document_Changing);
		}

		// The bookmarks belong to the lines of one document.
		Bookmarks = GetBookmarks(Document);

		UpdateLineEnding();

		// The highlighting and the folding of the old document are gone, and the new one gets its own unless it is missing
		// or too long.
		UpdateSyntax();
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

		// The folding moves its blocks with the change itself, so the blocks of a new text are found only after it.
		if (_replacedTextBookmarks is not { } bookmarkedLines)
		{
			return;
		}

		_replacedTextBookmarks = null;

		UpdateLineEnding();

		UpdateFoldings();

		// The change moves the bookmarks of a text replaced whole to its end, so they go back to their line numbers.
		if (bookmarkedLines.Length > 0)
		{
			Bookmarks.SetLines(bookmarkedLines);
		}
	}

	/// <summary>
	/// <see cref="Visual.IsVisibleProperty" /> changed handler.
	/// </summary>
	private void IsVisibleProperty_Changed(bool value) => UpdateSyntax();

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
	/// <see cref="SyntaxLanguageProperty" /> changed handler.
	/// </summary>
	private void SyntaxLanguageProperty_Changed(string? value) => UpdateSyntax();

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
	/// Removes the syntax highlighting, whose tokenizer holds a thread and keeps the editor and its text in memory,
	/// and the folding.
	/// </summary>
	public void Dispose()
	{
		_isDisposed = true;

		if (Document is { } document)
		{
			TextDocumentWeakEventManager.Changing.RemoveHandler(document, Document_Changing);
		}

		RemoveHighlighting();

		RemoveFolding();
	}

	/// <summary>
	/// Returns the offsets where the folded blocks start, or the unfolded ones; none while the text does not fold.
	/// </summary>
	public int[] GetBlockStarts(bool isFolded) => _folding.GetBlockStarts(isFolded);

	/// <summary>
	/// Folds or unfolds the blocks that start at the offsets and turns the other blocks the other way; an offset where no
	/// block starts is skipped.
	/// </summary>
	public void SetBlocksFolded(IEnumerable<int> starts, bool isFolded) => _folding.SetBlocksFolded(starts, isFolded);

	/// <summary>
	/// Puts the selection, the caret, the folded blocks and the scroll offset in place; nothing before the view is built.
	/// </summary>
	public void SetView(
		int selectionStart,
		int selectionLength,
		TextViewPosition caretPosition,
		IEnumerable<int> blockStarts,
		bool isFolded,
		Vector scrollOffset)
	{
		if (ScrollViewer is not { } scrollViewer)
		{
			return;
		}

		Select(selectionStart, selectionLength);

		TextArea.Caret.Position = caretPosition;

		// After the caret, whose move unfolds a block it lands in, and before the offset, which is measured over the
		// folded text.
		SetBlocksFolded(blockStarts, isFolded);

		scrollViewer.Offset = scrollOffset;
	}

	/// <summary>
	/// Finds the blocks that fold with a pass over all lines.
	/// </summary>
	internal void UpdateFoldings() => _folding.UpdateBlocks();

	/// <summary>
	/// Finds the line endings of the document with a pass over all its lines.
	/// </summary>
	internal void UpdateLineEnding()
	{
		Status = Status with
		{
			LineEnding = Document.FindLineEnding()
		};
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		// The highlighting and the folding serve the old document, while the text area may lay out a line of the new one
		// before the change is reported, as it does for the input method of Windows, which asks for the caret at once.
		if (change.Property == DocumentProperty)
		{
			RemoveHighlighting();

			RemoveFolding();
		}

		base.OnPropertyChanged(change);
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
	/// Returns the bookmarks of a document, or an empty set of its own without a document.
	/// </summary>
	private static LineBookmarks GetBookmarks(TextDocument? document) => document is null ? new() : LineBookmarks.Of(document);

	/// <summary>
	/// Returns the color theme of the words for a theme of the application.
	/// </summary>
	private static IRawTheme GetColorTheme(ThemeVariant variant) => SyntaxRegistry.Instance.GetColorTheme(variant == ThemeVariant.Dark);

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
	/// Validates <see cref="ConvertLineEndingsCommand" />.
	/// </summary>
	private bool CanConvertLineEndings(LineEnding lineEnding)
	{
		// A pass of its own, as the status catches up with an edit only after a delay.
		LineEnding current = Document.FindLineEnding();

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

		return !transform.NeedsSelection() || TextArea.Selection.Length > 0;
	}

	/// <summary>
	/// Validates <see cref="UndoCommand" />.
	/// </summary>
	private bool CanUndoEdit() => CanUndo;

	/// <summary>
	/// Brings every line break of the document to a style, as one step to undo.
	/// </summary>
	private void ConvertLineEndings(LineEnding lineEnding)
	{
		string newLine = lineEnding.ToNewLine();

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
		using (Document.RunUpdate())
		{
			foreach (SelectionSegment segment in TextArea.Selection.Segments.Reverse())
			{
				string text = TextCase.ToTitleCase(Document.GetText(segment), CultureInfo.CurrentCulture);

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
	/// Returns the scope name of the grammar that highlights the text; <c>null</c> when the text stays plain.
	/// </summary>
	private string? FindHighlightedScope()
	{
		if (_isDisposed
			|| !IsVisible
			|| SyntaxLanguage is not { } language
			|| Document is not { TextLength: <= MaxHighlightedLength })
		{
			return null;
		}

		return SyntaxRegistry
			.Instance
			.FindScope(language);
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
	/// Removes the folding, and the folded text comes back into view.
	/// </summary>
	private void RemoveFolding()
	{
		// The base constructor sets the first document before this constructor makes the folding.
		_folding?.Remove();

		CanFold = false;
	}

	/// <summary>
	/// Removes the syntax highlighting and stops its tokenizer.
	/// </summary>
	private void RemoveHighlighting()
	{
		if (_highlighting is not { } highlighting)
		{
			return;
		}

		_highlighting = null;

		_highlightingScope = null;

		highlighting.Dispose();
	}

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

		GetEngineCommand(transform).Execute(null, TextArea);
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

	/// <summary>
	/// Brings the folding in line with the language, the document and the visibility of the editor.
	/// </summary>
	private void UpdateFolding()
	{
		// A text folds while it is highlighted, by the rules of its language.
		_folding.Update(FindHighlightedScope() is null ? null : SyntaxLanguage);

		CanFold = _folding.CanFold;
	}

	/// <summary>
	/// Brings the syntax highlighting in line with the language, the document and the visibility of the editor.
	/// </summary>
	private void UpdateHighlighting()
	{
		string? scope = FindHighlightedScope();

		if (scope == _highlightingScope)
		{
			return;
		}

		// A grammar swapped under a running tokenizer may leave the tokens of the old one, so the highlighting starts anew.
		RemoveHighlighting();

		if (scope is null)
		{
			return;
		}

		// Kept before it is set up, so that a failed setup leaves nothing running unseen.
		_highlighting = this.InstallTextMate(SyntaxRegistry.Instance);

		_highlightingScope = scope;

		_highlighting.SetTheme(GetColorTheme(ActualThemeVariant));

		_highlighting.SetGrammar(scope);
	}

	/// <summary>
	/// Brings the syntax highlighting and the folding in line with the language, the document and the visibility of
	/// the editor.
	/// </summary>
	private void UpdateSyntax()
	{
		UpdateHighlighting();

		UpdateFolding();
	}
	#endregion
}
