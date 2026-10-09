using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Notepad;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Views;
using Shared.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels.Windows;

/// <summary>
/// View model for <c>NotepadWindow</c>.
/// </summary>
public sealed partial class NotepadViewModel : ObservableDisposableBase
{
	#region Properties
	/// <summary>
	/// Tab Ctrl+Tab goes back to.
	/// </summary>
	[ObservableProperty]
	public partial NotepadTabViewModel? PreviousTab { get; set; }

	/// <summary>
	/// Tab that is selected.
	/// </summary>
	[ObservableProperty]
	public partial NotepadTabViewModel? SelectedTab { get; set; }

	/// <summary>
	/// Open tabs; at least one is always open.
	/// </summary>
	public ObservableCollection<NotepadTabViewModel> Tabs { get; } = [];
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="PreviousTab" /> changes.
	/// </summary>
	partial void OnPreviousTabChanged(NotepadTabViewModel? value) => _sessionState.PreviousTabNumber = value?.Number;

	/// <summary>
	/// Called when <see cref="SelectedTab" /> changes.
	/// </summary>
	partial void OnSelectedTabChanged(NotepadTabViewModel? value) => WriteSettings();
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Brings the main window back from the minimized state and activates it.
	/// </summary>
	[RelayCommand]
	private void ActivateMainWindow() => _viewLauncher.ActivateMainWindow();

	/// <summary>
	/// Adds a tab with the smallest free number at the end and selects it.
	/// </summary>
	[RelayCommand]
	private void AddTab()
	{
		// One of the numbers up to one past the count of the tabs and of the texts left unread is always free.
		int number = Enumerable
			.Range(1, Tabs.Count + _unreadNumbers.Count + 1)
			.First(x => Tabs.All(y => y.Number != x) && !_unreadNumbers.Contains(x));

		NotepadTabViewModel tab = new()
		{
			Number = number
		};

		Tabs.Add(tab);

		SelectedTab = tab;

		// The texts are kept on the disk once they have been read from it; a new tab has nothing there to read.
		if (!_isKeepingTabs)
		{
			return;
		}

		tab.Document.TextChanged += Document_TextChanged;
	}

	/// <summary>
	/// Brings the main window to the center of the screen of the notepad.
	/// </summary>
	[RelayCommand]
	private void CenterMainWindow(Window? owner)
	{
		if (owner is null)
		{
			return;
		}

		_viewLauncher.CenterMainWindow(owner);
	}

	/// <summary>
	/// Closes a tab, after a question when it has text; the last one gives way to a new tab.
	/// </summary>
	[RelayCommand]
	private async Task CloseTab(NotepadTabViewModel? tab)
	{
		if (tab is null)
		{
			return;
		}

		if (HasText(tab) && !await _dialogService
			.RequestYesNoAsync(
				$@"{Strings.Close} ""{tab.Header}""?",
				DialogHostIdentifiers.Notepad)
			.ConfigureAwait(true))
		{
			return;
		}

		RemoveTab(tab);
	}

	/// <summary>
	/// Closes a group of tabs at once, after one question when any of them has text; a refusal closes none of them.
	/// </summary>
	[RelayCommand]
	private async Task CloseTabs(IEnumerable? items)
	{
		if (items is null)
		{
			return;
		}

		// The selected tab goes last, so the others close without being shown on the way.
		NotepadTabViewModel[] tabs = [.. items
			.OfType<NotepadTabViewModel>()
			.OrderBy(x => x == SelectedTab)];

		if (tabs.Any(HasText) && !await _dialogService
			.RequestYesNoAsync(
				Strings.CloseTabsTextWillBeDeleted,
				DialogHostIdentifiers.Notepad)
			.ConfigureAwait(true))
		{
			return;
		}

		foreach (NotepadTabViewModel tab in tabs)
		{
			RemoveTab(tab);
		}
	}

	/// <summary>
	/// Gives a tab the name entered in a dialog of the notepad.
	/// </summary>
	[RelayCommand]
	private async Task RenameTab(NotepadTabViewModel? tab)
	{
		if (tab is null)
		{
			return;
		}

		KeyValueInputParameters parameters = new()
		{
			DefaultButtonText = Strings.Rename,
			DialogHostIdentifier = DialogHostIdentifiers.Notepad,
			Key = tab.Header,
			KeyHint = Strings.Name
		};

		if (await _dialogService
			.RequestKeyValueInputAsync(parameters)
			.ConfigureAwait(true) is not { } result)
		{
			return;
		}

		tab.Name = result.Key;
	}
	#endregion

	#region Data
	/// <summary>
	/// Pause in typing after which a changed text is written to the disk.
	/// </summary>
	internal static readonly TimeSpan WriteDelay = TimeSpan.FromSeconds(0.5);

	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <summary>
	/// Tabs whose last write failed, so that a failure is told once rather than after every pause in typing.
	/// </summary>
	private readonly HashSet<NotepadTabViewModel> _failedWrites = [];

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <summary>
	/// Timers of the tabs whose changed texts wait for a pause in typing to be written.
	/// </summary>
	private readonly Dictionary<NotepadTabViewModel, ITimer> _pendingWrites = [];

	/// <inheritdoc cref="INotepadSessionState" />
	private readonly INotepadSessionState _sessionState;

	/// <inheritdoc cref="INotepadStore" />
	private readonly INotepadStore _store;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;

	/// <summary>
	/// Numbers of the texts that cannot be read: their files stay on the disk as they are, so no new tab takes them.
	/// </summary>
	private readonly HashSet<int> _unreadNumbers = [];

	/// <inheritdoc cref="IViewCache" />
	private readonly IViewCache _viewCache;

	/// <inheritdoc cref="IViewLauncher" />
	private readonly IViewLauncher _viewLauncher;

	/// <summary>
	/// <c>True</c> once the tabs have been read from the disk and until the notepad closes: meanwhile the tabs and their
	/// texts are written back, and a closed tab takes its text away.
	/// </summary>
	private bool _isKeepingTabs;
	#endregion

	#region Constructors
	public NotepadViewModel(
		IDialogService dialogService,
		IDispatcherAccessor dispatcher,
		INotepadSessionState sessionState,
		INotepadStore store,
		INotificationService notification,
		IViewCache viewCache,
		IViewLauncher viewLauncher,
		TimeProvider timeProvider)
	{
		_dialogService = dialogService;

		_dispatcher = dispatcher;

		_sessionState = sessionState;

		_store = store;

		_notification = notification;

		_viewCache = viewCache;

		_viewLauncher = viewLauncher;

		_timeProvider = timeProvider;

		Tabs.CollectionChanged += Tabs_CollectionChanged;

		AddTab();
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="TextDocument.TextChanged" /> event handler of the texts of the tabs.
	/// </summary>
	private void Document_TextChanged(object? sender, EventArgs e)
	{
		if (Tabs.FirstOrDefault(x => ReferenceEquals(x.Document, sender)) is not { } tab)
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

	/// <summary>
	/// <see cref="INotifyPropertyChanged.PropertyChanged" /> event handler of the tabs.
	/// </summary>
	private void Tab_PropertyChanged(object? sender, PropertyChangedEventArgs e) => WriteSettings();

	/// <summary>
	/// <see cref="INotifyCollectionChanged.CollectionChanged" /> event handler of <see cref="Tabs" />.
	/// </summary>
	private void Tabs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		// A tab is watched while it is open; a move takes it out and puts it back.
		foreach (NotepadTabViewModel tab in e.OldItems?.OfType<NotepadTabViewModel>() ?? [])
		{
			tab.PropertyChanged -= Tab_PropertyChanged;
		}

		foreach (NotepadTabViewModel tab in e.NewItems?.OfType<NotepadTabViewModel>() ?? [])
		{
			tab.PropertyChanged += Tab_PropertyChanged;
		}

		WriteSettings();
	}
	#endregion

	#region Methods
	/// <summary>
	/// Opens the tabs kept on the disk with their state and texts, and a tab at the end for each text that no tab lists;
	/// from then on every change of the tabs and their texts is written back, and a closed tab takes its text away.
	/// </summary>
	public void LoadTabs()
	{
		if (_store.ReadSettings() is { } settings)
		{
			RestoreTabs(settings);
		}

		_isKeepingTabs = true;

		foreach (NotepadTabViewModel tab in Tabs)
		{
			LoadText(tab);
		}

		// Texts that the settings do not list, as when the settings could not be written or read.
		foreach (int number in _store
			.FindNumbers()
			.Where(x => Tabs.All(y => y.Number != x))
			.Order()
			.ToArray())
		{
			NotepadTabViewModel tab = new()
			{
				Number = number
			};

			Tabs.Add(tab);

			LoadText(tab);
		}
	}

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		// No pause in typing comes after the close, so a text still waiting for one is written now.
		foreach (NotepadTabViewModel tab in _pendingWrites.Keys.ToArray())
		{
			WritePendingText(tab);
		}

		// Once more as the window closes, in case a write after a change failed.
		WriteSettings();

		// The editors may still report the state of their tabs as the window goes, after the last write.
		_isKeepingTabs = false;

		// The editors are cached for the whole application, which would keep them after the window.
		foreach (NotepadTabViewModel tab in Tabs)
		{
			_viewCache.Remove(tab);
		}

		base.AfterDispose();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the settings that keep a tab: its number, name, the state of its editor with the encoding of its text, and
	/// its split.
	/// </summary>
	private static NotepadTabSettings CreateTabSettings(NotepadTabViewModel tab)
	{
		return new()
		{
			EditorState = EditorStateMapper.Create(tab, tab.Codec.Encoding, isEncrypted: false),
			Name = tab.Name,
			Number = tab.Number,
			Split = tab.IsSplit ? tab.SplitShare : null
		};
	}

	/// <summary>
	/// <c>True</c> when the text of the tab has more than white space.
	/// </summary>
	private static bool HasText(NotepadTabViewModel tab) => !string.IsNullOrWhiteSpace(tab.Document.Text);

	/// <summary>
	/// Returns a tab with the name, the editor state and the split kept in its settings; its text is read later, in the
	/// encoding kept with them.
	/// </summary>
	private static NotepadTabViewModel RestoreTab(NotepadTabSettings settings)
	{
		NotepadTabViewModel tab = new()
		{
			Name = settings.Name,
			Number = settings.Number
		};

		if (settings.Split is { } split)
		{
			tab.SplitShare = split;

			tab.IsSplit = true;
		}

		if (settings.EditorState is not { } state)
		{
			return tab;
		}

		EditorStateMapper.Apply(tab, state);

		// Reading no bytes gives the tab the kept encoding, which its text is read in later.
		tab.Codec.Read([], state.Encoding);

		return tab;
	}

	/// <summary>
	/// Returns the settings that keep the tabs in their order, with the number of the selected one.
	/// </summary>
	private NotepadViewSettings CreateSettings()
	{
		return new()
		{
			SelectedTabNumber = SelectedTab?.Number,
			Tabs = [.. Tabs.Select(CreateTabSettings)]
		};
	}

	/// <summary>
	/// Returns the bytes of the text of a tab in its encoding; a text that the encoding cannot hold is kept in UTF-8 from
	/// then on, where a lone surrogate becomes the replacement character.
	/// </summary>
	private byte[] EncodeText(NotepadTabViewModel tab)
	{
		string text = tab.Document.Text;

		if (tab.Codec.Encode(text, out string? missingCharacter) is { } contents)
		{
			return contents;
		}

		byte[] utf8 = Encoding.UTF8.GetBytes(text);

		if (tab.Codec.Encoding == Encoding.UTF8.WebName)
		{
			return utf8;
		}

		_notification.ShowWarningSnackbar(
			string.Format(
				CultureInfo.CurrentCulture,
				Strings.CharacterNotInEncodingSavedInUtf8Format,
				missingCharacter,
				tab.Codec.EncodingName),
			SnackbarHostIdentifiers.Notepad);

		// The text takes UTF-8 as it is read back from its bytes.
		tab.Codec.Read(utf8, Encoding.UTF8.WebName);

		// The settings name the new encoding before the text is written in it.
		WriteSettings();

		return utf8;
	}

	/// <summary>
	/// Stops writing the text of a closed tab and erases it from the disk; a text that cannot be read stays as it is.
	/// </summary>
	private void EraseText(NotepadTabViewModel tab)
	{
		tab.Document.TextChanged -= Document_TextChanged;

		if (_pendingWrites.Remove(tab, out ITimer? timer))
		{
			timer.Dispose();
		}

		_failedWrites.Remove(tab);

		if (!_isKeepingTabs || tab.IsReadOnly)
		{
			return;
		}

		_store.Erase(tab.Number);
	}

	/// <summary>
	/// Reads the text of a tab from the disk and writes it back after each pause in typing; a text that cannot be read
	/// leaves the tab read-only, with its file and its number untouched.
	/// </summary>
	private void LoadText(NotepadTabViewModel tab)
	{
		// The text is read in the encoding of the tab, UTF-8 unless the settings keep another one.
		if (_store.Read(tab.Number) is not { } contents
			|| tab.Codec.Read(contents, tab.Codec.Encoding) is not { } text)
		{
			tab.IsReadOnly = true;

			_unreadNumbers.Add(tab.Number);

			string message = string.Format(CultureInfo.CurrentCulture, Strings.TabTextUnreadableFormat, tab.Header);

			// The snackbar of the window takes messages only once the window is loaded, which comes before the background.
			_dispatcher.Post(
				() => _notification.ShowErrorSnackbar(message, SnackbarHostIdentifiers.Notepad),
				DispatcherPriority.Background);

			return;
		}

		tab.Document.Text = text;

		// Undo starts from the text as it was read, not from an empty tab.
		tab
			.Document
			.UndoStack
			.ClearAll();

		tab.Document.TextChanged += Document_TextChanged;
	}

	/// <summary>
	/// Takes a tab away with its editor and its text; the last one gives way to a new tab.
	/// </summary>
	private void RemoveTab(NotepadTabViewModel tab)
	{
		Tabs.Remove(tab);

		_viewCache.Remove(tab);

		EraseText(tab);

		// A closed tab is no way back, and its number may go to a new tab.
		if (tab == PreviousTab)
		{
			PreviousTab = null;
		}

		if (Tabs.Count > 0)
		{
			return;
		}

		AddTab();
	}

	/// <summary>
	/// Opens the saved tabs in their order, selects the saved one, or the first one when it is missing, and gives Ctrl+Tab
	/// the way back kept for the session; without saved tabs the open ones stay.
	/// </summary>
	private void RestoreTabs(NotepadViewSettings settings)
	{
		if (settings.Tabs is not { Length: > 0 } tabs)
		{
			return;
		}

		Tabs.Clear();

		foreach (NotepadTabSettings tab in tabs)
		{
			Tabs.Add(RestoreTab(tab));
		}

		SelectedTab = Tabs.FirstOrDefault(x => x.Number == settings.SelectedTabNumber) ?? Tabs[0];

		PreviousTab = Tabs.FirstOrDefault(x => x.Number == _sessionState.PreviousTabNumber);
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
	/// Writes the settings of the tabs to the disk while the notepad keeps its tabs there.
	/// </summary>
	private void WriteSettings()
	{
		if (!_isKeepingTabs)
		{
			return;
		}

		_store.WriteSettings(CreateSettings());
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
