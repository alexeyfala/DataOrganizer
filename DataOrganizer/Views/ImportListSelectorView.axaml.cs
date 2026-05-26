using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal sealed partial class ImportListSelectorView : UserControl
{
	#region Constructors
	public ImportListSelectorView() => InitializeComponent();

	public ImportListSelectorView(ImportListSelectorViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
