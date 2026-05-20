using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal sealed partial class ImportListSelectorView : UserControl
{
	#region Constructors
	public ImportListSelectorView(ImportListSelectorViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
	}
	#endregion
}