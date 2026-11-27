using Avalonia;
using Avalonia.Interactivity;
using DataOrganizer.UI.ViewModels.Windows;

namespace DataOrganizer.UI.Windows;

/// <summary>
/// Main window that edits the hierarchy of folders and files.
/// </summary>
public sealed partial class EditorWindow : AppWindowBase, IMainWindow
{
	#region Properties
	/// <inheritdoc />
	public bool IsClosingToSwitch { get; set; }

	/// <inheritdoc cref="EditorViewModel" />
	public EditorViewModel ViewModel { get; } = null!;

	/// <inheritdoc cref="WindowPlacementTracker" />
	internal WindowPlacementTracker Placement { get; }
	#endregion

	#region Constructors
	/// <summary>
	/// Loads the markup and starts tracking the placement of the window.
	/// </summary>
	public EditorWindow()
	{
		InitializeComponent();

		Placement = WindowPlacementTracker.Attach(this);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EditorWindow" /> class.
	/// </summary>
	public EditorWindow(EditorViewModel viewModel) : this() => DataContext = ViewModel = viewModel;
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);

		ReportShown();
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == IsVisibleProperty)
		{
			ReportShown();
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Tells the view model whether the window is loaded and visible.
	/// </summary>
	private void ReportShown()
	{
		if (DataContext is not EditorViewModel viewModel)
		{
			return;
		}

		viewModel.IsWindowShown = IsLoaded && IsVisible;
	}
	#endregion
}
