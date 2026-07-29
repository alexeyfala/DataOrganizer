using Avalonia.Controls;
using DataOrganizer.Helpers;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Windows;

public sealed partial class ConsoleWindow : Window
{
	#region Properties
	/// <inheritdoc cref="WindowPlacementTracker" />
	internal WindowPlacementTracker Placement { get; }

	/// <inheritdoc cref="ConsoleViewModel" />
	public ConsoleViewModel ViewModel { get; } = null!;
	#endregion Properties

	#region Constructors
	public ConsoleWindow()
	{
		InitializeComponent();

		Placement = WindowPlacementTracker.Attach(this);
	}

	public ConsoleWindow(ConsoleViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion
}
