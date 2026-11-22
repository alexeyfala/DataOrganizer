using DataOrganizer.ViewModels.Windows;

namespace DataOrganizer.Windows;

/// <summary>
/// Main window that shows the favorite files by category.
/// </summary>
public sealed partial class FavoritesWindow : AppWindowBase, IMainWindow
{
	#region Properties
	/// <inheritdoc />
	public bool IsClosingToSwitch { get; set; }

	/// <inheritdoc cref="WindowPlacementTracker" />
	internal WindowPlacementTracker Placement { get; }

	/// <inheritdoc cref="FavoritesViewModel" />
	public FavoritesViewModel ViewModel { get; } = null!;
	#endregion

	#region Constructors
	/// <summary>
	/// Loads the markup and starts tracking the position of the window.
	/// </summary>
	public FavoritesWindow()
	{
		InitializeComponent();

		Placement = WindowPlacementTracker.Attach(this, sizeTracked: false);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FavoritesWindow" /> class.
	/// </summary>
	public FavoritesWindow(FavoritesViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion
}
