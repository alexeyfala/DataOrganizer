using DataOrganizer.ViewModels;
using DataOrganizer.Wrappers;

namespace DataOrganizer.Views;

public partial class HotkeysEditorView : CustomUserControl
{
	#region Properties
	/// <inheritdoc cref="HotkeysEditorViewModel" />
	public HotkeysEditorViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public HotkeysEditorView(HotkeysEditorViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}