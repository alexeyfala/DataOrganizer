using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public partial class YesNoCancelBoxView : UserControl
{
	#region Constructors
	public YesNoCancelBoxView() => InitializeComponent();

	public YesNoCancelBoxView(YesNoCancelBoxViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
