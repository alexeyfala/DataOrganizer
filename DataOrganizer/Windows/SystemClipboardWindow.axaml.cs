using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class SystemClipboardWindow : Window
{
	#region Constructors
	public SystemClipboardWindow() => InitializeComponent();

	public SystemClipboardWindow(CustomClipboardViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
