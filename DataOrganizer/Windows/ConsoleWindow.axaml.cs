using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class ConsoleWindow : Window
{
	#region Properties
	/// <inheritdoc cref="ConsoleViewModel" />
	public ConsoleViewModel ViewModel { get; }
	#endregion Properties

	#region Constructors
	public ConsoleWindow(ConsoleViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}