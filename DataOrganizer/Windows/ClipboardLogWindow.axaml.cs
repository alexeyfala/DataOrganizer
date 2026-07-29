using Avalonia.Controls;
using DataOrganizer.Helpers;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class ClipboardLogWindow : Window
{
	#region Properties
	/// <inheritdoc cref="ClipboardLogViewModel" />
	public ClipboardLogViewModel ViewModel { get; } = null!;

	/// <inheritdoc cref="WindowPlacementTracker" />
	internal WindowPlacementTracker Placement { get; }
	#endregion

	#region Constructors
	public ClipboardLogWindow()
	{
		InitializeComponent();

		Placement = WindowPlacementTracker.Attach(this);
	}

	public ClipboardLogWindow(ClipboardLogViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion
}
