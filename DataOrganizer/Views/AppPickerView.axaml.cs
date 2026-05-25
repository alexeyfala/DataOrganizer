using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal sealed partial class AppPickerView : UserControl
{
	#region Constructors
	public AppPickerView(AppPickerViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
	}
	#endregion
}
