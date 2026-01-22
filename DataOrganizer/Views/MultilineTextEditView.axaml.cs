using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class MultilineTextEditView : UserControl
{
	#region Properties
	/// <inheritdoc cref="MultilineTextEditViewModel" />
	public MultilineTextEditViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public MultilineTextEditView(MultilineTextEditViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}