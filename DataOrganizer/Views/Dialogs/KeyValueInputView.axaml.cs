using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public partial class KeyValueInputView : UserControl
{
	#region Constructors
	public KeyValueInputView() => InitializeComponent();

	public KeyValueInputView(KeyValueInputViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
