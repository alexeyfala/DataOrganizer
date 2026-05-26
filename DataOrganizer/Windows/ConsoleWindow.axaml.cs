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
	/// <summary>
	/// Parameterless ctor for the Avalonia XAML compiler / previewer.
	/// Not used at runtime — DI always invokes the overload below.
	/// </summary>
	public ConsoleWindow() => InitializeComponent();

	public ConsoleWindow(ConsoleViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion
}
