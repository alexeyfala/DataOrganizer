using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class SelectedFavoritesView : UserControl
{
	#region Properties
	/// <inheritdoc cref="SelectedFavoritesViewModel" />
	public SelectedFavoritesViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public SelectedFavoritesView()
	{
		InitializeComponent();

		DataContext = ViewModel = Ioc
			.Default
			.GetRequiredService<SelectedFavoritesViewModel>();
	}
	#endregion
}