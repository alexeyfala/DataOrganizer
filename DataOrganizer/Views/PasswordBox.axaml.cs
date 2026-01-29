using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

public sealed partial class PasswordBox : UserControl
{
	#region Properties
	/// <inheritdoc cref="PasswordBoxViewModel" />
	public PasswordBoxViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public PasswordBox(PasswordBoxViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}