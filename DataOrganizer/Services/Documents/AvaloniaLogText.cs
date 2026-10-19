using AvaloniaEdit.Document;
using DataOrganizer.Interfaces.Documents;

namespace DataOrganizer.Services.Documents;

public sealed class AvaloniaLogText : ILogText, ITextDocumentHolder
{
	#region Properties
	/// <inheritdoc />
	public TextDocument Document { get; } = new()
	{
		// An undo history of the log would keep every record and every removed line in memory.
		UndoStack =
		{
			SizeLimit = 0
		}
	};
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Append(string record, int maxLines)
	{
		Document.Insert(Document.TextLength, record);

		RemoveStartLines(Document, maxLines);
	}

	/// <inheritdoc />
	public void Clear() => Document.Remove(0, Document.TextLength);

	/// <summary>
	/// Removes leading lines of <paramref name="document" />, so that at most <paramref name="maxLines" /> remain.
	/// </summary>
	internal static void RemoveStartLines(TextDocument document, int maxLines)
	{
		if (document.LineCount <= maxLines)
		{
			return;
		}

		DocumentLine firstKeptLine = document.GetLineByNumber(document.LineCount - maxLines + 1);

		document.Remove(0, firstKeptLine.Offset);
	}
	#endregion
}
