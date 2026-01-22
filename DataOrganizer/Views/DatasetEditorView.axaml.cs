using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal partial class DatasetEditorView : UserControl
{
	#region Properties
	/// <inheritdoc cref="DatasetEditorViewModel" />
	public DatasetEditorViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public DatasetEditorView(DatasetEditorViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}