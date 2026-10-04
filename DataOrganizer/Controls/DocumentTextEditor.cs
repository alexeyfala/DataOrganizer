using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.TextMate;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Messages.Documents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using TextMateSharp.Themes;

namespace DataOrganizer.Controls;

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
	/// Folds every block of the text.
	/// </summary>
	public RelayCommand FoldAllCommand { get; }

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
	/// Modifier of the keys of the folding chords, the same on every system, as macOS keeps ⌘M for minimizing a window.
	/// </summary>
	private const KeyModifiers ChordModifier = KeyModifiers.Control;

	/// <summary>
	/// The longest text that gets the syntax highlighting, as a longer one would take too much time and memory.
	/// </summary>
	private const int MaxHighlightedLength = 5 * 1024 * 1024;

	/// <summary>
	/// Modifier of the keys of the commands: ⌘ on macOS, like the keys of the engine, and Ctrl elsewhere.
	/// </summary>
	private static readonly KeyModifiers CommandModifier = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

	/// <inheritdoc cref="CanFold" />
	private bool _canFold;

	/// <summary>
	/// Folding of the blocks of the text; <c>null</c> while the text is plain.
	/// </summary>
	private SyntaxFolding? _folding;

	/// <summary>
	/// Syntax highlighting of the text; <c>null</c> while the text is plain.
	/// </summary>
	private TextMate.Installation? _highlighting;

	/// <summary>
	/// Scope name of the grammar of <see cref="_highlighting" />.
	/// </summary>
	private string? _highlightingScope;

	/// <summary>
	/// <c>True</c> after the first key of a chord, while its second key is awaited.
	/// </summary>
	private bool _isChordStarted;

	/// <summary>
	/// <c>True</c> once the editor has been disposed, after which the text stays plain.
	/// </summary>
	private bool _isDisposed;

	/// <inheritdoc cref="Status" />
	private DocumentStatus _status;
	#endregion

	#region Constructors
	public DocumentTextEditor()
	{
		ClearBookmarksCommand = new(ClearBookmarks, HasBookmarks);

		ConvertLineEndingsCommand = new(ConvertLineEndings, CanConvertLineEndings);

		CutCommand = new(CutSelection, CanCutSelection);

		FoldAllCommand = new(FoldAll, CanFoldAll);

		NextBookmarkCommand = new(GoToNextBookmark, HasBookmarks);

		PasteCommand = new(PasteText, CanPasteText);

		PreviousBookmarkCommand = new(GoToPreviousBookmark, HasBookmarks);

		RedoCommand = new(RedoEdit, CanRedoEdit);

		ToggleBookmarkCommand = new(ToggleBookmark);

		ToggleFoldingCommand = new(ToggleFolding, CanToggleFolding);

		TransformCommand = new(TransformText, CanTransformText);

		UndoCommand = new(UndoEdit, CanUndoEdit);

		UnfoldAllCommand = new(UnfoldAll, CanUnfoldAll);

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

		KeyBindings.Add(new KeyBinding
		{
			Command = ToggleBookmarkCommand,
			Gesture = new KeyGesture(Key.F2, CommandModifier)
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

		// The chords of the folding, which key bindings cannot express, reach the editor on the tunnel before its text,
		// and so do the keys that key bindings have taken, as those end a chord too.
		AddHandler(
			KeyDownEvent,
			DocumentTextEditor_KeyDown,
			RoutingStrategies.Tunnel,
			handledEventsToo: true);

		// A chord ends with the focus, so that a key typed on the return is not taken for its second key.
		this
			.GetObservable(IsKeyboardFocusWithinProperty)
			.Subscribe(IsKeyboardFocusWithinProperty_Changed);

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

		TextView textView = TextArea.TextView;

		// The tip of a folded block takes the look of the application rather than the Fluent one the editor brings for itself.
		if (Application
			.Current?
			.TryFindResource(typeof(ToolTip), out object? toolTipTheme) == true)
		{
			textView.Resources[typeof(ToolTip)] = toolTipTheme;
		}

		// The tip opens and closes with the hover over a box of the text, not with the pointer over the whole view.
		ToolTip.SetServiceEnabled(textView, false);

		textView.PointerHover += TextView_PointerHover;

		textView.PointerHoverStopped += TextView_PointerHoverStopped;

		// A double click on a box unfolds its block, which the tip would still cover, and the presses on a box are handled.
		textView.AddHandler(
			PointerPressedEvent,
			TextView_PointerPressed,
			RoutingStrategies.Tunnel,
			handledEventsToo: true);

		DocumentChanged += DocumentTextEditor_DocumentChanged;

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
			LineEnding = FindLineEnding(Document),
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
	/// <see cref="TextEditor.DocumentChanged" /> event handler.
	/// </summary>
	private void DocumentTextEditor_DocumentChanged(object? sender, DocumentChangedEventArgs e)
	{
		// The bookmarks belong to the lines of one document.
		Bookmarks = GetBookmarks(Document);

		UpdateLineEnding();

		// The highlighting and the folding of the old document are gone, and the new one gets its own unless it is missing
		// or too long.
		UpdateSyntax();
	}

	/// <summary>
	/// <see cref="InputElement.KeyDownEvent" /> handler, which folds on the chords of Visual Studio: Ctrl+M, Ctrl+M for
	/// the block of the caret line and Ctrl+M, Ctrl+L for every block.
	/// </summary>
	private void DocumentTextEditor_KeyDown(object? sender, KeyEventArgs e)
	{
		// A modifier held through a chord repeats its key, and a modifier pressed anew comes before the second key.
		if (IsModifierKey(e.Key))
		{
			return;
		}

		// Key bindings take their keys before the event is raised, and such a key ends a chord without acting in it.
		if (e.Handled)
		{
			_isChordStarted = false;

			return;
		}

		if (!_isChordStarted)
		{
			if (e.Key != Key.M || e.KeyModifiers != ChordModifier)
			{
				return;
			}

			_isChordStarted = true;

			e.Handled = true;

			return;
		}

		_isChordStarted = false;

		// The second key works with the modifier of the first one or without it, and any other key acts as usual.
		if (e.KeyModifiers != KeyModifiers.None && e.KeyModifiers != ChordModifier)
		{
			return;
		}

		switch (e.Key)
		{
			case Key.L:
				ToggleAllFoldings();
				break;

			case Key.M:
				ToggleFolding();
				break;

			default:
				return;
		}

		// A chord with nothing to fold does nothing, rather than type its second key into the text.
		e.Handled = true;
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
	/// <see cref="InputElement.PointerExited" /> event handler of the host of a tip that took the pointer.
	/// </summary>
	private void FoldingTipHost_PointerExited(object? sender, PointerEventArgs e) => CloseFoldingTip();

	/// <summary>
	/// <see cref="InputElement.PointerPressed" /> event handler of the host of a tip that took the pointer.
	/// </summary>
	private void FoldingTipHost_PointerPressed(object? sender, PointerPressedEventArgs e) => CloseFoldingTip();

	/// <summary>
	/// <see cref="InputElement.IsKeyboardFocusWithinProperty" /> changed handler.
	/// </summary>
	private void IsKeyboardFocusWithinProperty_Changed(bool value)
	{
		if (value)
		{
			return;
		}

		_isChordStarted = false;
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
	/// <see cref="TextView.PointerHover" /> event handler, which shows the text that the box of a folded block under the
	/// pointer hides.
	/// </summary>
	private void TextView_PointerHover(object? sender, PointerEventArgs e)
	{
		TextView textView = TextArea.TextView;

		// The text of an encrypted file stays in the text area.
		if (IsSensitive || _folding?.FindHiddenText(e.GetPosition(textView)) is not { } text)
		{
			return;
		}

		ToolTip.SetTip(textView, new TextBlock
		{
			FontFamily = TextArea.FontFamily,
			Text = text
		});

		ToolTip.SetIsOpen(textView, true);
	}

	/// <summary>
	/// <see cref="TextView.PointerHoverStopped" /> event handler, which closes the tip, unless the tip opened under the
	/// pointer and so took the pointer from the view.
	/// </summary>
	private void TextView_PointerHoverStopped(object? sender, PointerEventArgs e)
	{
		TextView textView = TextArea.TextView;

		// Closed, such a tip would leave the pointer to the view, whose hover would open it again, so it stays until the
		// pointer leaves it or presses it, as in Visual Studio Code.
		if (!textView.IsPointerOver && FindFoldingTipHost(e.GetPosition(textView)) is { } host)
		{
			host.PointerExited += FoldingTipHost_PointerExited;

			host.PointerPressed += FoldingTipHost_PointerPressed;

			return;
		}

		CloseFoldingTip();
	}

	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of the text view.
	/// </summary>
	private void TextView_PointerPressed(object? sender, PointerPressedEventArgs e) => CloseFoldingTip();

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

		RemoveHighlighting();

		RemoveFolding();
	}

	/// <summary>
	/// Returns the offsets where the folded blocks start, or the unfolded ones; none while the text does not fold.
	/// </summary>
	public int[] GetBlockStarts(bool isFolded) => _folding?.GetBlockStarts(isFolded) ?? [];

	/// <summary>
	/// Folds or unfolds the blocks that start at the offsets and turns the other blocks the other way; an offset where no
	/// block starts is skipped.
	/// </summary>
	public void SetBlocksFolded(IEnumerable<int> starts, bool isFolded) => _folding?.SetBlocksFolded(starts, isFolded);

	/// <summary>
	/// Finds the blocks that fold with a pass over all lines.
	/// </summary>
	internal void UpdateFoldings() => _folding?.Update();

	/// <summary>
	/// Finds the line endings of the document with a pass over all its lines.
	/// </summary>
	internal void UpdateLineEnding()
	{
		Status = Status with
		{
			LineEnding = FindLineEnding(Document)
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
	/// <c>True</c> for the key of a modifier.
	/// </summary>
	private static bool IsModifierKey(Key key)
	{
		return key is Key.LeftCtrl
			or Key.RightCtrl
			or Key.LeftShift
			or Key.RightShift
			or Key.LeftAlt
			or Key.RightAlt
			or Key.LWin
			or Key.RWin;
	}

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
	/// Validates <see cref="FoldAllCommand" />.
	/// </summary>
	private bool CanFoldAll() => _folding is { HasUnfoldedBlocks: true };

	/// <summary>
	/// Validates <see cref="PasteCommand" />.
	/// </summary>
	private bool CanPasteText() => CanPaste;

	/// <summary>
	/// Validates <see cref="RedoCommand" />.
	/// </summary>
	private bool CanRedoEdit() => CanRedo;

	/// <summary>
	/// Validates <see cref="ToggleFoldingCommand" />.
	/// </summary>
	private bool CanToggleFolding() => _folding?.FindBlock(TextArea.Caret.Line) is not null;

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
	/// Validates <see cref="UnfoldAllCommand" />.
	/// </summary>
	private bool CanUnfoldAll() => _folding is { HasFoldedBlocks: true };

	/// <summary>
	/// Removes all bookmarks.
	/// </summary>
	private void ClearBookmarks() => Bookmarks.Clear();

	/// <summary>
	/// Takes away the tip of a folded block with its text, which closes the tip.
	/// </summary>
	private void CloseFoldingTip() => ToolTip.SetTip(TextArea.TextView, null);

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
	/// Returns the host of the open tip, the window or the element that shows it, when it covers a point of the view;
	/// <c>null</c> when no open tip covers the point.
	/// </summary>
	private InputElement? FindFoldingTipHost(Point point)
	{
		TextView textView = TextArea.TextView;

		// A popup shows in a window of its own, or in the overlay of the window where windows of their own are not at hand.
		if ((ToolTip.GetTip(textView) as Visual)?
			.GetVisualAncestors()
			.FirstOrDefault(static x => x is PopupRoot or OverlayPopupHost) is not InputElement host)
		{
			return null;
		}

		// A tip at the pointer starts right where the pointer stands, which rounding to pixels may move by one pixel.
		PixelVector rounding = new(1, 1);

		// The view and the tip may stand in windows of their own, so they meet on the screen.
		PixelRect bounds = new(
			host.PointToScreen(default) - rounding,
			host.PointToScreen(new Point(host.Bounds.Width, host.Bounds.Height)) + rounding);

		return bounds.Contains(textView.PointToScreen(point)) ? host : null;
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
	/// Folds every block and moves the caret out of the folded text.
	/// </summary>
	private void FoldAll()
	{
		if (_folding is not { } folding)
		{
			return;
		}

		folding.FoldAll();

		MoveCaretOutOfFolding();

		ReportFolding();
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
	/// Moves the caret out of the folded block that hides it to the start of the block, which stays in view.
	/// </summary>
	private void MoveCaretOutOfFolding()
	{
		if (_folding?.FindFoldedBlock(TextArea.Caret.Offset) is not { } block)
		{
			return;
		}

		// A selection left behind would reach into the hidden text.
		Select(block.StartOffset, 0);

		TextArea
			.Caret
			.BringCaretToView();
	}

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
	/// Removes the folding, and the folded text comes back into view.
	/// </summary>
	private void RemoveFolding()
	{
		if (_folding is not { } folding)
		{
			return;
		}

		_folding = null;

		CanFold = false;

		// The tip shows the text of a block that goes away with the folding.
		CloseFoldingTip();

		folding.Dispose();
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
	/// Tells that blocks of the editor folded or unfolded.
	/// </summary>
	private void ReportFolding()
	{
		WeakReferenceMessenger
			.Default
			.Send(new FoldingChangedMessage(this));
	}

	/// <summary>
	/// Unfolds every block when a block is folded, and folds every block otherwise, as Visual Studio does.
	/// </summary>
	private void ToggleAllFoldings()
	{
		if (_folding is { HasFoldedBlocks: true })
		{
			UnfoldAll();

			return;
		}

		FoldAll();
	}

	/// <summary>
	/// Sets or removes the bookmark of the caret line.
	/// </summary>
	private void ToggleBookmark() => Bookmarks.Toggle(TextArea.Caret.Line);

	/// <summary>
	/// Folds or unfolds the innermost block of the caret line, and moves the caret out of it when it folds.
	/// </summary>
	private void ToggleFolding()
	{
		if (_folding?.FindBlock(TextArea.Caret.Line) is not { } block)
		{
			return;
		}

		block.IsFolded = !block.IsFolded;

		MoveCaretOutOfFolding();

		ReportFolding();
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
	/// Unfolds every block.
	/// </summary>
	private void UnfoldAll()
	{
		if (_folding is not { } folding)
		{
			return;
		}

		folding.UnfoldAll();

		ReportFolding();
	}

	/// <summary>
	/// Brings the folding in line with the language, the document and the visibility of the editor.
	/// </summary>
	private void UpdateFolding()
	{
		// A text folds while it is highlighted, by the rules of its language.
		string? language = FindHighlightedScope() is null ? null : SyntaxLanguage;

		if (language == _folding?.Language && Document == _folding?.Document)
		{
			return;
		}

		// The folded text comes back into view with the folding, while neither the caret nor the view moves.
		bool hadFoldedBlocks = _folding is { HasFoldedBlocks: true };

		RemoveFolding();

		if (hadFoldedBlocks)
		{
			ReportFolding();
		}

		if (language is null || SyntaxRegistry
			.Instance
			.FindFoldingRules(language) is not { } rules)
		{
			return;
		}

		_folding = new SyntaxFolding(TextArea, language, rules);

		CanFold = true;
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
