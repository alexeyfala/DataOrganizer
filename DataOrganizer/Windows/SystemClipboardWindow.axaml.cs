using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DataOrganizer.ViewModels;
using System;

namespace DataOrganizer.Windows;

public sealed partial class SystemClipboardWindow : Window
{
	#region Constructors
	public SystemClipboardWindow() => InitializeComponent();

	public SystemClipboardWindow(SystemClipboardViewModel viewModel) : this()
	{
		DataContext = viewModel;

		Deactivated += OnDeactivated;

		KeyDown += OnKeyDown;

		// Any button inside the list ends up here via bubbling. We close after
		// the bound RelayCommand has been queued — restoration finishes on the
		// dispatcher independently of the popup's lifetime.
		AddHandler(Button.ClickEvent, OnButtonClick);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// Closes the popup as soon as any item button is clicked.
	/// </summary>
	private void OnButtonClick(object? sender, RoutedEventArgs e) => Close();

	/// <summary>
	/// Auto-close when focus moves to any other window.
	/// </summary>
	private void OnDeactivated(object? sender, EventArgs e) => Close();

	/// <summary>
	/// Esc closes the popup.
	/// </summary>
	private void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			Close();
		}
	}
	#endregion
}
