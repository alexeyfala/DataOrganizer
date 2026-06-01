using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class FavoritesWindow : Window
{
	#region Properties
	/// <inheritdoc cref="FavoritesViewModel" />
	public FavoritesViewModel ViewModel { get; } = null!;
	#endregion Properties

	#region Constructors
	public FavoritesWindow() => InitializeComponent();

	public FavoritesWindow(FavoritesViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion
}
