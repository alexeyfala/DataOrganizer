using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class MultilineTextEditView : UserControl
{
	#region Constructors
	public MultilineTextEditView(MultilineTextEditViewModel viewModel)
	{
		InitializeComponent();

		DataContext =  viewModel;
	}
	#endregion
}