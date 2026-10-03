using System;
using System.Buffers;
using System.Collections.Frozen;
using System.IO;
using System.Linq;
using System.Text;

namespace DataOrganizer.Helpers.Execution;

/// <summary>
/// Turns the name of an object into the name of its file in the sandbox, one that every file system accepts.
/// </summary>
internal static class SandboxFileName
{
	#region Data
	/// <summary>
	/// Longest file name in UTF-8 bytes: the limit of Linux and macOS, which also keeps within the limit of Windows.
	/// </summary>
	private const int MaxByteCount = 255;

	/// <summary>
	/// Character put in place of each one a file name cannot hold, and the whole name when nothing is left of it.
	/// </summary>
	private const char Replacement = '_';

	/// <summary>
	/// Characters Windows refuses in a file name, the directory separators of every system among them.
	/// </summary>
	private static readonly SearchValues<char> ForbiddenChars = SearchValues.Create(
	[
		.. Enumerable
			.Range(0, ' ')
			.Select(x => (char)x),
		'"',
		'*',
		'/',
		':',
		'<',
		'>',
		'?',
		'\\',
		'|'
	]);

	/// <summary>
	/// Names of the devices that Windows takes a file name for, even with an extension.
	/// </summary>
	private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> ReservedNames = new[]
	{
		"AUX",
		"CON",
		"NUL",
		"PRN",
		"COM0",
		"COM1",
		"COM2",
		"COM3",
		"COM4",
		"COM5",
		"COM6",
		"COM7",
		"COM8",
		"COM9",
		"COM¹",
		"COM²",
		"COM³",
		"LPT0",
		"LPT1",
		"LPT2",
		"LPT3",
		"LPT4",
		"LPT5",
		"LPT6",
		"LPT7",
		"LPT8",
		"LPT9",
		"LPT¹",
		"LPT²",
		"LPT³"
	}.ToFrozenSet(StringComparer.OrdinalIgnoreCase).GetAlternateLookup<ReadOnlySpan<char>>();
	#endregion

	#region Methods
	/// <summary>
	/// Returns the name of the file in the sandbox for an object with the given name.
	/// </summary>
	public static string Create(string name)
	{
		string fileName = FileNameHelper
			.TrimIgnoredTail(ReplaceForbiddenChars(name))
			.ToString();

		if (IsReserved(fileName))
		{
			fileName = Replacement + fileName;
		}

		fileName = FileNameHelper
			.TrimIgnoredTail(Shorten(fileName))
			.ToString();

		return fileName.Length > 0
			? fileName
			: Replacement.ToString();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when Windows takes the file name for a device.
	/// </summary>
	private static bool IsReserved(string fileName)
	{
		ReadOnlySpan<char> stem = fileName.AsSpan();

		int dotIndex = stem.IndexOf('.');

		if (dotIndex >= 0)
		{
			stem = stem[..dotIndex];
		}

		return ReservedNames.Contains(FileNameHelper.TrimIgnoredTail(stem));
	}

	/// <summary>
	/// Returns the name with every character that a file name cannot hold replaced.
	/// </summary>
	private static string ReplaceForbiddenChars(string name)
	{
		if (!name.AsSpan().ContainsAny(ForbiddenChars))
		{
			return name;
		}

		return string.Create(name.Length, name, static (destination, source) =>
		{
			source.CopyTo(destination);

			foreach (ref char character in destination)
			{
				if (ForbiddenChars.Contains(character))
				{
					character = Replacement;
				}
			}
		});
	}

	/// <summary>
	/// Returns the name cut down to the longest file name, with its extension and its characters kept whole.
	/// </summary>
	private static string Shorten(string fileName)
	{
		if (Encoding.UTF8.GetByteCount(fileName) <= MaxByteCount)
		{
			return fileName;
		}

		string extension = Path.GetExtension(fileName);

		int budget = MaxByteCount - Encoding.UTF8.GetByteCount(extension);

		// An extension that leaves no room for the rest of the name is cut along with it.
		if (budget <= 0)
		{
			extension = string.Empty;

			budget = MaxByteCount;
		}

		ReadOnlySpan<char> stem = fileName.AsSpan(0, fileName.Length - extension.Length);

		int length = 0;

		foreach (Rune rune in stem.EnumerateRunes())
		{
			budget -= rune.Utf8SequenceLength;

			if (budget < 0)
			{
				break;
			}

			length += rune.Utf16SequenceLength;
		}

		return string.Concat(stem[..length], extension);
	}
	#endregion
}
