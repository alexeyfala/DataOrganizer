using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public partial class YesNoCancelBox : UserControl
{
	#region Constructors
	public YesNoCancelBox(YesNoCancelBoxViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
	}
	#endregion
}