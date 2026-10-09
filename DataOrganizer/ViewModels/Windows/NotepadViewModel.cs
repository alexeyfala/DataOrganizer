using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Views;
using Shared.Properties;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
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
		// One of the numbers up to one past the count of the tabs is always free.
		int number = Enumerable
			.Range(1, Tabs.Count + 1)
			.First(x => Tabs.All(y => y.Number != x));

		NotepadTabViewModel tab = new()
		{
			Number = number
		};

		Tabs.Add(tab);

		SelectedTab = tab;
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
				Strings.TabsBeingClosedContainText,
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
	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="INotepadSessionState" />
	private readonly INotepadSessionState _sessionState;

	/// <inheritdoc cref="IViewCache" />
	private readonly IViewCache _viewCache;

	/// <inheritdoc cref="IViewLauncher" />
	private readonly IViewLauncher _viewLauncher;
	#endregion

	#region Constructors
	public NotepadViewModel(
		IDialogService dialogService,
		INotepadSessionState sessionState,
		IViewCache viewCache,
		IViewLauncher viewLauncher)
	{
		_dialogService = dialogService;

		_sessionState = sessionState;

		_viewCache = viewCache;

		_viewLauncher = viewLauncher;

		AddTab();
	}
	#endregion

	#region Methods
	/// <summary>
	/// Opens the saved tabs with their names in their order, selects the saved one, or the first one when it is missing,
	/// and gives Ctrl+Tab the way back kept for the session; without saved tabs the open ones stay.
	/// </summary>
	public void RestoreTabs(NotepadViewSettings settings)
	{
		if (settings.Tabs is not { Length: > 0 } tabs)
		{
			return;
		}

		Tabs.Clear();

		foreach (NotepadTabSettings tab in tabs)
		{
			Tabs.Add(new()
			{
				Name = tab.Name,
				Number = tab.Number
			});
		}

		SelectedTab = Tabs.FirstOrDefault(x => x.Number == settings.SelectedTabNumber) ?? Tabs[0];

		PreviousTab = Tabs.FirstOrDefault(x => x.Number == _sessionState.PreviousTabNumber);
	}

	/// <inheritdoc />
	protected override void AfterDispose()
	{
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
	/// <c>True</c> when the text of the tab has more than white space.
	/// </summary>
	private static bool HasText(NotepadTabViewModel tab) => !string.IsNullOrWhiteSpace(tab.Document.Text);

	/// <summary>
	/// Takes a tab away with its editor; the last one gives way to a new tab.
	/// </summary>
	private void RemoveTab(NotepadTabViewModel tab)
	{
		Tabs.Remove(tab);

		_viewCache.Remove(tab);

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
	#endregion
}
