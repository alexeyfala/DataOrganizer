using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

internal partial class ToastWindow : Window
{
	#region Properties
	/// <inheritdoc cref="ToastViewModel" />
	public ToastViewModel ViewModel { get; }
	#endregion Properties

	#region Constructors
	public ToastWindow(ToastViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}