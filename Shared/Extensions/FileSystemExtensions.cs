using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Shared.Extensions;

public static class FileSystemExtensions
{
	#region Methods
	/// <summary>
	/// Determines whether a folder exists whose name matches the last segment of the path in its exact case.
	/// </summary>
	public static bool DirectoryExists(this IFileSystem fileSystem, [NotNullWhen(true)] string? directoryPath)
	{
		if (string.IsNullOrEmpty(directoryPath) || !fileSystem.Directory.Exists(directoryPath))
		{
			return false;
		}

		string trimmedPath = Path.TrimEndingDirectorySeparator(directoryPath);

		if (Path.GetDirectoryName(trimmedPath) is not { } parentPath)
		{
			return true;
		}

		try
		{
			string directoryName = Path.GetFileName(trimmedPath);

			if (string.IsNullOrEmpty(directoryName))
			{
				return true;
			}

			return fileSystem
				.Directory
				.EnumerateDirectories(parentPath)
				.Any(x => string.Equals(Path.GetFileName(x), directoryName, StringComparison.Ordinal));
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex);

			return false;
		}
	}

	/// <summary>
	/// Determines whether a file exists whose name matches the last segment of the path in its exact case.
	/// </summary>
	public static bool FileExists(this IFileSystem fileSystem, [NotNullWhen(true)] string? filePath)
	{
		// Stryker disable once Logical : equivalent: the listing below finds no file that the quick check misses
		if (string.IsNullOrEmpty(filePath) || !fileSystem.File.Exists(filePath))
		{
			return false;
		}

		try
		{
			string fileName = Path.GetFileName(filePath);

			// An existing file always has a parent folder and a name.
			return fileSystem
				.Directory
				.EnumerateFiles(Path.GetDirectoryName(filePath)!)
				.Any(x => string.Equals(Path.GetFileName(x), fileName, StringComparison.Ordinal));
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex);

			return false;
		}
	}

	/// <summary>
	/// Adds/removes the <see cref="FileAttributes.Hidden" /> attribute to a file.
	/// </summary>
	public static void SetFileHidden(
		this IFileSystem fileSystem,
		string filePath,
		bool isHidden)
	{
		SetFileAttribute(
			fileSystem,
			filePath,
			FileAttributes.Hidden,
			isHidden);
	}

	/// <summary>
	/// Adds/removes the <see cref="FileAttributes.ReadOnly" /> attribute to a file.
	/// </summary>
	public static void SetFileReadOnly(
		this IFileSystem fileSystem,
		string filePath,
		bool isReadOnly)
	{
		SetFileAttribute(
			fileSystem,
			filePath,
			FileAttributes.ReadOnly,
			isReadOnly);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Adds or removes <paramref name="attribute" /> and keeps the other attributes of the file.
	/// </summary>
	private static void SetFileAttribute(
		IFileSystem fileSystem,
		string filePath,
		FileAttributes attribute,
		bool isSet)
	{
		FileAttributes current = fileSystem
			.File
			.GetAttributes(filePath);

		fileSystem
			.File
			.SetAttributes(filePath, isSet ? current | attribute : current & ~attribute);
	}
	#endregion
}
