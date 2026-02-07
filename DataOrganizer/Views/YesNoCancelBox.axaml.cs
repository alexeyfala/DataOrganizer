using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public partial class YesNoCancelBox : UserControl
{
	#region Properties
	/// <inheritdoc cref="YesNoCancelBoxViewModel" />
	public YesNoCancelBoxViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public YesNoCancelBox(YesNoCancelBoxViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}