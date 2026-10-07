using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Interfaces.Views;

namespace DataOrganizer.ViewModels.Windows;

/// <summary>
/// View model for <c>NotepadWindow</c>.
/// </summary>
public sealed partial class NotepadViewModel : ObservableObject
{
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
	#endregion

	#region Data
	/// <inheritdoc cref="IViewLauncher" />
	private readonly IViewLauncher _viewLauncher;
	#endregion

	#region Constructors
	public NotepadViewModel(IViewLauncher viewLauncher)
	{
		_viewLauncher = viewLauncher;
	}
	#endregion
}
