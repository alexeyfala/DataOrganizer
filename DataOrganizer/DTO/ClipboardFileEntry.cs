namespace DataOrganizer.DTO;

/// <summary>
/// One filesystem item captured in a <see cref="ClipboardHistoryEntry" /> of
/// kind <see cref="Enums.ClipboardEntryKind.Files" />.
/// </summary>
/// <param name="Path">Absolute local path as reported by <c>IStorageItem.Path.LocalPath</c>.</param>
/// <param name="IsFolder"><c>True</c> when the item is a directory.</param>
public sealed record ClipboardFileEntry(string Path, bool IsFolder);
