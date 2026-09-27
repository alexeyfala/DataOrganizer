using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Messages.Documents;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Bookmarks of the lines of a document, which stay with their lines through the edits.
/// </summary>
internal sealed class LineBookmarks
{
	#region Properties
	/// <summary>
	/// Document with the lines.
	/// </summary>
	public TextDocument? Document { get; init; }
	#endregion

	#region Data
	/// <summary>
	/// Bookmarks of each document, which live as long as their document.
	/// </summary>
	private static readonly ConditionalWeakTable<TextDocument, LineBookmarks> ByDocument = [];

	/// <summary>
	/// Anchors at the starts of the bookmarked lines.
	/// </summary>
	private readonly List<TextAnchor> _anchors = [];
	#endregion

	#region Methods
	/// <summary>
	/// Returns the bookmarks of a document without creating them; <c>null</c> for a document that has none.
	/// </summary>
	public static LineBookmarks? Find(TextDocument document)
	{
		return ByDocument.TryGetValue(document, out LineBookmarks? bookmarks) ? bookmarks : null;
	}

	/// <summary>
	/// Returns the one set of bookmarks of a document, created on first use.
	/// </summary>
	public static LineBookmarks Of(TextDocument document)
	{
		return ByDocument.GetValue(document, static x => new LineBookmarks
		{
			Document = x
		});
	}

	/// <summary>
	/// Removes all bookmarks.
	/// </summary>
	public void Clear()
	{
		if (_anchors.Count == 0)
		{
			return;
		}

		_anchors.Clear();

		RaiseChanged();
	}

	/// <summary>
	/// <c>True</c> when a line has a bookmark.
	/// </summary>
	public bool Contains(int line) => _anchors.Exists(x => x.Line == line);

	/// <summary>
	/// Returns the first bookmarked line after a line, going round to the start of the document;
	/// <c>null</c> without bookmarks.
	/// </summary>
	public int? FindNext(int line)
	{
		int[] lines = GetLines();

		return lines.Length > 0 ? lines.FirstOrDefault(x => x > line, lines[0]) : null;
	}

	/// <summary>
	/// Returns the last bookmarked line before a line, going round to the end of the document;
	/// <c>null</c> without bookmarks.
	/// </summary>
	public int? FindPrevious(int line)
	{
		int[] lines = GetLines();

		return lines.Length > 0 ? lines.LastOrDefault(x => x < line, lines[^1]) : null;
	}

	/// <summary>
	/// Returns the numbers of the bookmarked lines in ascending order.
	/// </summary>
	public int[] GetLines() => [.. _anchors.Select(static x => x.Line).Distinct().Order()];

	/// <summary>
	/// Replaces the bookmarks with those of the lines; a line out of the document gets none.
	/// </summary>
	public void SetLines(IEnumerable<int> lines)
	{
		_anchors.Clear();

		if (Document is { } document)
		{
			_anchors.AddRange(lines
				.Distinct()
				.Where(x => x >= 1 && x <= document.LineCount)
				.Select(x => CreateAnchor(document, x)));
		}

		// One message for all the lines, as a restored set is one change.
		RaiseChanged();
	}

	/// <summary>
	/// Sets a bookmark on a line without one and removes the bookmark of a line with one.
	/// </summary>
	public void Toggle(int line)
	{
		if (Document is not { } document || line < 1 || line > document.LineCount)
		{
			return;
		}

		// Lines joined by an edit keep the bookmarks of both, and one toggle removes them all.
		if (_anchors.RemoveAll(x => x.Line == line) > 0)
		{
			RaiseChanged();

			return;
		}

		_anchors.Add(CreateAnchor(document, line));

		RaiseChanged();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates the anchor of a bookmark at the start of a line.
	/// </summary>
	private static TextAnchor CreateAnchor(TextDocument document, int line)
	{
		// Text put at the start of the line, a line break too, pushes the anchor on, so the bookmark stays with the text.
		TextAnchor anchor = document.CreateAnchor(document
			.GetLineByNumber(line)
			.Offset);

		// A deletion that takes the start of the line away leaves the bookmark on the line joined in its place.
		anchor.SurviveDeletion = true;

		return anchor;
	}

	/// <summary>
	/// Sends <see cref="BookmarksChangedMessage" /> about these bookmarks.
	/// </summary>
	private void RaiseChanged()
	{
		WeakReferenceMessenger
			.Default
			.Send(new BookmarksChangedMessage(this));
	}
	#endregion
}
