using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal partial class EmbeddedFileEditorView : UserControl
{
	#region Properties
	/// <inheritdoc cref="EmbeddedFileEditorViewModel" />
	public EmbeddedFileEditorViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public EmbeddedFileEditorView(EmbeddedFileEditorViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}