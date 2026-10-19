using Avalonia.Controls;
using DataOrganizer.ViewModels.Windows;
using System;

namespace DataOrganizer.Windows;

public sealed partial class NotepadWindow : Window
{
	#region Properties
	/// <inheritdoc cref="NotepadViewModel" />
	public NotepadViewModel ViewModel { get; } = null!;

	/// <inheritdoc cref="WindowPlacementTracker" />
	internal WindowPlacementTracker Placement { get; }
	#endregion

	#region Constructors
	public NotepadWindow()
	{
		InitializeComponent();

		Placement = WindowPlacementTracker.Attach(this);
	}

	public NotepadWindow(NotepadViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnClosed(EventArgs e)
	{
		base.OnClosed(e);

		// The tabs end with the window, and the views kept for them go with it.
		Tabs.ForgetViews();
	}
	#endregion
}
