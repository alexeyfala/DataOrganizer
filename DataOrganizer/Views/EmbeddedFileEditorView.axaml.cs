using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal partial class EmbeddedFileEditorView : UserControl
{
	#region Constructors
	public EmbeddedFileEditorView() => InitializeComponent();

	public EmbeddedFileEditorView(EmbeddedFileEditorViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
