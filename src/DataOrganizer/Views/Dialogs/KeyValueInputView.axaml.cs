using Avalonia.Controls;
using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

public partial class KeyValueInputView : UserControl
{
	#region Constructors
	public KeyValueInputView() => InitializeComponent();

	public KeyValueInputView(KeyValueInputViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
