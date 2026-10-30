using EditorDocuments.Interfaces;
using NotepadDocuments.Interfaces;
using Notifications.Constants;
using Notifications.Interfaces;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Threading.Enums;
using Threading.Interfaces;

namespace NotepadDocuments.Services;

/// <inheritdoc cref="INotepadTexts" />
public sealed class NotepadTexts : INotepadTexts
{
	#region Data
	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <summary>
	/// Tabs whose last write failed, so that a failure is told once rather than after every pause in typing.
	/// </summary>
	private readonly HashSet<INotepadTab> _failedWrites = [];

	/// <summary>
	/// Tabs whose texts are written back after each pause in typing, with the subscriptions to the changes of their texts.
	/// </summary>
	private readonly Dictionary<INotepadTab, IDisposable> _followedTabs = [];

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="INotepadStore" />
	private readonly INotepadStore _store;

	/// <inheritdoc cref="ITypingPauseFactory" />
	private readonly ITypingPauseFactory _typingPauseFactory;

	/// <summary>
	/// Pauses in typing after which the changed texts of the tabs are written.
	/// </summary>
	private readonly Dictionary<INotepadTab, ITypingPause> _typingPauses = [];
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
	/// <see cref="IDocumentText.Changed" /> handler of a tab: puts the write of the changed text off until typing pauses.
	/// </summary>
	private void Document_Changed(INotepadTab tab)
	{
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
	public void Erase(INotepadTab tab)
	{
		if (tab.IsReadOnly)
		{
			return;
		}

		_store.Erase(tab.Number);
	}

	/// <inheritdoc />
	public void Follow(INotepadTab tab)
	{
		tab.Editor.RefreshEncoding();

		StartFollowing(tab);
	}

	/// <inheritdoc />
	public void Forget(INotepadTab tab)
	{
		if (_followedTabs.Remove(tab, out IDisposable? subscription))
		{
			subscription.Dispose();
		}

		if (_typingPauses.Remove(tab, out ITypingPause? typingPause))
		{
			typingPause.Dispose();
		}

		_failedWrites.Remove(tab);
	}

	/// <inheritdoc />
	public bool Load(INotepadTab tab)
	{
		IEncodedDocument editor = tab.Editor;

		// The text is read in the encoding of the tab, UTF-8 unless the settings keep another one.
		if (_store.Read(tab.Number) is not { } contents
			|| editor.Codec.Read(contents, editor.Codec.Encoding) is not { } text)
		{
			tab.IsReadOnly = true;

			string message = string.Format(CultureInfo.CurrentCulture, Strings.TabTextUnreadableFormat, tab.Header);

			// The snackbar of the window takes messages only once the window is loaded, which comes before the background.
			// Stryker disable once Statement : the post only carries a notification, which the run ignores
			_dispatcher.Post(
				() => _notification.ShowErrorSnackbar(message, SnackbarHostIdentifiers.Notepad),
				UiPriority.Background);

			return false;
		}

		editor.ReplaceText(text);

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
	private byte[] EncodeText(INotepadTab tab)
	{
		IEncodedDocument editor = tab.Editor;

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
	private void StartFollowing(INotepadTab tab)
	{
		_followedTabs[tab] = tab
			.Editor
			.Document
			.Changed
			.Subscribe(_ => Document_Changed(tab));
	}

	/// <summary>
	/// Writes the text of a tab to the disk.
	/// </summary>
	private void WriteText(INotepadTab tab)
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
