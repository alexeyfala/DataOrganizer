using AvaloniaEdit;
using CommunityToolkit.Mvvm.Input;
using System.Linq;

namespace DataOrganizer.Controls;

/// <summary>
/// <see cref="TextEditorBase" /> for documents.
/// </summary>
internal sealed class DocumentTextEditor : TextEditorBase
{
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
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="RoutedCommandBinding.CanExecute" /> handler of undo and redo, which denies them in read-only mode.
	/// </summary>
	private void UndoRedoBinding_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
	{
		if (IsReadOnly)
		{
			e.CanExecute = false;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="CutCommand" />.
	/// </summary>
	private bool CanCutSelection()
	{
		// Without a selection the engine would cut the whole line of the caret.
		return CanCut
			&& TextArea.Selection.Length > 0;
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
