using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public partial class YesNoQuestionBox : UserControl
{
	#region Properties
	/// <inheritdoc cref="YesNoQuestionBoxViewModel" />
	public YesNoQuestionBoxViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public YesNoQuestionBox(YesNoQuestionBoxViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}
