using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Notepad;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Notepad;
using DataOrganizer.Interfaces.Text;
using DataOrganizer.Interfaces.Views;
using DynamicData;
using DynamicData.Binding;
using Shared.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
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
	private void ActivateMainWindow() => _mainWindowActivator.ActivateMainWindow();

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

		NotepadTabViewModel tab = new(_documentTextFactory.Create())
		{
			Number = number
		};

		Tabs.Add(tab);

		SelectedTab = tab;

		// The texts are kept on the disk once they have been read from it.
		if (!_isKeepingTabs)
		{
			return;
		}

		_texts.Follow(tab);
	}

	/// <summary>
	/// Brings the main window to the center of the screen of the notepad.
	/// </summary>
	[RelayCommand]
	private void CenterMainWindow() => _mainWindowActivator.CenterMainWindow();

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

		if (HasText(tab) && !await _confirmationPrompt
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

		if (tabs.Any(HasText) && !await _confirmationPrompt
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

		if (await _textInputPrompt
			.RequestKeyValueInputAsync(parameters)
			.ConfigureAwait(true) is not { } result)
		{
			return;
		}

		tab.Name = result.Key;
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IConfirmationPrompt" />
	private readonly IConfirmationPrompt _confirmationPrompt;

	/// <inheritdoc cref="IDocumentTextFactory" />
	private readonly IDocumentTextFactory _documentTextFactory;

	/// <inheritdoc cref="IDocumentEncodingSwitcher" />
	private readonly IDocumentEncodingSwitcher _encodingSwitcher;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IMainWindowActivator" />
	private readonly IMainWindowActivator _mainWindowActivator;

	/// <inheritdoc cref="INotepadSessionState" />
	private readonly INotepadSessionState _sessionState;

	/// <inheritdoc cref="INotepadStore" />
	private readonly INotepadStore _store;

	/// <inheritdoc cref="ISyntaxRegistry" />
	private readonly ISyntaxRegistry _syntaxRegistry;

	/// <inheritdoc cref="ITextInputPrompt" />
	private readonly ITextInputPrompt _textInputPrompt;

	/// <inheritdoc cref="INotepadTexts" />
	private readonly INotepadTexts _texts;

	/// <summary>
	/// Numbers of the texts that cannot be read: their files stay on the disk as they are, so no new tab takes them.
	/// </summary>
	private readonly HashSet<int> _unreadNumbers = [];

	/// <summary>
	/// <c>True</c> once the tabs have been read from the disk and until the notepad closes: meanwhile the tabs and their
	/// texts are written back, and a closed tab takes its text away.
	/// </summary>
	private bool _isKeepingTabs;
	#endregion

	#region Constructors
	public NotepadViewModel(
		IConfirmationPrompt confirmationPrompt,
		IDocumentEncodingSwitcher encodingSwitcher,
		IDocumentTextFactory documentTextFactory,
		IMainWindowActivator mainWindowActivator,
		INotepadSessionState sessionState,
		INotepadStore store,
		INotepadTexts texts,
		ISyntaxRegistry syntaxRegistry,
		ITaskExceptionHandler exceptionHandler,
		ITextInputPrompt textInputPrompt)
	{
		_confirmationPrompt = confirmationPrompt;

		_encodingSwitcher = encodingSwitcher;

		_documentTextFactory = documentTextFactory;

		_mainWindowActivator = mainWindowActivator;

		_sessionState = sessionState;

		_store = store;

		_texts = texts;

		_syntaxRegistry = syntaxRegistry;

		_textInputPrompt = textInputPrompt;

		_exceptionHandler = exceptionHandler;

		// A tab opened, closed or moved, or a change of any property of a tab or of its editor, writes the tabs.
		Tabs
			.ToObservableChangeSet()
			.AutoRefresh()
			.AutoRefreshOnObservable(x => x.Editor.WhenAnyPropertyChanged())
			.Subscribe(_ => WriteSettings())
			.DisposeWith(_disposables);

		// A choice of another encoding reads the text of its tab again.
		Tabs
			.ToObservableChangeSet()
			.MergeMany(x => x.Editor
				.WhenValueChanged(y => y.Encoding, notifyOnInitialValue: false)
				.Select(y => (Tab: x, Encoding: y)))
			.Subscribe(x => _exceptionHandler.Watch(ChangeEncodingAsync(x.Tab, x.Encoding)))
			.DisposeWith(_disposables);

		AddTab();
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
			NotepadTabViewModel tab = new(_documentTextFactory.Create())
			{
				Number = number
			};

			Tabs.Add(tab);

			LoadText(tab);
		}

		// Reading the tabs changes nothing to write, so the writes start once they are read.
		_isKeepingTabs = true;
	}

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		// No pause in typing comes after the close, so a text still waiting for one is written now.
		_texts.WritePending();

		// Once more as the window closes, in case a write after a change failed.
		WriteSettings();

		// The window may still change the selected tab as it goes, after the last write.
		_isKeepingTabs = false;

		base.AfterDispose();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when the text of the tab has more than white space.
	/// </summary>
	private static bool HasText(NotepadTabViewModel tab) => !string.IsNullOrWhiteSpace(tab.Editor.Document.Text);

	/// <summary>
	/// Reads the text of a tab again from the same bytes in the encoding chosen for it; a change of the encoding writes the
	/// settings, so they name it before any text is written in it.
	/// </summary>
	private Task ChangeEncodingAsync(NotepadTabViewModel tab, string? encoding)
	{
		return _encodingSwitcher.SwitchAsync(
			tab.Editor,
			encoding,
			SnackbarHostIdentifiers.Notepad,
			$"notepad tab {tab.Number}");
	}

	/// <summary>
	/// Stops writing the text of a closed tab and erases it from the disk once the texts are kept there.
	/// </summary>
	private void EraseText(NotepadTabViewModel tab)
	{
		_texts.Forget(tab);

		if (!_isKeepingTabs)
		{
			return;
		}

		_texts.Erase(tab);
	}

	/// <summary>
	/// Reads the text of a tab from the disk; a text that cannot be read keeps its file and number untouched.
	/// </summary>
	private void LoadText(NotepadTabViewModel tab)
	{
		if (_texts.Load(tab))
		{
			return;
		}

		_unreadNumbers.Add(tab.Number);
	}

	/// <summary>
	/// Takes a tab away with its editor and its text; the last one gives way to a new tab.
	/// </summary>
	private void RemoveTab(NotepadTabViewModel tab)
	{
		Tabs.Remove(tab);

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
			Tabs.Add(NotepadSettingsMapper.CreateTab(tab, _documentTextFactory.Create(), _syntaxRegistry));
		}

		SelectedTab = Tabs.FirstOrDefault(x => x.Number == settings.SelectedTabNumber) ?? Tabs[0];

		PreviousTab = Tabs.FirstOrDefault(x => x.Number == _sessionState.PreviousTabNumber);
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

		_store.WriteSettings(NotepadSettingsMapper.Create(Tabs, SelectedTab));
	}
	#endregion
}
