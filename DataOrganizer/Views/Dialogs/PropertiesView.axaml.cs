using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal sealed partial class PropertiesView : UserControl
{
	#region Constructors
	public PropertiesView() => InitializeComponent();

	public PropertiesView(PropertiesViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
