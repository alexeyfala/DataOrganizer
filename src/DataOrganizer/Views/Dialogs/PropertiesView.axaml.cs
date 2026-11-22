using Avalonia.Controls;
using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

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
