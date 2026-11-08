using System.IO;
using System.IO.Abstractions;

namespace Shared.Extensions;

public static class FileSystemExtensions
{
	#region Methods
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
