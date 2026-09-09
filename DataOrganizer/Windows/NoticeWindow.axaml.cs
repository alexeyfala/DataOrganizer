using Avalonia.Controls;
using DataOrganizer.ViewModels.Windows;

namespace DataOrganizer.Windows;

internal partial class NoticeWindow : Window
{
	#region Constructors
	public NoticeWindow() => InitializeComponent();

	public NoticeWindow(NoticeViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
