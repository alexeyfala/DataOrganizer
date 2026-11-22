using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

/// <summary>
/// Dialog that edits the hotkeys of a file.
/// </summary>
public partial class HotkeysEditorView : DialogViewBase
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="HotkeysEditorView" /> class.
	/// </summary>
	public HotkeysEditorView() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="HotkeysEditorView" /> class.
	/// </summary>
	public HotkeysEditorView(HotkeysEditorViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
