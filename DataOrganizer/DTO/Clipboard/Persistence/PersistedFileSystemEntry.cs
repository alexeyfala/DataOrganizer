namespace DataOrganizer.DTO.Clipboard.Persistence;

/// <summary>
/// One filesystem item captured in a <see cref="PersistedFilesEntry" />.
/// </summary>
/// <param name="Path">Absolute local path.</param>
/// <param name="IsFolder"><c>True</c> when the item is a directory.</param>
public sealed record PersistedFileSystemEntry(string Path, bool IsFolder);
