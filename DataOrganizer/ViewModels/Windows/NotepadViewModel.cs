using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Models.Notepad;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataOrganizer.ViewModels.Windows;

/// <summary>
/// View model for <c>NotepadWindow</c>.
/// </summary>
public sealed partial class NotepadViewModel : ObservableObject
{
	#region Properties
	/// <summary>
	/// Tab that is selected.
	/// </summary>
	[ObservableProperty]
	public partial NotepadTab? SelectedTab { get; set; }

	/// <summary>
	/// Open tabs; at least one is always open.
	/// </summary>
	public ObservableCollection<NotepadTab> Tabs { get; } = [];
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Brings the main window back from the minimized state and activates it.
	/// </summary>
	[RelayCommand]
	private void ActivateMainWindow()
	{
		_viewLauncher.ActivateMainWindow();
	}

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

		NotepadTab tab = new()
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
	/// Closes a tab; the last one gives way to a new tab.
	/// </summary>
	[RelayCommand]
	private void CloseTab(NotepadTab? tab)
	{
		if (tab is null)
		{
			return;
		}

		Tabs.Remove(tab);

		if (Tabs.Count > 0)
		{
			return;
		}

		AddTab();
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IViewLauncher" />
	private readonly IViewLauncher _viewLauncher;
	#endregion

	#region Constructors
	public NotepadViewModel(IViewLauncher viewLauncher)
	{
		_viewLauncher = viewLauncher;

		AddTab();
	}
	#endregion

	#region Methods
	/// <summary>
	/// Opens the saved tabs in their order and selects the saved one, or the first one when it is missing; without saved
	/// tabs the open ones stay.
	/// </summary>
	public void RestoreTabs(NotepadViewSettings settings)
	{
		if (settings.TabNumbers is not { Length: > 0 } numbers)
		{
			return;
		}

		Tabs.Clear();

		foreach (int number in numbers)
		{
			Tabs.Add(new()
			{
				Number = number
			});
		}

		SelectedTab = Tabs.FirstOrDefault(x => x.Number == settings.SelectedTabNumber) ?? Tabs[0];
	}
	#endregion
}
