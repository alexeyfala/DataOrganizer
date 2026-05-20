using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal partial class EmbeddedFileEditorView : UserControl
{
	#region Constructors
	public EmbeddedFileEditorView(EmbeddedFileEditorViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
	}
	#endregion
}