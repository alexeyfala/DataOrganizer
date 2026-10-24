using System;
using System.Buffers;

namespace TextProcessing.Helpers;

/// <summary>
/// Text with the bounds of its lines, read at once for a pass over all lines; a line ends at "\r\n", "\r" or "\n", as
/// in the editor.
/// </summary>
public sealed class FoldingText : IDisposable
{
	#region Properties
	/// <summary>
	/// Number of the lines, which are numbered from one.
	/// </summary>
	public int LineCount { get; }
	#endregion

	#region Data
	/// <summary>
	/// Characters that end a line, alone or as a pair.
	/// </summary>
	private static readonly SearchValues<char> LineBreaks = SearchValues.Create("\r\n");

	/// <summary>
	/// Offset of the end of each line before its line break, by the number of the line.
	/// </summary>
	private readonly int[] _lineEnds;

	/// <summary>
	/// Offset of the start of each line, by the number of the line.
	/// </summary>
	private readonly int[] _lineStarts;

	/// <summary>
	/// The whole text.
	/// </summary>
	private readonly string _text;
	#endregion

	#region Constructors
	public FoldingText(string text)
	{
		_text = text;

		LineCount = CountLines(text);

		// Pooled, as a long text would take large arrays on every pass.
		_lineEnds = ArrayPool<int>.Shared.Rent(LineCount + 1);

		_lineStarts = ArrayPool<int>.Shared.Rent(LineCount + 1);

		int start = 0;

		for (int number = 1; number < LineCount; number++)
		{
			int end = FindLineBreak(text, start);

			_lineStarts[number] = start;

			_lineEnds[number] = end;

			start = end + GetLineBreakLength(text, end);
		}

		// The last line runs to the end of the text.
		_lineStarts[LineCount] = start;

		_lineEnds[LineCount] = text.Length;
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

	/// <summary>
	/// Returns the number of the line that holds an offset, where the end of a line belongs to the line.
	/// </summary>
	public int GetLineNumber(int offset)
	{
		// The starts rise from line to line, so the line is the last one that starts at the offset or before it.
		int index = Array.BinarySearch(_lineStarts, 1, LineCount, offset);

		return index >= 0 ? index : ~index - 1;
	}

	/// <summary>
	/// Returns the offset of the start of a line.
	/// </summary>
	public int GetLineStart(int number) => _lineStarts[number];

	/// <summary>
	/// Returns the text between two offsets.
	/// </summary>
	public ReadOnlySpan<char> GetText(int start, int end) => _text.AsSpan(start, end - start);
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the number of the lines of a text.
	/// </summary>
	private static int CountLines(string text)
	{
		int count = 1;

		int offset = FindLineBreak(text, 0);

		while (offset < text.Length)
		{
			count++;

			offset = FindLineBreak(text, offset + GetLineBreakLength(text, offset));
		}

		return count;
	}

	/// <summary>
	/// Returns the offset of the first line break from <paramref name="start" /> on; the length of the text without one.
	/// </summary>
	private static int FindLineBreak(string text, int start)
	{
		int index = text
			.AsSpan(start)
			.IndexOfAny(LineBreaks);

		return index < 0 ? text.Length : start + index;
	}

	/// <summary>
	/// Returns the length of the line break at an offset, which is two for "\r\n".
	/// </summary>
	private static int GetLineBreakLength(string text, int offset)
	{
		return text[offset] == '\r' && offset + 1 < text.Length && text[offset + 1] == '\n' ? 2 : 1;
	}
	#endregion
}
