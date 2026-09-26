using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Messages.Documents;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Bookmarks of the lines of a document, which stay with their lines through the edits.
/// </summary>
internal sealed class LineBookmarks
{
	#region Properties
	/// <summary>
	/// Document with the lines; another document starts without bookmarks.
	/// </summary>
	public TextDocument? Document
	{
		get => _document;
		set
		{
			if (value == _document)
			{
				return;
			}

			_document = value;

			_anchors.Clear();
		}
	}
	#endregion

	#region Data
	/// <summary>
	/// Anchors at the starts of the bookmarked lines.
	/// </summary>
	private readonly List<TextAnchor> _anchors = [];

	/// <inheritdoc cref="Document" />
	private TextDocument? _document;
	#endregion

	#region Methods
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
	/// Sets a bookmark on a line without one and removes the bookmark of a line with one.
	/// </summary>
	public void Toggle(int line)
	{
		if (_document is not { } document || line < 1 || line > document.LineCount)
		{
			return;
		}

		// Lines joined by an edit keep the bookmarks of both, and one toggle removes them all.
		if (_anchors.RemoveAll(x => x.Line == line) > 0)
		{
			RaiseChanged();

			return;
		}

		// Text put at the start of the line, a line break too, pushes the anchor on, so the bookmark stays with the text.
		TextAnchor anchor = document.CreateAnchor(document
			.GetLineByNumber(line)
			.Offset);

		// A deletion that takes the start of the line away leaves the bookmark on the line joined in its place.
		anchor.SurviveDeletion = true;

		_anchors.Add(anchor);

		RaiseChanged();
	}
	#endregion

	#region Helpers
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
