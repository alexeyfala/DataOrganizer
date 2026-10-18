using DataOrganizer.Enums;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Notepad;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.ViewModels;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace DataOrganizer.Services.Notepad;

public sealed class NotepadTexts : INotepadTexts
{
	#region Data
	/// <summary>
	/// Pause in typing after which a changed text is written to the disk.
	/// </summary>
	internal static readonly TimeSpan WriteDelay = TimeSpan.FromSeconds(0.5);

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <summary>
	/// Tabs whose last write failed, so that a failure is told once rather than after every pause in typing.
	/// </summary>
	private readonly HashSet<NotepadTabViewModel> _failedWrites = [];

	/// <summary>
	/// Tabs whose texts are written back after each pause in typing.
	/// </summary>
	private readonly HashSet<NotepadTabViewModel> _followedTabs = [];

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <summary>
	/// Timers of the tabs whose changed texts wait for a pause in typing to be written.
	/// </summary>
	private readonly Dictionary<NotepadTabViewModel, ITimer> _pendingWrites = [];

	/// <inheritdoc cref="INotepadStore" />
	private readonly INotepadStore _store;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;
	#endregion

	#region Constructors
	public NotepadTexts(
		IDispatcherAccessor dispatcher,
		INotepadStore store,
		INotificationService notification,
		TimeProvider timeProvider)
	{
		_dispatcher = dispatcher;

		_store = store;

		_notification = notification;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// Puts the write of a changed text off until typing pauses.
	/// </summary>
	private void Document_TextChanged(object? sender, EventArgs e)
	{
		if (_followedTabs.FirstOrDefault(x => ReferenceEquals(x.Editor.Document, sender)) is not { } tab)
		{
			return;
		}

		// Each change puts the write off until typing pauses.
		if (_pendingWrites.TryGetValue(tab, out ITimer? timer))
		{
			timer.Change(WriteDelay, Timeout.InfiniteTimeSpan);

			return;
		}

		_pendingWrites[tab] = _timeProvider.CreateTimer(
			_ => _dispatcher.Post(() => WritePendingText(tab)),
			null,
			WriteDelay,
			Timeout.InfiniteTimeSpan);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Erase(NotepadTabViewModel tab)
	{
		if (tab.IsReadOnly)
		{
			return;
		}

		_store.Erase(tab.Number);
	}

	/// <inheritdoc />
	public void Follow(NotepadTabViewModel tab)
	{
		tab.Editor.RefreshEncoding();

		StartFollowing(tab);
	}

	/// <inheritdoc />
	public void Forget(NotepadTabViewModel tab)
	{
		tab.Editor.Document.TextChanged -= Document_TextChanged;

		_followedTabs.Remove(tab);

		if (_pendingWrites.Remove(tab, out ITimer? timer))
		{
			timer.Dispose();
		}

		_failedWrites.Remove(tab);
	}

	/// <inheritdoc />
	public bool Load(NotepadTabViewModel tab)
	{
		DocumentEditorViewModel editor = tab.Editor;

		// The text is read in the encoding of the tab, UTF-8 unless the settings keep another one.
		if (_store.Read(tab.Number) is not { } contents
			|| editor.Codec.Read(contents, editor.Codec.Encoding) is not { } text)
		{
			tab.IsReadOnly = true;

			string message = string.Format(CultureInfo.CurrentCulture, Strings.TabTextUnreadableFormat, tab.Header);

			// The snackbar of the window takes messages only once the window is loaded, which comes before the background.
			_dispatcher.Post(
				() => _notification.ShowErrorSnackbar(message, SnackbarHostIdentifiers.Notepad),
				UiPriority.Background);

			return false;
		}

		editor.ReplaceText(text);

		editor.RefreshEncoding();

		StartFollowing(tab);

		return true;
	}

	/// <inheritdoc />
	public void WritePending()
	{
		foreach (NotepadTabViewModel tab in _pendingWrites.Keys.ToArray())
		{
			WritePendingText(tab);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the bytes of the text of a tab in its encoding; a text that the encoding cannot hold is kept in UTF-8 from
	/// then on, where a lone surrogate becomes the replacement character.
	/// </summary>
	private byte[] EncodeText(NotepadTabViewModel tab)
	{
		DocumentEditorViewModel editor = tab.Editor;

		string text = editor.Document.Text;

		if (editor.Codec.Encode(text, out string? missingCharacter) is { } contents)
		{
			return contents;
		}

		byte[] utf8 = Encoding.UTF8.GetBytes(text);

		if (editor.Codec.Encoding == Encoding.UTF8.WebName)
		{
			return utf8;
		}

		_notification.ShowWarningSnackbar(
			string.Format(
				CultureInfo.CurrentCulture,
				Strings.CharacterNotInEncodingSavedInUtf8Format,
				missingCharacter,
				editor.Codec.EncodingName),
			SnackbarHostIdentifiers.Notepad);

		// The text takes UTF-8 as it is read back from its bytes.
		editor.Codec.Read(utf8, Encoding.UTF8.WebName);

		// A change of the editor writes the settings, so they name UTF-8 before the text is written in it.
		editor.RefreshEncoding();

		return utf8;
	}

	/// <summary>
	/// Writes the text of a tab back after each pause in typing.
	/// </summary>
	private void StartFollowing(NotepadTabViewModel tab)
	{
		_followedTabs.Add(tab);

		tab.Editor.Document.TextChanged += Document_TextChanged;
	}

	/// <summary>
	/// Writes the text of a tab once typing pauses, unless the tab has been closed in the meantime.
	/// </summary>
	private void WritePendingText(NotepadTabViewModel tab)
	{
		if (!_pendingWrites.Remove(tab, out ITimer? timer))
		{
			return;
		}

		timer.Dispose();

		WriteText(tab);
	}

	/// <summary>
	/// Writes the text of a tab to the disk.
	/// </summary>
	private void WriteText(NotepadTabViewModel tab)
	{
		if (_store.Write(tab.Number, EncodeText(tab)))
		{
			_failedWrites.Remove(tab);

			return;
		}

		if (!_failedWrites.Add(tab))
		{
			return;
		}

		_notification.ShowErrorSnackbar(
			string.Format(CultureInfo.CurrentCulture, Strings.TabTextNotSavedFormat, tab.Header),
			SnackbarHostIdentifiers.Notepad);
	}
	#endregion
}
