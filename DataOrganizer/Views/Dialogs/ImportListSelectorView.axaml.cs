using Avalonia.Controls;
using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

internal sealed partial class ImportListSelectorView : UserControl
{
	#region Constructors
	public ImportListSelectorView() => InitializeComponent();

	public ImportListSelectorView(ImportListSelectorViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
