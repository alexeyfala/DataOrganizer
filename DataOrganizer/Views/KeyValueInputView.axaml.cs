using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public partial class KeyValueInputView : UserControl
{
	#region Properties
	/// <inheritdoc cref="KeyValueInputViewModel" />
	public KeyValueInputViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public KeyValueInputView(KeyValueInputViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}