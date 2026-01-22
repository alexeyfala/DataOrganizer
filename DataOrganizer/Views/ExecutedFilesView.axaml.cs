using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class ExecutedFilesView : UserControl
{
	#region Properties
	/// <inheritdoc cref="ExecutedFilesViewModel" />
	public ExecutedFilesViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public ExecutedFilesView(ExecutedFilesViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}