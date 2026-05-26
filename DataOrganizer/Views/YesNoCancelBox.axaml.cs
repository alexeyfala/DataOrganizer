using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public partial class YesNoCancelBox : UserControl
{
	#region Constructors
	public YesNoCancelBox() => InitializeComponent();

	public YesNoCancelBox(YesNoCancelBoxViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
