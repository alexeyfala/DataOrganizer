using Hierarchy.Dto;

namespace DataOrganizer.Messages.Clipboard;

/// <summary>
/// Notification raised when a copied file goes to the top of the copy history and a shown list moves it there at once.
/// </summary>
public sealed record CopyHistoryFileAddedMessage(FileDto File);
