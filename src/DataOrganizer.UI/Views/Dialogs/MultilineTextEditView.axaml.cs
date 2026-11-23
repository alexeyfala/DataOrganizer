using Avalonia.Controls;
using DataOrganizer.UI.ViewModels.Dialogs;

namespace DataOrganizer.UI.Views.Dialogs;

/// <summary>
/// Dialog that edits a multiline text.
/// </summary>
public sealed partial class MultilineTextEditView : UserControl
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="MultilineTextEditView" /> class.
	/// </summary>
	public MultilineTextEditView() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="MultilineTextEditView" /> class.
	/// </summary>
	public MultilineTextEditView(MultilineTextEditViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
