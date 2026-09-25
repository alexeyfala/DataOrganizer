using Avalonia;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using System;
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
	/// Cuts the selected text.
	/// </summary>
	public RelayCommand CutCommand { get; }

	/// <summary>
	/// Pastes the text from the clipboard.
	/// </summary>
	public RelayCommand PasteCommand { get; }

	/// <summary>
	/// Redoes the last undone edit.
	/// </summary>
	public RelayCommand RedoCommand { get; }

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
		CutCommand = new(CutSelection, CanCutSelection);

		PasteCommand = new(PasteText, CanPasteText);

		RedoCommand = new(RedoEdit, CanRedoEdit);

		UndoCommand = new(UndoEdit, CanUndoEdit);

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
			Column = TextArea.Caret.Column,
			Line = TextArea.Caret.Line,
			LineCount = LineCount,
			LineEnding = FindLineEnding(Document),
			SelectionLength = TextArea.Selection.Length
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
			Column = TextArea.Caret.Column,
			Line = TextArea.Caret.Line
		};
	}

	/// <summary>
	/// <see cref="TextEditor.DocumentChanged" /> event handler.
	/// </summary>
	private void DocumentTextEditor_DocumentChanged(object? sender, DocumentChangedEventArgs e) => UpdateLineEnding();

	/// <summary>
	/// <see cref="TextEditor.TextChanged" /> event handler.
	/// </summary>
	private void DocumentTextEditor_TextChanged(object? sender, EventArgs e)
	{
		Status = Status with
		{
			LineCount = LineCount
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
			SelectionLength = TextArea.Selection.Length
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
	/// Validates <see cref="UndoCommand" />.
	/// </summary>
	private bool CanUndoEdit() => CanUndo;

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
