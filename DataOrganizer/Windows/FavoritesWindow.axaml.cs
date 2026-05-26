using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class FavoritesWindow : Window
{
	#region Properties
	/// <inheritdoc cref="FavoritesViewModel" />
	public FavoritesViewModel ViewModel { get; }
	#endregion Properties

	#region Constructors
	public FavoritesWindow(FavoritesViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}
