using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public partial class EncryptionView : UserControl
{
	#region Properties
	/// <inheritdoc cref="EncryptionViewModel" />
	public EncryptionViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public EncryptionView(EncryptionViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}