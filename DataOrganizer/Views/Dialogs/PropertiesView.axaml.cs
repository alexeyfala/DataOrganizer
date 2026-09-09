using Avalonia.Controls;
using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

internal sealed partial class PropertiesView : UserControl
{
	#region Constructors
	public PropertiesView() => InitializeComponent();

	public PropertiesView(PropertiesViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
