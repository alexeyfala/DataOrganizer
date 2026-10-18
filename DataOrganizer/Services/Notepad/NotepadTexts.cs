using DataOrganizer.Enums;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Notepad;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.ViewModels;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DataOrganizer.Services.Notepad;

public sealed class NotepadTexts : INotepadTexts
{
	#region Data
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

	/// <inheritdoc cref="INotepadStore" />
	private readonly INotepadStore _store;

	/// <inheritdoc cref="ITypingPauseFactory" />
	private readonly ITypingPauseFactory _typingPauseFactory;

	/// <summary>
	/// Pauses in typing after which the changed texts of the tabs are written.
	/// </summary>
	private readonly Dictionary<NotepadTabViewModel, ITypingPause> _typingPauses = [];
	#endregion

	#region Constructors
	public NotepadTexts(
		IDispatcherAccessor dispatcher,
		INotepadStore store,
		INotificationService notification,
		ITypingPauseFactory typingPauseFactory)
	{
		_dispatcher = dispatcher;

		_store = store;

		_notification = notification;

		_typingPauseFactory = typingPauseFactory;
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

		if (!_typingPauses.TryGetValue(tab, out ITypingPause? typingPause))
		{
			typingPause = _typingPauseFactory.Create(() => WriteText(tab));

			_typingPauses[tab] = typingPause;
		}

		typingPause.Restart();
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

		if (_typingPauses.Remove(tab, out ITypingPause? typingPause))
		{
			typingPause.Dispose();
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
		foreach (ITypingPause typingPause in _typingPauses.Values)
		{
			typingPause.Flush();
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
