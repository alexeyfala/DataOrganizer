namespace DataOrganizer.Enums.Views;

/// <summary>
/// The content of the popup in the favorites window.
/// </summary>
public enum FavoritesPopupContentKind
{
	/// <summary>
	/// The popup is closed.
	/// </summary>
	None,

	/// <summary>
	/// The contents copied to the clipboard.
	/// </summary>
	CopyHistory,

	/// <summary>
	/// The favorite files.
	/// </summary>
	Favorites
}
