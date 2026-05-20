using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public partial class KeyValueInputView : UserControl
{
	#region Constructors
	public KeyValueInputView(KeyValueInputViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
	}
	#endregion
}