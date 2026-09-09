using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class MultilineTextEditView : UserControl
{
	#region Constructors
	public MultilineTextEditView() => InitializeComponent();

	public MultilineTextEditView(MultilineTextEditViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
