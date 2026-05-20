using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal sealed partial class SelectedFavoritesView : UserControl
{
	#region Constructors
	public SelectedFavoritesView()
	{
		InitializeComponent();

		DataContext = Ioc
			.Default
			.GetRequiredService<SelectedFavoritesViewModel>();
	}
	#endregion
}