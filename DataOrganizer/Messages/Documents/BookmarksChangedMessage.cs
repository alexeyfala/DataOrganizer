using DataOrganizer.Helpers.Text;

namespace DataOrganizer.Messages.Documents;

/// <summary>
/// Notification raised when a bookmark of a document is set or removed.
/// </summary>
internal sealed record BookmarksChangedMessage(LineBookmarks Bookmarks);
