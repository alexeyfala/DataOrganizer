using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Interfaces.Notes;
using System.ComponentModel;

namespace DataOrganizer.Views;

internal sealed partial class NoteView : UserControl
{
	#region Properties
	/// <summary>
	/// Plain text shown in the popup.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public string? DisplayedNote
	{
		get => GetValue(DisplayedNoteProperty);
		set => SetValue(DisplayedNoteProperty, value);
	}

	/// <summary>
	/// <c>True</c> when the note is encrypted: a tooltip is shown instead of the popup.
	/// </summary>
	public bool IsLocked
	{
		get => GetValue(IsLockedProperty);
		set => SetValue(IsLockedProperty, value);
	}

	/// <summary>
	/// Note in plain text, used when <see cref="NoteReader" /> is not set.
	/// </summary>
	public string? Note
	{
		get => GetValue(NoteProperty);
		set => SetValue(NoteProperty, value);
	}

	/// <summary>
	/// An object the note belongs to, passed to <see cref="NoteReader" />.
	/// </summary>
	public object? NoteItem
	{
		get => GetValue(NoteItemProperty);
		set => SetValue(NoteItemProperty, value);
	}

	/// <inheritdoc cref="INoteReader" />
	public INoteReader? NoteReader
	{
		get => GetValue(NoteReaderProperty);
		set => SetValue(NoteReaderProperty, value);
	}

	/// <summary>
	/// Controls the display of popup for note.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool ShowNote
	{
		get => GetValue(ShowNoteProperty);
		set => SetValue(ShowNoteProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="DisplayedNote" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> DisplayedNoteProperty = AvaloniaProperty
		.Register<NoteView, string?>(name: nameof(DisplayedNote));

	/// <summary>
	/// Identifies the <see cref="IsLocked" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsLockedProperty = AvaloniaProperty
		.Register<NoteView, bool>(name: nameof(IsLocked));

	/// <summary>
	/// Identifies the <see cref="NoteItem" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<object?> NoteItemProperty = AvaloniaProperty
		.Register<NoteView, object?>(name: nameof(NoteItem));

	/// <summary>
	/// Identifies the <see cref="Note" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> NoteProperty = AvaloniaProperty
		.Register<NoteView, string?>(name: nameof(Note));

	/// <summary>
	/// Identifies the <see cref="NoteReader" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<INoteReader?> NoteReaderProperty = AvaloniaProperty
		.Register<NoteView, INoteReader?>(name: nameof(NoteReader));

	/// <summary>
	/// Identifies the <see cref="ShowNote" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowNoteProperty = AvaloniaProperty
		.Register<NoteView, bool>(name: nameof(ShowNote));
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Copies the currently selected text of the note <see cref="SelectableTextBlock" /> to clipboard.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanCopySelectedNote))]
	private void CopySelectedNote(SelectableTextBlock? target) => target?.Copy();

	/// <summary>
	/// <see cref="Popup.Closed" /> event handler of the note popup.
	/// </summary>
	/// <remarks>
	/// The plain text is kept only while the popup is open.
	/// </remarks>
	[RelayCommand]
	private void NoteClosed() => DisplayedNote = null;

	/// <summary>
	/// <see cref="InputElement.PointerEntered" /> event handler of control for "Note".
	/// </summary>
	/// <remarks>
	/// The hover delay and the pointer check are handled by <c>PointerHoverCommandBehavior</c>.
	/// </remarks>
	[RelayCommand]
	private void NotePointerEntered()
	{
		if (IsLocked)
		{
			return;
		}

		DisplayedNote = NoteReader is { } reader
			? reader.ReadNote(NoteItem)
			: Note;

		if (string.IsNullOrWhiteSpace(DisplayedNote))
		{
			return;
		}

		ShowNote = true;
	}
	#endregion

	#region Constructors
	public NoteView() => InitializeComponent();
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="CopySelectedNoteCommand" />.
	/// </summary>
	private static bool CanCopySelectedNote(SelectableTextBlock? noteView)
	{
		return noteView is not null && noteView.SelectionStart != noteView.SelectionEnd;
	}
	#endregion
}
