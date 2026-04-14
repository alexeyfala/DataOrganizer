using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

/// <summary>
/// <see cref="UserControl" /> for editing files.
/// </summary>
public sealed partial class EditFilesView : UserControl
{
	#region Properties
	/// <inheritdoc cref="EditFilesViewModel" />
	public EditFilesViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public EditFilesView(EditFilesViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}