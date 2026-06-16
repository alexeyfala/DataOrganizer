namespace DataOrganizer.Enums.Clipboard;

/// <summary>
/// Type filter applied to the clipboard history list.
/// </summary>
public enum ClipboardEntryFilter
{
	/// <summary>
	/// No filtering; every entry is shown.
	/// </summary>
	All,

	/// <summary>
	/// Plain / formatted text entries, excluding URLs.
	/// </summary>
	Text,

	/// <summary>
	/// URL entries.
	/// </summary>
	Url,

	/// <summary>
	/// Image entries.
	/// </summary>
	Image,

	/// <summary>
	/// File / folder entries.
	/// </summary>
	Files
}
