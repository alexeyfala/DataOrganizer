using Avalonia.Controls;
using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

public partial class YesNoCancelBoxView : UserControl
{
	#region Constructors
	public YesNoCancelBoxView() => InitializeComponent();

	public YesNoCancelBoxView(YesNoCancelBoxViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
