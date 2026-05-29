using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class CustomClipboardWindow : Window
{
	#region Constructors
	public CustomClipboardWindow() => InitializeComponent();

	public CustomClipboardWindow(CustomClipboardViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
