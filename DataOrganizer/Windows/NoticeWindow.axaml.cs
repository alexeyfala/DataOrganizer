using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

internal partial class StartupErrorWindow : Window
{
	#region Constructors
	public StartupErrorWindow() => InitializeComponent();

	public StartupErrorWindow(StartupErrorViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
