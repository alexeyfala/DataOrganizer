using Avalonia.Controls;
using DataOrganizer.UI.ViewModels.Dialogs;

namespace DataOrganizer.UI.Views.Dialogs;

internal sealed partial class AppPickerView : UserControl
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="AppPickerView" /> class.
	/// </summary>
	public AppPickerView() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="AppPickerView" /> class.
	/// </summary>
	public AppPickerView(AppPickerViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
