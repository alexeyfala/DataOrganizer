using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace DataOrganizer.Controls;

/// <summary>
/// Bookmarks of the lines of a <see cref="DocumentTextEditor" /> as the caret goes through them: sets, clears and follows
/// them on the commands of the editor and on the keys of Notepad++.
/// </summary>
internal sealed class DocumentBookmarkNavigation
{
	#region Commands
	/// <summary>
	/// Removes all bookmarks.
	/// </summary>
	public RelayCommand ClearBookmarksCommand { get; }

	/// <summary>
	/// Moves the caret to the next bookmarked line, going round to the first one.
	/// </summary>
	public RelayCommand NextBookmarkCommand { get; }

	/// <summary>
	/// Moves the caret to the previous bookmarked line, going round to the last one.
	/// </summary>
	public RelayCommand PreviousBookmarkCommand { get; }

	/// <summary>
	/// Sets or removes the bookmark of the caret line.
	/// </summary>
	public RelayCommand ToggleBookmarkCommand { get; }
	#endregion

	#region Data
	/// <summary>
	/// The editor whose lines are bookmarked.
	/// </summary>
	private readonly DocumentTextEditor _editor;
	#endregion

	#region Constructors
	public DocumentBookmarkNavigation(DocumentTextEditor editor)
	{
		_editor = editor;

		ClearBookmarksCommand = new(ClearBookmarks, HasBookmarks);

		NextBookmarkCommand = new(GoToNextBookmark, HasBookmarks);

		PreviousBookmarkCommand = new(GoToPreviousBookmark, HasBookmarks);

		ToggleBookmarkCommand = new(ToggleBookmark);

		editor.KeyBindings.Add(new KeyBinding
		{
			Command = ToggleBookmarkCommand,
			Gesture = new KeyGesture(Key.F2, DocumentTextEditor.CommandModifier)
		});

		editor.KeyBindings.Add(new KeyBinding
		{
			Command = NextBookmarkCommand,
			Gesture = new KeyGesture(Key.F2)
		});

		editor.KeyBindings.Add(new KeyBinding
		{
			Command = PreviousBookmarkCommand,
			Gesture = new KeyGesture(Key.F2, KeyModifiers.Shift)
		});
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Removes all bookmarks.
	/// </summary>
	private void ClearBookmarks() => _editor.Bookmarks.Clear();

	/// <summary>
	/// Moves the caret to the next bookmarked line, going round to the first one.
	/// </summary>
	private void GoToNextBookmark() => MoveCaretToLine(_editor.Bookmarks.FindNext(_editor.TextArea.Caret.Line));

	/// <summary>
	/// Moves the caret to the previous bookmarked line, going round to the last one.
	/// </summary>
	private void GoToPreviousBookmark() => MoveCaretToLine(_editor.Bookmarks.FindPrevious(_editor.TextArea.Caret.Line));

	/// <summary>
	/// Validates <see cref="ClearBookmarksCommand" />, <see cref="NextBookmarkCommand" /> and
	/// <see cref="PreviousBookmarkCommand" />.
	/// </summary>
	private bool HasBookmarks() => _editor.Bookmarks.GetLines().Length > 0;

	/// <summary>
	/// Puts the caret at the start of a line and brings the line into view.
	/// </summary>
	private void MoveCaretToLine(int? line)
	{
		if (line is not { } number)
		{
			return;
		}

		// A selection left behind would stretch over the jump.
		_editor.Select(_editor.Document.GetLineByNumber(number).Offset, 0);

		_editor.ScrollTo(number, 1);
	}

	/// <summary>
	/// Sets or removes the bookmark of the caret line.
	/// </summary>
	private void ToggleBookmark() => _editor.Bookmarks.Toggle(_editor.TextArea.Caret.Line);
	#endregion
}
