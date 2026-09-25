namespace DataOrganizer.Enums;

/// <summary>
/// Line break style of a document.
/// </summary>
public enum LineEnding
{
	/// <summary>
	/// The document has no line breaks.
	/// </summary>
	None,

	/// <summary>
	/// Every line ends with a carriage return and a line feed (Windows).
	/// </summary>
	CrLf,

	/// <summary>
	/// Every line ends with a line feed (Unix).
	/// </summary>
	Lf,

	/// <summary>
	/// Every line ends with a carriage return (Macintosh).
	/// </summary>
	Cr,

	/// <summary>
	/// The lines end in different ways.
	/// </summary>
	Mixed
}
