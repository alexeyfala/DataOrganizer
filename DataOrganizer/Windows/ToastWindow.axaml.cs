using DataOrganizer.ViewModels.Windows;

namespace DataOrganizer.Windows;

internal partial class ToastWindow : AppWindowBase
{
	#region Constructors
	public ToastWindow() => InitializeComponent();

	public ToastWindow(ToastViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
