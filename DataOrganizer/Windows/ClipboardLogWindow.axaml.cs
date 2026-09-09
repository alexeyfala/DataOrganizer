using Avalonia.Controls;
using DataOrganizer.ViewModels.Windows;

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
