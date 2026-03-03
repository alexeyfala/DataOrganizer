using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal sealed partial class PropertiesView : UserControl
{
	#region Properties
	/// <inheritdoc cref="PropertiesViewModel" />
	public PropertiesViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public PropertiesView(PropertiesViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}