using Avalonia.Controls;
using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

internal sealed partial class AppPickerView : UserControl
{
	#region Constructors
	public AppPickerView() => InitializeComponent();

	public AppPickerView(AppPickerViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
