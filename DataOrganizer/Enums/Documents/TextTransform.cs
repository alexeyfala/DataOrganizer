namespace DataOrganizer.Enums.Documents;

/// <summary>
/// Transformation of the text of a document.
/// </summary>
public enum TextTransform
{
	/// <summary>
	/// Every letter goes to upper case.
	/// </summary>
	UpperCase,

	/// <summary>
	/// Every letter goes to lower case.
	/// </summary>
	LowerCase,

	/// <summary>
	/// Every word starts with a capital letter, and the rest of its letters go to lower case.
	/// </summary>
	TitleCase,

	/// <summary>
	/// Every letter changes its case.
	/// </summary>
	InvertCase,

	/// <summary>
	/// Spaces and tabs at the ends of the lines are removed.
	/// </summary>
	RemoveTrailingWhitespace,

	/// <summary>
	/// Spaces and tabs at the starts of the lines are removed.
	/// </summary>
	RemoveLeadingWhitespace,

	/// <summary>
	/// Tabs in the indentation become spaces.
	/// </summary>
	LeadingTabsToSpaces,

	/// <summary>
	/// Spaces in the indentation become tabs.
	/// </summary>
	LeadingSpacesToTabs
}
