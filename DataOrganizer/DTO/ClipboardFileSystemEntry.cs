using SystemPath = System.IO.Path;

namespace DataOrganizer.DTO;

/// <summary>
/// One filesystem item captured in a <see cref="ClipboardHistoryEntry" /> of
/// kind <see cref="Enums.ClipboardEntryKind.FileSystemEntries" />.
/// </summary>
/// <param name="Path">Absolute local path as reported by <c>IStorageItem.Path.LocalPath</c>.</param>
/// <param name="IsFolder"><c>True</c> when the item is a directory.</param>
public sealed record ClipboardFileSystemEntry(string Path, bool IsFolder)
{
	#region Properties
	/// <summary>
	/// Display name derived from <see cref="Path" /> — file name or directory name.
	/// </summary>
	public string Name
	{
		get
		{
			string trimmed = Path.TrimEnd(
				SystemPath.DirectorySeparatorChar,
				SystemPath.AltDirectorySeparatorChar);

			string fileName = SystemPath.GetFileName(trimmed);

			return string.IsNullOrEmpty(fileName) ? trimmed : fileName;
		}
	}
	#endregion
}
