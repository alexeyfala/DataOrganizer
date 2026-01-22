using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class CopyHistoryView : UserControl
{
	#region Properties
	/// <inheritdoc cref="CopyHistoryViewModel" />
	public CopyHistoryViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public CopyHistoryView(CopyHistoryViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}