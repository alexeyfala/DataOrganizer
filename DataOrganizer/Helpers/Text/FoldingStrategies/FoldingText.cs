using AvaloniaEdit.Document;
using System;
using System.Buffers;

namespace DataOrganizer.Helpers.Text.FoldingStrategies;

/// <summary>
/// Text of a document with the bounds of its lines, read at once for a pass over all lines.
/// </summary>
internal sealed class FoldingText : IDisposable
{
	#region Properties
	/// <summary>
	/// Number of the lines, which are numbered from one.
	/// </summary>
	public int LineCount { get; }
	#endregion

	#region Data
	/// <summary>
	/// Offset of the end of each line before its line break, by the number of the line.
	/// </summary>
	private readonly int[] _lineEnds;

	/// <summary>
	/// Offset of the start of each line, by the number of the line.
	/// </summary>
	private readonly int[] _lineStarts;

	/// <summary>
	/// The whole text of the document.
	/// </summary>
	private readonly string _text;
	#endregion

	#region Constructors
	public FoldingText(TextDocument document)
	{
		_text = document.Text;

		LineCount = document.LineCount;

		// Pooled, as a long text would take large arrays on every pass.
		_lineEnds = ArrayPool<int>.Shared.Rent(LineCount + 1);

		_lineStarts = ArrayPool<int>.Shared.Rent(LineCount + 1);

		// A walk from line to line with a running offset spares a search in the tree of lines for each line.
		int offset = 0;

		int number = 1;

		for (DocumentLine? line = document.GetLineByNumber(1); line is not null; line = line.NextLine)
		{
			_lineStarts[number] = offset;

			_lineEnds[number] = offset + line.Length;

			offset += line.TotalLength;

			number++;
		}
	}
	#endregion

	#region Methods
	/// <summary>
	/// Gives the arrays of the bounds back to the pool.
	/// </summary>
	public void Dispose()
	{
		ArrayPool<int>.Shared.Return(_lineEnds);

		ArrayPool<int>.Shared.Return(_lineStarts);
	}

	/// <summary>
	/// Returns the text of a line without its line break.
	/// </summary>
	public ReadOnlySpan<char> GetLine(int number) => _text.AsSpan(_lineStarts[number], _lineEnds[number] - _lineStarts[number]);

	/// <summary>
	/// Returns the offset of the end of a line, before its line break.
	/// </summary>
	public int GetLineEnd(int number) => _lineEnds[number];
	#endregion
}
