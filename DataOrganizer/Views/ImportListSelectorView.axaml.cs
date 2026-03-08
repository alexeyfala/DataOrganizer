using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal sealed partial class ImportListSelectorView : UserControl
{
	#region Properties
	/// <inheritdoc cref="ImportListSelectorViewModel" />
	public ImportListSelectorViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public ImportListSelectorView(ImportListSelectorViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}