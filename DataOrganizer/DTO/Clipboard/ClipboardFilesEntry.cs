using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.DTO.Clipboard;

/// <summary>
/// Clipboard entry holding a captured list of filesystem items.
/// </summary>
public sealed class ClipboardFilesEntry : ClipboardHistoryEntryBase
{
	#region Properties
	/// <inheritdoc />
	public override string? ContentToolTip => field ??= BuildFilesSummaryToolTip();

	/// <summary>
	/// Pre-computed multi-line display block for <see cref="FileSystemEntries" />.
	/// </summary>
	public string? FilesSummary => field ??= BuildFilesSummary();

	/// <summary>
	/// File / folder items captured for this entry.
	/// </summary>
	public required IReadOnlyList<ClipboardFileSystemEntry> FileSystemEntries { get; init; }

	/// <inheritdoc />
	public override string TypeGlyph => "🗂️";

	/// <inheritdoc />
	public override string TypeToolTip => $"{Strings.Folders} - {Strings.Files}";
	#endregion

	#region Data
	/// <summary>
	/// Total maximum number of lines rendered by the files-summary block.
	/// </summary>
	private const int FilesSummaryMaxLines = 7;

	/// <summary>
	/// Total maximum number of lines rendered by the files-summary tooltip
	/// (full expanded list, capped to avoid a screen-tall tooltip).
	/// </summary>
	private const int FilesSummaryToolTipMaxLines = 22;
	#endregion

	#region Helpers
	/// <summary>
	/// Enumerates the lines of the files-summary block on demand.
	/// </summary>
	private static IEnumerable<string> EnumerateFilesSummaryLines(
		IReadOnlyList<ClipboardFileSystemEntry> entries,
		int maxLines)
	{
		const int headerLines = 1;

		int maxItemLines = maxLines - headerLines;

		bool truncated = entries.Count > maxItemLines;

		int visibleCount = truncated ? maxItemLines - 1 : entries.Count;

		int folderCount = entries.Count(e => e.IsFolder);

		const string folderGlyph = "📁";

		const string fileGlyph = "📄";

		const string bulletGlyph = "·";

		yield return $"{folderGlyph} {folderCount}  {fileGlyph} {entries.Count - folderCount}";

		foreach (ClipboardFileSystemEntry entry in entries.Take(visibleCount))
		{
			yield return $"{bulletGlyph}  {(entry.IsFolder ? folderGlyph : fileGlyph)}  {entry.Name}";
		}

		if (truncated)
		{
			yield return "...";
		}
	}

	/// <summary>
	/// Builds a multi-line display block for <see cref="FileSystemEntries" />.
	/// </summary>
	private string BuildFilesSummary()
	{
		return string.Join(Environment.NewLine, EnumerateFilesSummaryLines(FileSystemEntries, FilesSummaryMaxLines));
	}

	/// <summary>
	/// Builds the expanded tooltip version of <see cref="FilesSummary" />.
	/// </summary>
	private string? BuildFilesSummaryToolTip()
	{
		// Show the tooltip only when the visible block was truncated.
		const int headerLines = 1;

		if (FileSystemEntries.Count <= FilesSummaryMaxLines - headerLines)
		{
			return null;
		}

		return string.Join(Environment.NewLine, EnumerateFilesSummaryLines(FileSystemEntries, FilesSummaryToolTipMaxLines));
	}
	#endregion
}
