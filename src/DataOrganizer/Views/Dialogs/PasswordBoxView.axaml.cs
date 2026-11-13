using Avalonia.Controls;
using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

public sealed partial class PasswordBoxView : UserControl
{
	#region Constructors
	public PasswordBoxView() => InitializeComponent();

	public PasswordBoxView(PasswordBoxViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
