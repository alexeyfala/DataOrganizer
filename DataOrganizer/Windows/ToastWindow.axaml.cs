using Avalonia.Controls;
using DataOrganizer.ViewModels.Windows;

namespace DataOrganizer.Windows;

internal partial class ToastWindow : Window
{
	#region Constructors
	public ToastWindow() => InitializeComponent();

	public ToastWindow(ToastViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
