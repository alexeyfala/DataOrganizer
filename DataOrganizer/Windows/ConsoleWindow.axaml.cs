using DataOrganizer.ViewModels.Windows;

namespace DataOrganizer.Windows;

public sealed partial class ConsoleWindow : AppWindowBase
{
	#region Properties
	/// <inheritdoc cref="WindowPlacementTracker" />
	internal WindowPlacementTracker Placement { get; }

	/// <inheritdoc cref="ConsoleViewModel" />
	public ConsoleViewModel ViewModel { get; } = null!;
	#endregion

	#region Constructors
	public ConsoleWindow()
	{
		InitializeComponent();

		Placement = WindowPlacementTracker.Attach(this);
	}

	public ConsoleWindow(ConsoleViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion
}
