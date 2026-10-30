using Hierarchy.Dto;

namespace Clipboard.Messages;

/// <summary>
/// Notification raised when a copied file goes to the top of the copy history and a shown list moves it there at once.
/// </summary>
public sealed record CopyHistoryFileAddedMessage(FileDto File);
