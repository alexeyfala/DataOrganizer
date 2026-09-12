using System;
using System.IO;

namespace DataOrganizer.Helpers;

/// <summary>
/// Contains methods for working with file system object names.
/// </summary>
public static class FileNameHelper
{
	#region Methods
	/// <summary>
	/// Returns the extension of the name, ignoring the tail the file system drops.
	/// </summary>
	public static ReadOnlySpan<char> GetExtension(string? name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return [];
		}

		return Path.GetExtension(TrimIgnoredTail(name));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Removes the trailing dots and whitespace that Windows silently drops from a file name.
	/// </summary>
	public static ReadOnlySpan<char> TrimIgnoredTail(ReadOnlySpan<char> fileName)
	{
		while (!fileName.IsEmpty)
		{
			char last = fileName[^1];

			if (last != '.' && !char.IsWhiteSpace(last))
			{
				break;
			}

			fileName = fileName[..^1];
		}

		return fileName;
	}
	#endregion
}
