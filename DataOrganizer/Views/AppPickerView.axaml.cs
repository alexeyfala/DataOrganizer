using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal sealed partial class AppPickerView : UserControl
{
	#region Constructors
	public AppPickerView()
	{
		InitializeComponent();
	}

	public AppPickerView(AppPickerViewModel viewModel) : this()
	{
		DataContext = viewModel;
	}
	#endregion
}
