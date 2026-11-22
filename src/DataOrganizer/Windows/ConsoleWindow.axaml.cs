using DataOrganizer.ViewModels.Windows;

namespace DataOrganizer.Windows;

/// <summary>
/// Window that shows the log of the application.
/// </summary>
public sealed partial class ConsoleWindow : AppWindowBase
{
	#region Properties
	/// <inheritdoc cref="WindowPlacementTracker" />
	internal WindowPlacementTracker Placement { get; }

	/// <inheritdoc cref="ConsoleViewModel" />
	public ConsoleViewModel ViewModel { get; } = null!;
	#endregion

	#region Constructors
	/// <summary>
	/// Loads the markup and starts tracking the placement of the window.
	/// </summary>
	public ConsoleWindow()
	{
		InitializeComponent();

		Placement = WindowPlacementTracker.Attach(this);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ConsoleWindow" /> class.
	/// </summary>
	public ConsoleWindow(ConsoleViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion
}
