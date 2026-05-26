using DataOrganizer.ViewModels;
using DataOrganizer.Wrappers;

namespace DataOrganizer.Views;

public partial class HotkeysEditorView : CustomUserControl
{
	#region Constructors
	public HotkeysEditorView() => InitializeComponent();

	public HotkeysEditorView(HotkeysEditorViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
