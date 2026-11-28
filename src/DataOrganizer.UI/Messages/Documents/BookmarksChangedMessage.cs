using DataOrganizer.UI.Controls.Text;

namespace DataOrganizer.UI.Messages.Documents;

/// <summary>
/// Notification raised when a bookmark of a document is set or removed.
/// </summary>
/// <param name="Bookmarks">Bookmarks of the document.</param>
internal sealed record BookmarksChangedMessage(LineBookmarks Bookmarks);
