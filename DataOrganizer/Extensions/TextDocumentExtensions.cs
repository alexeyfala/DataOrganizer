using AvaloniaEdit.Document;
using EditorDocuments.Enums;

namespace DataOrganizer.Extensions.Documents;

internal static class TextDocumentExtensions
{
	#region Methods
	/// <summary>
	/// Returns the line break style shared by the lines of a document.
	/// </summary>
	public static LineEnding FindLineEnding(this TextDocument? document)
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
	#endregion
}
