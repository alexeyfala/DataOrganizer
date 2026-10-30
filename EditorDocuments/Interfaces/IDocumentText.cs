using System;
using System.Reactive;

namespace EditorDocuments.Interfaces;

/// <summary>
/// Text of a document that an editor shows and edits, with its undo history.
/// </summary>
public interface IDocumentText
{
	#region Properties
	/// <summary>
	/// Signals each change of the text.
	/// </summary>
	IObservable<Unit> Changed { get; }

	/// <summary>
	/// The text as it is now.
	/// </summary>
	string Text { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Replaces the text with one that is not an edit, so undo starts from it.
	/// </summary>
	void Replace(string text);
	#endregion
}
