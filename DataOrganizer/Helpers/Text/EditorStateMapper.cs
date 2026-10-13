using AvaloniaEdit;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Text;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Carries the state of a text editor between <see cref="FileEditorState" /> and the values of the editor.
/// </summary>
internal static class EditorStateMapper
{
	#region Methods
	/// <summary>
	/// Gives the values of an editor those of a saved state; a stored language counts only while it has a grammar.
	/// </summary>
	public static void Apply(
		IEditorStateValues values,
		FileEditorState state,
		ISyntaxRegistry syntaxRegistry)
	{
		values.FontSize = state.FontSize;

		values.ShowEndOfLine = state.ShowEndOfLine;

		values.ShowSpaces = state.ShowSpaces;

		values.ShowTabs = state.ShowTabs;

		values.SyntaxLanguage = GetSyntaxLanguage(
			state.SyntaxLanguage,
			values.DefaultSyntaxLanguage,
			syntaxRegistry);

		values.WordWrap = state.WordWrap;

		values.ViewState = new DocumentViewState
		{
			Bookmarks = state.Bookmarks,
			CaretPosition = ToTextViewPosition(state.CaretPosition),
			FoldedBlocks = state.FoldedBlocks,
			ScrollOffset = new(state.ScrollOffset.X, state.ScrollOffset.Y),
			SelectionLength = state.SelectionLength,
			SelectionStart = state.SelectionStart,
			UnfoldedBlocks = state.UnfoldedBlocks
		};
	}

	/// <summary>
	/// Returns the state that keeps the values of an editor together with the web name of the encoding to store; an
	/// encrypted text keeps no folded blocks.
	/// </summary>
	public static FileEditorState Create(IEditorStateValues values, string? encoding, bool isEncrypted)
	{
		DocumentViewState view = values.ViewState.GetValueOrDefault();

		return new()
		{
			Bookmarks = view.Bookmarks,
			CaretPosition = ToCaretPosition(view.CaretPosition),
			Encoding = encoding,
			// The blocks of a protected text would give away its outline, which its ciphertext does not.
			FoldedBlocks = isEncrypted ? null : view.FoldedBlocks,
			FontSize = values.FontSize,
			ScrollOffset = new((int)view.ScrollOffset.X, (int)view.ScrollOffset.Y),
			SelectionLength = view.SelectionLength,
			SelectionStart = view.SelectionStart,
			ShowEndOfLine = values.ShowEndOfLine,
			ShowSpaces = values.ShowSpaces,
			ShowTabs = values.ShowTabs,
			SyntaxLanguage = GetStoredSyntaxLanguage(values.SyntaxLanguage, values.DefaultSyntaxLanguage),
			UnfoldedBlocks = isEncrypted ? null : view.UnfoldedBlocks,
			WordWrap = values.WordWrap
		};
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the language to store in the editor state; <c>null</c> while the text keeps the default one.
	/// </summary>
	private static string? GetStoredSyntaxLanguage(string? language, string? defaultLanguage)
	{
		if (language == defaultLanguage)
		{
			return null;
		}

		return language ?? FileEditorState.PlainTextLanguage;
	}

	/// <summary>
	/// Returns the language of the text for the one stored in the editor state.
	/// </summary>
	private static string? GetSyntaxLanguage(
		string? stored,
		string? defaultLanguage,
		ISyntaxRegistry syntaxRegistry)
	{
		if (stored is null)
		{
			return defaultLanguage;
		}

		// Plain text is stored as a language without a grammar, and so is a language whose grammar is gone.
		return syntaxRegistry.FindScope(stored) is null ? null : stored;
	}

	/// <summary>
	/// Returns the caret to store for the one of the editor.
	/// </summary>
	private static CaretPosition ToCaretPosition(TextViewPosition position)
	{
		return new()
		{
			Column = position.Column,
			IsAtEndOfLine = position.IsAtEndOfLine,
			Line = position.Line,
			VisualColumn = position.VisualColumn
		};
	}

	/// <summary>
	/// Returns the caret of the editor for the stored one.
	/// </summary>
	private static TextViewPosition ToTextViewPosition(CaretPosition position)
	{
		return new(
			line: position.Line,
			column: position.Column,
			visualColumn: position.VisualColumn)
		{
			IsAtEndOfLine = position.IsAtEndOfLine
		};
	}
	#endregion
}
