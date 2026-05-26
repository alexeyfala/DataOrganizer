using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class PasswordBox : UserControl
{
	#region Constructors
	public PasswordBox() => InitializeComponent();

	public PasswordBox(PasswordBoxViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
