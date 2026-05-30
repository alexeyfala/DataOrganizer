using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.DTO.Clipboard;

/// <summary>
/// Clipboard entry holding a captured list of filesystem entries.
/// </summary>
public sealed class ClipboardFilesEntry : ClipboardHistoryEntryBase
{
	#region Properties
	/// <inheritdoc />
	public override string? ContentToolTip => field ??= BuildEntriesSummaryToolTip();

	/// <summary>
	/// Pre-computed multi-line display block for <see cref="FileSystemEntries" />.
	/// </summary>
	public string? EntriesSummary => field ??= BuildEntriesSummary();

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
	/// Total maximum number of lines rendered by the entries-summary tooltip
	/// (full expanded list, capped to avoid a screen-tall tooltip).
	/// </summary>
	private const int ContentToolTipMaxLines = 22;

	/// <summary>
	/// Total maximum number of lines rendered by the entries-summary block.
	/// </summary>
	private const int EntriesSummaryMaxLines = 7;
	#endregion

	#region Helpers
	/// <summary>
	/// Enumerates the lines of the entries-summary block on demand.
	/// </summary>
	private static IEnumerable<string> EnumerateEntriesSummaryLines(
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
	private string BuildEntriesSummary()
	{
		return string.Join(Environment.NewLine, EnumerateEntriesSummaryLines(FileSystemEntries, EntriesSummaryMaxLines));
	}

	/// <summary>
	/// Builds the expanded tooltip version of <see cref="EntriesSummary" />.
	/// </summary>
	private string? BuildEntriesSummaryToolTip()
	{
		// Show the tooltip only when the visible block was truncated.
		const int headerLines = 1;

		if (FileSystemEntries.Count <= EntriesSummaryMaxLines - headerLines)
		{
			return null;
		}

		return string.Join(Environment.NewLine, EnumerateEntriesSummaryLines(FileSystemEntries, ContentToolTipMaxLines));
	}
	#endregion
}
