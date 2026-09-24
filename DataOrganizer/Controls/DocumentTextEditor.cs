using Avalonia;
using AvaloniaEdit;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;

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
	}
	#endregion

	#region Event Handlers
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

	#region Helpers
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
