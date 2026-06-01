using Avalonia.Controls;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class ConsoleWindow : Window
{
	#region Properties
	/// <inheritdoc cref="ConsoleViewModel" />
	public ConsoleViewModel ViewModel { get; } = null!;
	#endregion Properties

	#region Constructors
	public ConsoleWindow() => InitializeComponent();

	public ConsoleWindow(ConsoleViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion
}
