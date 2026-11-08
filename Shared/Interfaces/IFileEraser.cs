using System;
using System.IO;

namespace Shared.Interfaces;

/// <summary>
/// Overwrites files with random values before deleting them, so their contents cannot be read back from the disk.
/// </summary>
public interface IFileEraser
{
	#region Methods
	/// <summary>
	/// Overwrites with random values the contents of every file of the folder and of its subfolders, then deletes the folder.
	/// </summary>
	/// <remarks>
	/// The <see cref="FileAttributes.ReadOnly" /> sign is removed from every file.
	/// A file that cannot be overwritten does not stop the others; the failures are reported through <see cref="AggregateException" />.
	/// </remarks>
	void EraseAndDeleteDirectory(string directoryPath);

	/// <summary>
	/// Overwrites the file contents with random values, then deletes the file.
	/// </summary>
	void EraseAndDeleteFile(string filePath);
	#endregion
}
