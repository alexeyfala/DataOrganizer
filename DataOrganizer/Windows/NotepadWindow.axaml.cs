using Avalonia.Controls;
using DataOrganizer.ViewModels.Windows;

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
}
