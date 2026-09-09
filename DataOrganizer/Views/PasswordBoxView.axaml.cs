using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class PasswordBoxView : UserControl
{
	#region Constructors
	public PasswordBoxView() => InitializeComponent();

	public PasswordBoxView(PasswordBoxViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
