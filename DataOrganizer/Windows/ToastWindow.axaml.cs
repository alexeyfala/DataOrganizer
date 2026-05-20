using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

internal partial class ToastWindow : Window
{
	#region Constructors
	public ToastWindow(ToastViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
	}
	#endregion
}