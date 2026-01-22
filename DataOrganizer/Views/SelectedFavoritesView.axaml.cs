using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class SelectedFavoritesView : UserControl
{
	#region Properties
	/// <inheritdoc cref="SelectedFavoritesViewModel" />
	public SelectedFavoritesViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public SelectedFavoritesView(SelectedFavoritesViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}