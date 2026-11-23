using DataOrganizer.UI.ViewModels.Windows;

namespace DataOrganizer.UI.Windows;

/// <summary>
/// Window of the clipboard history.
/// </summary>
public sealed partial class ClipboardLogWindow : AppWindowBase
{
	#region Properties
	/// <inheritdoc cref="ClipboardLogViewModel" />
	public ClipboardLogViewModel ViewModel { get; } = null!;

	/// <inheritdoc cref="WindowPlacementTracker" />
	internal WindowPlacementTracker Placement { get; }
	#endregion

	#region Constructors
	/// <summary>
	/// Loads the markup and starts tracking the placement of the window.
	/// </summary>
	public ClipboardLogWindow()
	{
		InitializeComponent();

		Placement = WindowPlacementTracker.Attach(this);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ClipboardLogWindow" /> class.
	/// </summary>
	public ClipboardLogWindow(ClipboardLogViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion
}
