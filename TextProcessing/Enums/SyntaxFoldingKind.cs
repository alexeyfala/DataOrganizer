namespace TextProcessing.Enums;

/// <summary>
/// How the blocks of a language are found.
/// </summary>
public enum SyntaxFoldingKind
{
	/// <summary>
	/// Blocks by indentation, with the runs of imports and of comments.
	/// </summary>
	Indentation,

	/// <summary>
	/// Sections by headings, with comments, as in Markdown.
	/// </summary>
	Headings,

	/// <summary>
	/// Elements by their tags, as in XML.
	/// </summary>
	Tags
}
