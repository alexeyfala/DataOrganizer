using Avalonia.Controls;
using DataOrganizer.UI.ViewModels.Dialogs;

namespace DataOrganizer.UI.Views.Dialogs;

internal sealed partial class PropertiesView : UserControl
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="PropertiesView" /> class.
	/// </summary>
	public PropertiesView() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="PropertiesView" /> class.
	/// </summary>
	public PropertiesView(PropertiesViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
