using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

public partial class HotkeysEditorView : DialogViewBase
{
	#region Constructors
	public HotkeysEditorView() => InitializeComponent();

	public HotkeysEditorView(HotkeysEditorViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
