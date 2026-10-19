using DataOrganizer.Models.Clipboard;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UnitTests.Factories;

/// <summary>
/// Factory methods that build clipboard journal entries.
/// </summary>
public static class ClipboardEntryFactory
{
	#region Methods
	/// <summary>
	/// Creates the required number of plain files, named in order from <c>file0.txt</c>, in a path split at '/' as every
	/// OS does.
	/// </summary>
	public static ClipboardFileSystemEntry[] CreateFiles(int count)
	{
		return [.. Enumerable
			.Range(0, count)
			.Select(static i => new ClipboardFileSystemEntry($"/dir/file{i}.txt", IsFolder: false))];
	}

	/// <summary>
	/// Creates a <see cref="ClipboardFilesEntry" /> holding the given entries, a single plain file by default.
	/// </summary>
	public static ClipboardFilesEntry CreateFilesEntry(
		IReadOnlyList<ClipboardFileSystemEntry>? entries = null,
		byte[]? hash = null) => new()
		{
			FileSystemEntries = entries ?? [new ClipboardFileSystemEntry(@"C:\file.txt", IsFolder: false)],
			Hash = hash ?? [1]
		};

	/// <summary>
	/// Creates a <see cref="ClipboardImageEntry" /> backed by the given PNG bytes.
	/// </summary>
	public static ClipboardImageEntry CreateImageEntry(byte[]? png = null, byte[]? hash = null) => new()
	{
		OriginalPng = png ?? [],
		Hash = hash ?? [1]
	};

	/// <summary>
	/// Creates a pinned <see cref="ClipboardTextEntry" />.
	/// </summary>
	public static ClipboardTextEntry CreatePinnedTextEntry(string text, byte[]? hash = null)
	{
		ClipboardTextEntry entry = CreateTextEntry(text, hash);

		entry.IsPinned = true;

		return entry;
	}

	/// <summary>
	/// Creates a <see cref="ClipboardTextEntry" /> with optional companion formats, treated as a secret when asked.
	/// </summary>
	public static ClipboardTextEntry CreateTextEntry(
		string text,
		byte[]? hash = null,
		string? html = null,
		string? rtf = null,
		bool isSensitive = false) => new()
		{
			Text = text,
			Html = html,
			Rtf = rtf,
			Hash = hash ?? [1],
			IsSensitive = isSensitive
		};

	/// <summary>
	/// Creates a <see cref="ClipboardUrlEntry" /> whose text and URL are the same.
	/// </summary>
	public static ClipboardUrlEntry CreateUrlEntry(string url, byte[]? hash = null) => new()
	{
		Text = url,
		Html = null,
		Rtf = null,
		Url = url,
		Hash = hash ?? [1]
	};
	#endregion
}
