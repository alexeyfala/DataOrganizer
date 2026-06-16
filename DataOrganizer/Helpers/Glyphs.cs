namespace DataOrganizer.Helpers;

/// <summary>
/// Single source of truth for display glyphs (emoji and symbol characters) shared across the UI.
/// Names follow the Unicode CLDR short name of each glyph where one exists.
/// </summary>
internal static class Glyphs
{
	#region Data
	/// <summary>
	/// <c>&lt;/&gt;</c> — angle brackets around a slash, the conventional markup symbol.
	/// </summary>
	public const string AngleBracketSlash = "</>";

	/// <summary>
	/// B button (blood type).
	/// </summary>
	public const string BButton = "🅱️";

	/// <summary>
	/// Card index dividers.
	/// </summary>
	public const string CardIndexDividers = "🗂️";

	/// <summary>
	/// Clipboard.
	/// </summary>
	public const string Clipboard = "📋";

	/// <summary>
	/// File folder.
	/// </summary>
	public const string FileFolder = "📁";

	/// <summary>
	/// Framed picture.
	/// </summary>
	public const string FramedPicture = "🖼️";

	/// <summary>
	/// Globe showing Americas.
	/// </summary>
	public const string GlobeShowingAmericas = "🌎";

	/// <summary>
	/// Input latin letters.
	/// </summary>
	public const string InputLatinLetters = "🔤";

	/// <summary>
	/// Link.
	/// </summary>
	public const string Link = "🔗";

	/// <summary>
	/// Memo.
	/// </summary>
	public const string Memo = "📝";

	/// <summary>
	/// Middle dot.
	/// </summary>
	public const string MiddleDot = "·";

	/// <summary>
	/// Page facing up.
	/// </summary>
	public const string PageFacingUp = "📄";
	#endregion
}
