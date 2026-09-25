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
	/// Every line ends with a carriage return and a line feed.
	/// </summary>
	CrLf,

	/// <summary>
	/// Every line ends with a line feed.
	/// </summary>
	Lf,

	/// <summary>
	/// Every line ends with a carriage return.
	/// </summary>
	Cr,

	/// <summary>
	/// The lines end in different ways.
	/// </summary>
	Mixed
}
