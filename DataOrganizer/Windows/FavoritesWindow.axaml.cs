using Avalonia.Controls;
using DataOrganizer.Helpers;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class FavoritesWindow : Window
{
	#region Properties
	/// <inheritdoc cref="WindowPlacementTracker" />
	internal WindowPlacementTracker Placement { get; }

	/// <inheritdoc cref="FavoritesViewModel" />
	public FavoritesViewModel ViewModel { get; } = null!;
	#endregion Properties

	#region Constructors
	public FavoritesWindow()
	{
		InitializeComponent();

		Placement = WindowPlacementTracker.Attach(this);
	}

	public FavoritesWindow(FavoritesViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion
}
