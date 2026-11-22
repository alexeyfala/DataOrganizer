using Avalonia.Controls;
using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

/// <summary>
/// Dialog that asks for a key and, optionally, a value.
/// </summary>
public partial class KeyValueInputView : UserControl
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="KeyValueInputView" /> class.
	/// </summary>
	public KeyValueInputView() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="KeyValueInputView" /> class.
	/// </summary>
	public KeyValueInputView(KeyValueInputViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
