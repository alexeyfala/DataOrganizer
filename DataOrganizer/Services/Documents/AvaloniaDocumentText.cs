using AvaloniaEdit.Document;
using DataOrganizer.Interfaces.Documents;
using System;
using System.Reactive;
using System.Reactive.Linq;

namespace DataOrganizer.Services.Documents;

public sealed class AvaloniaDocumentText : IDocumentText, ITextDocumentHolder
{
	#region Properties
	/// <inheritdoc />
	public IObservable<Unit> Changed { get; }

	/// <inheritdoc />
	public TextDocument Document { get; } = new();

	/// <inheritdoc />
	public string Text => Document.Text;
	#endregion

	#region Constructors
	public AvaloniaDocumentText()
	{
		Changed = Observable
			.FromEventPattern<EventHandler, EventArgs>(
				x => Document.TextChanged += x,
				x => Document.TextChanged -= x)
			.Select(static _ => Unit.Default);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Replace(string text)
	{
		Document.Text = text;

		Document
			.UndoStack
			.ClearAll();
	}
	#endregion
}
