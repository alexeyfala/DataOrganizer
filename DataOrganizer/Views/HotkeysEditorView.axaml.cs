using DataOrganizer.ViewModels;
using DataOrganizer.Wrappers;

namespace DataOrganizer.Views;

public partial class HotkeysEditorView : CustomUserControl
{
	#region Constructors
	public HotkeysEditorView(HotkeysEditorViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
	}
	#endregion
}