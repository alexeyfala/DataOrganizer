using DataOrganizer.Hierarchy.Dto;

namespace DataOrganizer.SystemIntegration.Clipboard.Messages;

/// <summary>
/// Notification raised when a copied file goes to the top of the copy history and a shown list moves it there at once.
/// </summary>
/// <param name="File">Copied file.</param>
public sealed record CopyHistoryFileAddedMessage(FileDto File);
