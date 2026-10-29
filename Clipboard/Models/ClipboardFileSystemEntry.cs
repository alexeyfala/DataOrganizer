using SystemPath = System.IO.Path;

namespace Clipboard.Models;

/// <summary>
/// One filesystem item captured in a <see cref="ClipboardFilesEntry" />.
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

			// The root of a Unix file system trims down to nothing, so it keeps its own path.
			if (trimmed.Length == 0)
			{
				return Path;
			}

			string fileName = SystemPath.GetFileName(trimmed);

			return string.IsNullOrEmpty(fileName) ? trimmed : fileName;
		}
	}
	#endregion
}
