using Avalonia.Controls;
using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

internal sealed partial class ImportListSelectorView : UserControl
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="ImportListSelectorView" /> class.
	/// </summary>
	public ImportListSelectorView() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="ImportListSelectorView" /> class.
	/// </summary>
	public ImportListSelectorView(ImportListSelectorViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
